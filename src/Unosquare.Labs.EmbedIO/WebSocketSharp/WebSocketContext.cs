#if NET452
#region License
/*
 * TcpListenerWebSocketContext.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2016 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

#region Contributors
/*
 * Contributors:
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Log;
#if SSL
using System.Net.Security;
#endif

namespace WebSocketSharp.Net.WebSockets
{
    /// <summary>
    /// Provides the properties used to access the information in
    /// a WebSocket handshake request received by the <see cref="TcpListener"/>.
    /// </summary>
    public class WebSocketContext
    {
        #region Private Fields

        private CookieCollection _cookies;
        private NameValueCollection _queryString;
        private readonly HttpRequest _request;
        private readonly TcpClient _tcpClient;

        #endregion

        #region Internal Constructors

        internal WebSocketContext(
            TcpClient tcpClient,
            string protocol,
            bool secure,
#if SSL
          ServerSslConfiguration sslConfig,
#endif
            ILog logger)
        {
            _tcpClient = tcpClient;
            IsSecureConnection = secure;
            Log = logger;

            var netStream = tcpClient.GetStream();
            if (secure)
            {
#if SSL
                var sslStream =
                  new SslStream(netStream, false, sslConfig.ClientCertificateValidationCallback);

                sslStream.AuthenticateAsServer(
                  sslConfig.ServerCertificate,
                  sslConfig.ClientCertificateRequired,
                  sslConfig.EnabledSslProtocols,
                  sslConfig.CheckCertificateRevocation
                );

                _stream = sslStream;
#else
                throw new Exception("SSL is not supported");
#endif
            }
            else
            {
                Stream = netStream;
            }

            _request = HttpRequest.Read(Stream, 90000);
            RequestUri = CreateRequestUrl(_request.RequestUri, _request.Headers["Host"], _request.IsWebSocketRequest, secure);

            WebSocket = new WebSocket(this, protocol);
        }

        #endregion

        #region Internal Properties

        internal ILog Log { get; }

        internal Stream Stream { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP cookies included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="CookieCollection"/> that contains the cookies.
        /// </value>
        public CookieCollection CookieCollection => _cookies ?? (_cookies = _request.Cookies);

        /// <summary>
        /// Gets the HTTP headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public NameValueCollection Headers => _request.Headers;

        /// <summary>
        /// Gets the value of the Host header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Host header.
        /// </value>
        public string Host => _request.Headers[nameof(Host)];

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthenticated => User != null;

        /// <summary>
        /// Gets a value indicating whether the client connected from the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client connected from the local computer; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal => IsIPAddressLocal(UserEndPoint.Address);

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secured; otherwise, <c>false</c>.
        /// </value>
        public bool IsSecureConnection { get; }

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise, <c>false</c>.
        /// </value>
        public bool IsWebSocketRequest => _request.IsWebSocketRequest;

        /// <summary>
        /// Gets the value of the Origin header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Origin header.
        /// </value>
        public string Origin => _request.Headers[nameof(Origin)];

        /// <summary>
        /// Gets the query string included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the query string parameters.
        /// </value>
        public NameValueCollection QueryString
            => _queryString ?? (_queryString = ParseQueryString(RequestUri?.Query));

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the requested URI.
        /// </value>
        public Uri RequestUri { get; }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in the request.
        /// </summary>
        /// <remarks>
        /// This property provides a part of the information used by the server to prove that
        /// it received a valid WebSocket handshake request.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Key header.
        /// </value>
        public string SecWebSocketKey => _request.Headers["Sec-WebSocket-Key"];

        /// <summary>
        /// Gets the values of the Sec-WebSocket-Protocol header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the subprotocols requested by the client.
        /// </remarks>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable{string}"/> instance that provides
        /// an enumerator which supports the iteration over the values of the Sec-WebSocket-Protocol
        /// header.
        /// </value>
        public IEnumerable<string> SecWebSocketProtocols
        {
            get
            {
                var protocols = _request.Headers["Sec-WebSocket-Protocol"];
                if (protocols != null)
                {
                    foreach (var protocol in protocols.Split(','))
                        yield return protocol.Trim();
                }
            }
        }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public string SecWebSocketVersion => _request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the server endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="IPEndPoint"/> that represents the server endpoint.
        /// </value>
        public IPEndPoint ServerEndPoint => (IPEndPoint)_tcpClient.Client.LocalEndPoint;

        /// <summary>
        /// Gets the client information (identity, authentication, and security roles).
        /// </summary>
        /// <value>
        /// A <see cref="IPrincipal"/> instance that represents the client information.
        /// </value>
        public IPrincipal User { get; } = null;

        /// <summary>
        /// Gets the client endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="IPEndPoint"/> that represents the client endpoint.
        /// </value>
        public IPEndPoint UserEndPoint => (IPEndPoint)_tcpClient.Client.RemoteEndPoint;

        /// <summary>
        /// Gets the <see cref="WebSocketSharp.WebSocket"/> instance used for
        /// two-way communication between client and server.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSharp.WebSocket"/>.
        /// </value>
        public WebSocket WebSocket { get; }

        #endregion

        #region Internal Methods

        internal Uri CreateRequestUrl(string requestUri, string host, bool websocketRequest, bool secure)
        {
            if (string.IsNullOrEmpty(requestUri) || host == null || host.Length == 0)
                return null;

            string schm = null;
            string path = null;
            if (requestUri.StartsWith("/"))
            {
                path = requestUri;
            }
            else if (requestUri.MaybeUri())
            {
                Uri uri;
                var valid = Uri.TryCreate(requestUri, UriKind.Absolute, out uri) &&
                            (((schm = uri.Scheme).StartsWith("http") && !websocketRequest) ||
                             (schm.StartsWith("ws") && websocketRequest));

                if (!valid)
                    return null;

                host = uri.Authority;
                path = uri.PathAndQuery;
            }
            else if (requestUri == "*")
            {
            }
            else
            {
                // As authority form
                host = requestUri;
            }

            if (schm == null)
                schm = (websocketRequest ? "ws" : "http") + (secure ? "s" : String.Empty);

            var colon = host.IndexOf(':');
            if (colon == -1)
                host = $"{host}:{(schm == "http" || schm == "ws" ? 80 : 443)}";

            var url = $"{schm}://{host}{path}";

            Uri res;
            return !Uri.TryCreate(url, UriKind.Absolute, out res) ? null : res;
        }

#if AUTHENTICATION
        internal bool Authenticate(AuthenticationSchemes scheme,string realm, Func<IIdentity, NetworkCredential> credentialsFinder)
        {
            if (scheme == AuthenticationSchemes.Anonymous)
                return true;

            if (scheme == AuthenticationSchemes.None)
            {
                Close(HttpStatusCode.Forbidden);
                return false;
            }

            var chal = new AuthenticationChallenge(scheme, realm).ToString();

            var retry = -1;
            Func<bool> auth = null;
            auth =
              () => {
                  retry++;
                  if (retry > 99)
                  {
                      Close(HttpStatusCode.Forbidden);
                      return false;
                  }

                  var user =
              HttpUtility.CreateUser(
                _request.Headers["Authorization"],
                scheme,
                realm,
                _request.HttpMethod,
                credentialsFinder
              );

                  if (user == null || !user.Identity.IsAuthenticated)
                  {
                      SendAuthenticationChallenge(chal);
                      return auth();
                  }

                  _user = user;
                  return true;
              };

            return auth();
        }
#endif

        internal void Close()
        {
            Stream.Close();
            _tcpClient.Close();
        }

        internal void Close(HttpStatusCode code)
        {
            WebSocket.Close(HttpResponse.CreateCloseResponse(code));
        }

#if AUTHENTICATION
        internal void SendAuthenticationChallenge(string challenge)
        {
            var buff = HttpResponse.CreateUnauthorizedResponse(challenge).ToByteArray();
            _stream.Write(buff, 0, buff.Length);
            _request = HttpRequest.Read(_stream, 15000);
        }
#endif

        internal bool IsIPAddressLocal(IPAddress address)
        {
            if (address == null)
                return false;

            if (address.Equals(IPAddress.Any))
                return true;

            if (address.Equals(IPAddress.Loopback))
                return true;

            if (Socket.OSSupportsIPv6)
            {
                if (address.Equals(IPAddress.IPv6Any))
                    return true;

                if (address.Equals(IPAddress.IPv6Loopback))
                    return true;
            }

            var host = Dns.GetHostName();
            var addrs = Dns.GetHostAddresses(host);

            return addrs.Contains(address);
        }

        internal static NameValueCollection ParseQueryString(string query)
        {
            int len;
            if (query == null || (len = query.Length) == 0 || (len == 1 && query[0] == '?'))
                return new NameValueCollection(1);

            if (query[0] == '?')
                query = query.Substring(1);

            var res = new QueryStringCollection();
            var components = query.Split('&');

            foreach (var component in components)
            {
                var i = component.IndexOf('=');
                if (i > -1)
                {
                    var name = WebUtility.UrlDecode(component.Substring(0, i));
                    var val = component.Length > i + 1
                        ? WebUtility.UrlDecode(component.Substring(i + 1))
                        : string.Empty;

                    res.Add(name, val);
                }
                else
                {
                    res.Add(null, WebUtility.UrlDecode(component));
                }
            }

            return res;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _request.ToString();
        }

        #endregion
    }
}
#endif
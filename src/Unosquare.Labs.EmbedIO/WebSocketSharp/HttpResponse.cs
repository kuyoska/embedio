#if NET452
#region License
/*
 * HttpResponse.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
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

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using Unosquare.Labs.EmbedIO;

namespace WebSocketSharp
{
    internal class HttpResponse : HttpBase
    {
        #region Private Constructors

        private HttpResponse(string code, string reason, Version version, NameValueCollection headers)
          : base(version, headers)
        {
            StatusCode = code;
            Reason = reason;
        }

        #endregion

        #region Internal Constructors

        internal HttpResponse(HttpStatusCode code)
          : this(code, GetStatusDescription((int)code))
        {
        }

        internal HttpResponse(HttpStatusCode code, string reason)
          : this(((int)code).ToString(), reason, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["Server"] = "embedio/1.0";
        }

        #endregion

        #region Public Properties

        public CookieCollection Cookies => Headers.GetCookies(true);

        public bool HasConnectionClose => Headers.Contains("Connection", "close");

        public bool IsProxyAuthenticationRequired => StatusCode == "407";

        public bool IsRedirect => StatusCode == "301" || StatusCode == "302";

        public bool IsUnauthorized => StatusCode == "401";

        public bool IsWebSocketResponse
        {
            get
            {
                var headers = Headers;
                return ProtocolVersion > HttpVersion.Version10 &&
                       StatusCode == "101" &&
                       headers.Contains("Upgrade", "websocket") &&
                       headers.Contains("Connection", "Upgrade");
            }
        }

        public string Reason { get; }

        public string StatusCode { get; }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the description of the specified HTTP status <paramref name="code"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the description of the HTTP status code.
        /// </returns>
        /// <param name="code">
        /// An <see cref="int"/> that represents the HTTP status code.
        /// </param>
        public static string GetStatusDescription(int code)
        {
            switch (code)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-Uri Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 422: return "Unprocessable Entity";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "Http Version Not Supported";
                case 507: return "Insufficient Storage";
                default: return string.Empty;
            }
        }

        internal static HttpResponse CreateCloseResponse(HttpStatusCode code)
        {
            var res = new HttpResponse(code);
            res.Headers["Connection"] = "close";

            return res;
        }

        internal static HttpResponse CreateUnauthorizedResponse(string challenge)
        {
            var res = new HttpResponse(HttpStatusCode.Unauthorized);
            res.Headers["WWW-Authenticate"] = challenge;

            return res;
        }

        internal static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return res;
        }

        internal static HttpResponse Parse(string[] headerParts)
        {
            var statusLine = headerParts[0].Split(new[] { ' ' }, 3);
            if (statusLine.Length != 3)
                throw new ArgumentException("Invalid status line: " + headerParts[0]);

            var headers = new WebHeaderCollection();
            for (var i = 1; i < headerParts.Length; i++)
            {
                var parts = headerParts[i].Split(':');

                headers[parts[0]] = parts[1];
            }

            return new HttpResponse(
              statusLine[1], statusLine[2], new Version(statusLine[0].Substring(5)), headers);
        }

        internal static HttpResponse Read(Stream stream, int millisecondsTimeout)
        {
            return Read(stream, Parse, millisecondsTimeout);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the cookies.
        /// </summary>
        /// <param name="cookies">The cookies.</param>
        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var headers = Headers;

            foreach (var cookie in cookies) // TODO: .Sorted)
                headers.Add("Set-Cookie", cookie.ToString()); //.ToResponseString ());
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("HTTP/{0} {1} {2}{3}", ProtocolVersion, StatusCode, Reason, CrLf);

            var headers = Headers;
            foreach (var key in headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

            output.Append(CrLf);

            var entity = EntityBody;
            if (entity.Length > 0)
                output.Append(entity);

            return output.ToString();
        }

        #endregion
    }
}
#endif
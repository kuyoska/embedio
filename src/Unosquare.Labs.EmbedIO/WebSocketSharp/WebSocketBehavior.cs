#if NET452
#region License
/*
 * WebSocketBehavior.cs
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

using System;
using System.IO;
using System.Net;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Log;
using WebSocketSharp.Net.WebSockets;

namespace WebSocketSharp.Server
{
    /// <summary>
    /// Exposes the methods and properties used to define the behavior of a WebSocket service
    /// provided by the <see cref="WebSocketServer"/>
    /// </summary>
    /// <remarks>
    /// The WebSocketBehavior class is an abstract class.
    /// </remarks>
    public abstract class WebSocketBehavior : IWebSocketSession
    {
        #region Private Fields

        private bool _emitOnPing;
        private string _protocol;
        private WebSocket _websocket;

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketBehavior"/> class.
        /// </summary>
        protected WebSocketBehavior()
        {
            StartTime = DateTime.MaxValue;
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets or sets the cookies validator.
        /// </summary>
        /// <value>
        /// The cookies validator.
        /// </value>
        public Func<CookieCollection, CookieCollection, bool> CookiesValidator { get; set; }

        /// <summary>
        /// Gets the logging functions.
        /// </summary>
        /// <value>
        /// A <see cref="ILog"/> that provides the logging functions,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        protected ILog Log => _websocket?.Log;

        /// <summary>
        /// Gets the access to the sessions in the WebSocket service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that provides the access to the sessions,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        protected WebSocketSessionManager Sessions { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the information in a handshake request to the WebSocket service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketContext"/> instance that provides the access to the handshake request,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        public WebSocketContext Context { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="WebSocket"/> used in a session emits
        /// a <see cref="WebSocket.OnMessage"/> event when receives a Ping.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="WebSocket"/> emits a <see cref="WebSocket.OnMessage"/> event
        /// when receives a Ping; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool EmitOnPing
        {
            get
            {
                return _websocket?.EmitOnPing ?? _emitOnPing;
            }

            set
            {
                if (_websocket != null)
                {
                    _websocket.EmitOnPing = value;
                    return;
                }

                _emitOnPing = value;
            }
        }

        /// <summary>
        /// Gets the unique ID of a session.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the unique ID of the session,
        /// or <see langword="null"/> if the WebSocket connection isn't established.
        /// </value>
        public string ID { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the WebSocket service ignores
        /// the Sec-WebSocket-Extensions header included in a handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the WebSocket service ignores the extensions requested from
        /// a client; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool IgnoreExtensions { get; set; } = true;

        /// <summary>
        /// Gets or sets the delegate called to validate the Origin header included in
        /// a handshake request to the WebSocket service.
        /// </summary>
        /// <remarks>
        /// This delegate is called when the <see cref="WebSocket"/> used in a session validates
        /// the handshake request.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <c>Func&lt;string, bool&gt;</c> delegate that references the method(s) used to
        ///   validate the origin header.
        ///   </para>
        ///   <para>
        ///   <see cref="string"/> parameter passed to this delegate represents the value of
        ///   the origin header to validate if any.
        ///   </para>
        ///   <para>
        ///   This delegate should return <c>true</c> if the origin header is valid.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>, and it does nothing to validate.
        ///   </para>
        /// </value>
        public Func<string, bool> OriginValidator { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket subprotocol used in the WebSocket service.
        /// </summary>
        /// <remarks>
        /// Set operation of this property is available before the WebSocket connection has
        /// been established.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the subprotocol if any.
        ///   The default value is <see cref="String.Empty"/>.
        ///   </para>
        ///   <para>
        ///   The value to set must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">RFC 2616</see>.
        ///   </para>
        /// </value>
        public string Protocol
        {
            get
            {
                return _websocket != null ? _websocket.Protocol : (_protocol ?? String.Empty);
            }

            set
            {
                if (State != WebSocketState.Connecting)
                    return;

                if (value != null && (value.Length == 0 || !value.IsToken()))
                    return;

                _protocol = value;
            }
        }

        /// <summary>
        /// Gets the time that a session has started.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the time that the session has started,
        /// or <see cref="DateTime.MaxValue"/> if the WebSocket connection isn't established.
        /// </value>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the state of the <see cref="WebSocket"/> used in a session.
        /// </summary>
        /// <value>
        /// One of the <see cref="WebSocketState"/> enum values, indicates the state of
        /// the <see cref="WebSocket"/>.
        /// </value>
        public WebSocketState State => _websocket?.ReadyState ?? WebSocketState.Connecting;

        #endregion

        #region Private Methods

        private string CheckHandshakeRequest(WebSocketContext context)
        {
            return OriginValidator != null && !OriginValidator(context.Origin)
             ? "Includes no Origin header, or it has an invalid value."
             : CookiesValidator != null
               && !CookiesValidator(context.CookieCollection, context.WebSocket.CookieCollection)
               ? "Includes no cookie, or an invalid cookie exists."
               : null;
        }

        private void onClose(object sender, CloseEventArgs e)
        {
            if (ID == null)
                return;

            Sessions.Remove(ID);
            OnClose(e);
        }
        
        private void onOpen(object sender, EventArgs e)
        {
            ID = Sessions.Add(this);
            if (ID == null)
            {
                _websocket.Close(CloseStatusCode.Away);
                return;
            }

            StartTime = DateTime.Now;
            OnOpen();
        }

        #endregion

        #region Internal Methods

        internal void Start(WebSocketContext context, ILog logger)
        {
            if (_websocket != null)
            {
                _websocket.Log.Error("A session instance cannot be reused.");
                context.WebSocket.Close(HttpStatusCode.ServiceUnavailable);

                return;
            }

            Context = context;
            Sessions = new WebSocketSessionManager(logger);
            Sessions.Start();

            _websocket = context.WebSocket;
            _websocket.CustomHandshakeRequestChecker = CheckHandshakeRequest;
            _websocket.EmitOnPing = _emitOnPing;
            _websocket.IgnoreExtensions = IgnoreExtensions;
            _websocket.Protocol = _protocol;

            var waitTime = Sessions.WaitTime;
            if (waitTime != _websocket.WaitTime)
                _websocket.WaitTime = waitTime;

            _websocket.OnOpen += onOpen;
            _websocket.OnMessage += (s,e) => OnMessage(e);
            _websocket.OnError += (s,e) => OnError(e);
            _websocket.OnClose += onClose;

            _websocket.InternalAccept();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Calls the <see cref="OnError"/> method with the specified <paramref name="message"/> and
        /// <paramref name="exception"/>.
        /// </summary>
        /// <remarks>
        /// This method doesn't call the <see cref="OnError"/> method if <paramref name="message"/> is
        /// <see langword="null"/> or empty.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the error message.
        /// </param>
        /// <param name="exception">
        /// An <see cref="Exception"/> instance that represents the cause of the error if any.
        /// </param>
        protected void Error(string message, Exception exception)
        {
            if (!string.IsNullOrEmpty(message))
                OnError(new ErrorEventArgs(exception));
        }

        /// <summary>
        /// Called when the WebSocket connection used in a session has been closed.
        /// </summary>
        /// <param name="e">
        /// A <see cref="CloseEventArgs"/> that represents the event data passed to
        /// a <see cref="WebSocket.OnClose"/> event.
        /// </param>
        protected virtual void OnClose(CloseEventArgs e)
        {
        }

        /// <summary>
        /// Called when the <see cref="WebSocket"/> used in a session gets an error.
        /// </summary>
        /// <param name="e">
        /// A <see cref="ErrorEventArgs"/> that represents the event data passed to
        /// a <see cref="WebSocket.OnError"/> event.
        /// </param>
        protected virtual void OnError(ErrorEventArgs e)
        {
        }

        /// <summary>
        /// Called when the <see cref="WebSocket"/> used in a session receives a message.
        /// </summary>
        /// <param name="e">
        /// A <see cref="MessageEventArgs"/> that represents the event data passed to
        /// a <see cref="WebSocket.OnMessage"/> event.
        /// </param>
        protected virtual void OnMessage(MessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket connection used in a session has been established.
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        /// <summary>
        /// Sends binary <paramref name="data"/> to the client on a session.
        /// </summary>
        /// <remarks>
        /// This method is available after the WebSocket connection has been established.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        protected void Send(byte[] data)
        {
            _websocket?.Send(data);
        }

        /// <summary>
        /// Sends the specified <paramref name="file"/> as binary data to the client on a session.
        /// </summary>
        /// <remarks>
        /// This method is available after the WebSocket connection has been established.
        /// </remarks>
        /// <param name="file">
        /// A <see cref="FileInfo"/> that represents the file to send.
        /// </param>
        protected void Send(FileInfo file)
        {
            _websocket?.Send(file);
        }

        /// <summary>
        /// Sends text <paramref name="data"/> to the client on a session.
        /// </summary>
        /// <remarks>
        /// This method is available after the WebSocket connection has been established.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        protected void Send(string data)
        {
            _websocket?.Send(data);
        }

        /// <summary>
        /// Sends binary <paramref name="data"/> asynchronously to the client on a session.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method is available after the WebSocket connection has been established.
        ///   </para>
        ///   <para>
        ///   This method doesn't wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        protected void SendAsync(byte[] data, Action<bool> completed)
        {
            _websocket?.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends the specified <paramref name="file"/> as binary data asynchronously to
        /// the client on a session.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method is available after the WebSocket connection has been established.
        ///   </para>
        ///   <para>
        ///   This method doesn't wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="file">
        /// A <see cref="FileInfo"/> that represents the file to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        protected void SendAsync(FileInfo file, Action<bool> completed)
        {
            _websocket?.SendAsync(file, completed);
        }

        /// <summary>
        /// Sends text <paramref name="data"/> asynchronously to the client on a session.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method is available after the WebSocket connection has been established.
        ///   </para>
        ///   <para>
        ///   This method doesn't wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        protected void SendAsync(string data, Action<bool> completed)
        {
            _websocket?.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends binary data from the specified <see cref="Stream"/> asynchronously to
        /// the client on a session.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method is available after the WebSocket connection has been established.
        ///   </para>
        ///   <para>
        ///   This method doesn't wait for the send to be complete.
        ///   </para>
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> from which contains the binary data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that represents the number of bytes to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        protected void SendAsync(Stream stream, int length, Action<bool> completed)
        {
            _websocket?.SendAsync(stream, length, completed);
        }

        #endregion
    }
}
#endif
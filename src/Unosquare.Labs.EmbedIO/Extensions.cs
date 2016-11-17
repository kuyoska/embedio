namespace Unosquare.Labs.EmbedIO
{
    using System.Collections.Generic;
    using System.Net;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

#if NET452
    using System.Net.WebSockets;
    using WebSocketSharp;
    using System.Collections.Specialized;
    using global::WebSocketSharp;
    using System.Net.Sockets;
#endif

#if NET452
    #region License
    /*
     * Ext.cs
     *
     * Some parts of this code are derived from Mono (http://www.mono-project.com):
     * - GetStatusDescription is derived from HttpListenerResponse.cs (System.Net)
     * - IsPredefinedScheme is derived from Uri.cs (System)
     * - MaybeUri is derived from Uri.cs (System)
     *
     * The MIT License
     *
     * Copyright (c) 2001 Garrett Rooney
     * Copyright (c) 2003 Ian MacLean
     * Copyright (c) 2003 Ben Maurer
     * Copyright (c) 2003, 2005, 2009 Novell, Inc. (http://www.novell.com)
     * Copyright (c) 2009 Stephane Delcroix
     * Copyright (c) 2010-2016 sta.blockhead
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

    /// <summary>
    /// Specifies the byte order.
    /// Copyright (c) 2012-2015 sta.blockhead
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Specifies Little-endian.
        /// </summary>
        Little,
        /// <summary>
        /// Specifies Big-endian.
        /// </summary>
        Big
    }
#endif
    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    public static partial class Extensions
    {

        #region Constants

        private const string UrlEncodedContentType = "application/x-www-form-urlencoded";

        #endregion

        #region Session Management Methods

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static void DeleteSession(this HttpListenerContext context, WebServer server)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static void DeleteSession(this WebServer server, HttpListenerContext context)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the given session object.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="session">The session info.</param>
        /// <returns></returns>
        public static void DeleteSession(this WebServer server, SessionInfo session)
        {
            server.SessionModule?.DeleteSession(session);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, HttpListenerContext context)
        {
            return server.SessionModule?.GetSession(context);
        }

#if NET452
        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebSocketContext context, WebServer server)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, WebSocketContext context)
        {
            return server.SessionModule?.GetSession(context);
        }
#endif

        #endregion

        #region HTTP Request Helpers

        /// <summary>
        /// Determines whether [is web socket request] by identifying the Upgrade: websocket header.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        ///   <c>true</c> if [is web socket request] [the specified request]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWebSocketRequest(this HttpListenerRequest request)
        {
            return request.IsWebSocketRequest;
            /* // TODO: https://github.com/sta/websocket-sharp/blob/master/websocket-sharp/HttpRequest.cs#L99
            var upgradeKey = request.Headers.AllKeys.FirstOrDefault(k => k.ToLowerInvariant().Equals("upgrade"));
            if (request.Headers[upgradeKey].Equals("websocket"))
                return true;

            return false;
            */
        }

        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestPath(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath.ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the Request HTTP Verb (also called Method) of this context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            HttpVerbs verb;
            Enum.TryParse(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out verb);
            return verb;
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// If the value does not exist it returns null.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string QueryString(this HttpListenerContext context, string key)
        {
            return context.InQueryString(key) ? context.Request.QueryString[key] : null;
        }

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static bool InQueryString(this HttpListenerContext context, string key)
        {
            return context.Request.QueryString.AllKeys.Contains(key);
        }

        /// <summary>
        /// Retrieves the specified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static string RequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.HasRequestHeader(headerName) == false ? string.Empty : context.Request.Headers[headerName];
        }

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static bool HasRequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.Request.Headers[headerName] != null;
        }

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestBody(this HttpListenerContext context)
        {
            if (context.Request.HasEntityBody == false)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion

        #region HTTP Response Manipulation Methods

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(Constants.HeaderExpires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(Constants.HeaderLastModified,
                DateTime.UtcNow.ToString(Constants.BrowserTimeFormat, Constants.StandardCultureInfo));
            context.Response.AddHeader(Constants.HeaderCacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(Constants.HeaderPragma, "no-cache");
        }

        /// <summary>
        /// Sets a response statis code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        public static void Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetLeftPart(UriPartial.Authority);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);
        }

        #endregion

        #region JSON and Exception Extensions

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex)
        {
            return ex.ExceptionMessage(string.Empty);
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="priorMessage">The prior message.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex, string priorMessage)
        {
            var fullMessage = string.IsNullOrWhiteSpace(priorMessage) ? ex.Message : priorMessage + "\r\n" + ex.Message;
            if (ex.InnerException != null && string.IsNullOrWhiteSpace(ex.InnerException.Message) == false)
                return ExceptionMessage(ex.InnerException, fullMessage);

            return fullMessage;
        }


        /// <summary>
        /// Outputs a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, object data)
        {
            var jsonFormatting = Formatting.None;
#if DEBUG
            jsonFormatting = Formatting.Indented;
#endif
            var json = JsonConvert.SerializeObject(data, jsonFormatting);
            return context.JsonResponse(json);
        }

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, string json)
        {
            var buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            return true;
        }

        /// <summary>
        /// Parses the json as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            var requestBody = context.RequestBody();
            return requestBody == null ? null : JsonConvert.DeserializeObject<T>(requestBody);
        }

        /// <summary>
        /// Parses the json as a given type from the request body string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this string requestBody)
            where T : class
        {
            return requestBody == null ? null : JsonConvert.DeserializeObject<T>(requestBody);
        }


        /// <summary>
        /// Prettifies the given JSON string by adding indenting.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static string PrettifyJson(this string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        #endregion

        #region Data Parsing Methods

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [Obsolete("Use RequestFormDataDictionary methods instead")]
        public static Dictionary<string, string> RequestFormData(this HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HasEntityBody == false) return null;

            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    var stringData = reader.ReadToEnd();
                    return RequestFormData(stringData);
                }
            }
        }

        /// <summary>
        /// Returns a dictionary of KVPs from Request data
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        [Obsolete("Use RequestFormDataDictionary methods instead")]
        public static Dictionary<string, string> RequestFormData(this string requestBody)
        {
            var dictionary = ParseFormDataAsDictionary(requestBody);
            var result = new Dictionary<string, string>();
            foreach (var kvp in dictionary)
            {
                var listValue = kvp.Value as List<string>;
                if (listValue == null)
                {
                    result[kvp.Key] = kvp.Value as string;
                }
                else
                {
                    result[kvp.Key] = string.Join("\r\n", listValue.ToArray());
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a dictionary of KVPs from Request data
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
        {
            return ParseFormDataAsDictionary(requestBody);
        }

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HasEntityBody == false) return null;

            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    var stringData = reader.ReadToEnd();
                    return RequestFormDataDictionary(stringData);
                }
            }
        }

        /// <summary>
        /// Parses the form data given the request body string.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <param name="contentTypeHeader">The content type header.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">multipart/form-data Content Type parsing is not yet implemented</exception>
        private static Dictionary<string, object> ParseFormDataAsDictionary(string requestBody, string contentTypeHeader = UrlEncodedContentType)
        {
            // TODO: implement multipart/form-data parsing
            // example available here: http://stackoverflow.com/questions/5483851/manually-parse-raw-http-data-with-php

            if (contentTypeHeader.ToLowerInvariant().StartsWith("multipart/form-data"))
                throw new NotImplementedException("multipart/form-data Content Type parsing is not yet implemented");

            // verify there is data to parse
            if (string.IsNullOrWhiteSpace(requestBody)) return null;

            // define a character for KV pairs
            var kvpSeparator = new char[] { '=' };

            // Create the result object
            var resultDictionary = new Dictionary<string, object>();

            // Split the request body into key-value pair strings
            var keyValuePairStrings = requestBody.Split('&');

            foreach (var kvps in keyValuePairStrings)
            {
                // Skip KVP strings if they are empty
                if (string.IsNullOrWhiteSpace(kvps))
                    continue;

                // Split by the equals char into key values.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                var kvpsParts = kvps.Split(kvpSeparator, 2);

                // We don't want empty KVPs
                if (kvpsParts.Length == 0)
                    continue;

                // Decode the key and the value. Discard Special Characters
                var key = WebUtility.UrlDecode(kvpsParts[0]);
                if (key.IndexOf("[") > 0) key = key.Substring(0, key.IndexOf("["));

                var value = kvpsParts.Length >= 2 ? WebUtility.UrlDecode(kvpsParts[1]) : null;

                // If the result already contains the key, then turn the value of that key into a List of strings
                if (resultDictionary.ContainsKey(key))
                {
                    // Check if this key has a List value already
                    var listValue = resultDictionary[key] as List<string>;
                    if (listValue == null)
                    {
                        // if we don't have a list value for this key, then create one and add the existing item
                        var existingValue = resultDictionary[key] as string;
                        resultDictionary[key] = new List<string>();
                        listValue = resultDictionary[key] as List<string>;
                        listValue.Add(existingValue);
                    }

                    // By this time, we are sure listValue exists. Simply add the item
                    listValue.Add(value);
                }
                else
                {
                    // Simply set the key to the parsed value
                    resultDictionary[key] = value;
                }

            }

            return resultDictionary;
        }

        #endregion

        #region Hashing and Compression Methods

        /// <summary>
        /// Compresses the specified buffer stream using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static MemoryStream Compress(this Stream buffer)
        {
            buffer.Position = 0;
            var targetStream = new MemoryStream();

            using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
            {
                buffer.CopyTo(compressor);
            }

            return targetStream;
        }

        /// <summary>
        /// Computes the MD5 hash of the given stream.
        /// Do not use for large streams as this reads ALL bytes at once
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static string ComputeMd5Hash(Stream stream)
        {
            var md5 = MD5.Create();
#if NET452
            const int bufferSize = 4096;

            var readAheadBuffer = new byte[bufferSize];
            var readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

            do
            {
                var bytesRead = readAheadBytesRead;
                var buffer = readAheadBuffer;

                readAheadBuffer = new byte[bufferSize];
                readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                if (readAheadBytesRead == 0)
                    md5.TransformFinalBlock(buffer, 0, bytesRead);
                else
                    md5.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            } while (readAheadBytesRead != 0);

            return GetHashString(md5.Hash);
#else
            using (var ms = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);

                return GetHashString(md5.ComputeHash(ms.ToArray()));
            }
#endif
        }

        /// <summary>
        /// Gets a hexadecimal representation of the hash bytes
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        private static string GetHashString(byte[] hash)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Computes the MD5 hash of the given byte array
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <returns></returns>
        public static string ComputeMd5Hash(byte[] inputBytes)
        {
            var hash = MD5.Create().ComputeHash(inputBytes);
            return GetHashString(hash);
        }

        /// <summary>
        /// Computes the MD5 hash of the given input string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ComputeMd5Hash(string input)
        {
            return ComputeMd5Hash(Constants.DefaultEncoding.GetBytes(input));
        }

        #endregion

#if NET452
        #region WebSocket

        /// <summary>
        /// Retrieves a sub-array from the specified <paramref name="array"/>. A sub-array starts at
        /// the specified element position in <paramref name="array"/>.
        /// </summary>
        /// <returns>
        /// An array of T that receives a sub-array, or an empty array of T if any problems with
        /// the parameters.
        /// </returns>
        /// <param name="array">
        /// An array of T from which to retrieve a sub-array.
        /// </param>
        /// <param name="startIndex">
        /// An <see cref="int"/> that represents the zero-based starting position of
        /// a sub-array in <paramref name="array"/>.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that represents the number of elements to retrieve.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        public static T[] SubArray<T>(this T[] array, int startIndex, int length)
        {
            int len;
            if (array == null || (len = array.Length) == 0)
                return new T[0];

            if (startIndex < 0 || length <= 0 || startIndex + length > len)
                return new T[0];

            if (startIndex == 0 && length == len)
                return array;

            var subArray = new T[length];
            Array.Copy(array, startIndex, subArray, 0, length);

            return subArray;
        }

        /// <summary>
        /// Retrieves a sub-array from the specified <paramref name="array"/>. A sub-array starts at
        /// the specified element position in <paramref name="array"/>.
        /// </summary>
        /// <returns>
        /// An array of T that receives a sub-array, or an empty array of T if any problems with
        /// the parameters.
        /// </returns>
        /// <param name="array">
        /// An array of T from which to retrieve a sub-array.
        /// </param>
        /// <param name="startIndex">
        /// A <see cref="long"/> that represents the zero-based starting position of
        /// a sub-array in <paramref name="array"/>.
        /// </param>
        /// <param name="length">
        /// A <see cref="long"/> that represents the number of elements to retrieve.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        public static T[] SubArray<T>(this T[] array, long startIndex, long length)
        {
            long len;
            if (array == null || (len = array.LongLength) == 0)
                return new T[0];

            if (startIndex < 0 || length <= 0 || startIndex + length > len)
                return new T[0];

            if (startIndex == 0 && length == len)
                return array;

            var subArray = new T[length];
            Array.Copy(array, startIndex, subArray, 0, length);

            return subArray;
        }

        internal static bool IsData(this byte opcode)
        {
            return opcode == 0x1 || opcode == 0x2;
        }

        internal static bool IsData(this Opcode opcode)
        {
            return opcode == Opcode.Text || opcode == Opcode.Binary;
        }

        internal static byte[] InternalToByteArray(this ushort value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] InternalToByteArray(this ulong value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] Append(this ushort code, string reason)
        {
            var ret = code.InternalToByteArray(ByteOrder.Big);
            if (reason != null && reason.Length > 0)
            {
                var buff = new List<byte>(ret);
                buff.AddRange(Encoding.UTF8.GetBytes(reason));
                ret = buff.ToArray();
            }

            return ret;
        }

        internal static bool IsControl(this byte opcode)
        {
            return opcode > 0x7 && opcode < 0x10;
        }

        internal static byte[] ReadBytes(this Stream stream, long length, int bufferLength)
        {
            using (var dest = new MemoryStream())
            {
                try
                {
                    var buff = new byte[bufferLength];
                    var nread = 0;
                    while (length > 0)
                    {
                        if (length < bufferLength)
                            bufferLength = (int)length;

                        nread = stream.Read(buff, 0, bufferLength);
                        if (nread == 0)
                            break;

                        dest.Write(buff, 0, nread);
                        length -= nread;
                    }
                }
                catch
                {
                }

                dest.Close();
                return dest.ToArray();
            }
        }

        internal static void WriteBytes(this Stream stream, byte[] bytes, int bufferLength)
        {
            using (var input = new MemoryStream(bytes))
                input.CopyTo(stream, bufferLength);
        }

        internal static byte[] ReadBytes(this Stream stream, int length)
        {
            var buff = new byte[length];
            var offset = 0;
            try
            {
                var nread = 0;
                while (length > 0)
                {
                    nread = stream.Read(buff, offset, length);
                    if (nread == 0)
                        break;

                    offset += nread;
                    length -= nread;
                }
            }
            catch
            {
            }

            return buff.SubArray(0, offset);
        }

        private static readonly int _retry = 5;

        internal static void ReadBytesAsync(
          this Stream stream, int length, Action<byte[]> completed, Action<Exception> error
        )
        {
            var buff = new byte[length];
            var offset = 0;
            var retry = 0;

            AsyncCallback callback = null;
            callback =
              ar =>
              {
                  try
                  {
                      var nread = stream.EndRead(ar);
                      if (nread == 0 && retry < _retry)
                      {
                          retry++;
                          stream.BeginRead(buff, offset, length, callback, null);

                          return;
                      }

                      if (nread == 0 || nread == length)
                      {
                          if (completed != null)
                              completed(buff.SubArray(0, offset + nread));

                          return;
                      }

                      retry = 0;

                      offset += nread;
                      length -= nread;

                      stream.BeginRead(buff, offset, length, callback, null);
                  }
                  catch (Exception ex)
                  {
                      if (error != null)
                          error(ex);
                  }
              };

            try
            {
                stream.BeginRead(buff, offset, length, callback, null);
            }
            catch (Exception ex)
            {
                if (error != null)
                    error(ex);
            }
        }

        internal static void ReadBytesAsync(
          this Stream stream,
          long length,
          int bufferLength,
          Action<byte[]> completed,
          Action<Exception> error
        )
        {
            var dest = new MemoryStream();
            var buff = new byte[bufferLength];
            var retry = 0;

            Action<long> read = null;
            read =
              len =>
              {
                  if (len < bufferLength)
                      bufferLength = (int)len;

                  stream.BeginRead(
              buff,
              0,
              bufferLength,
              ar =>
              {
                  try
                  {
                      var nread = stream.EndRead(ar);
                      if (nread > 0)
                          dest.Write(buff, 0, nread);

                      if (nread == 0 && retry < _retry)
                      {
                          retry++;
                          read(len);

                          return;
                      }

                      if (nread == 0 || nread == len)
                      {
                          if (completed != null)
                          {
                              dest.Close();
                              completed(dest.ToArray());
                          }

                          dest.Dispose();
                          return;
                      }

                      retry = 0;
                      read(len - nread);
                  }
                  catch (Exception ex)
                  {
                      dest.Dispose();
                      if (error != null)
                          error(ex);
                  }
              },
              null
            );
              };

            try
            {
                read(length);
            }
            catch (Exception ex)
            {
                dest.Dispose();
                if (error != null)
                    error(ex);
            }
        }

        internal static bool IsSupported(this byte opcode)
        {
            return Enum.IsDefined(typeof(Opcode), opcode);
        }

        internal static ulong ToUInt64(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt64(source.ToHostOrder(sourceOrder), 0);
        }

        internal static string UTF8Decode(this byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsReserved(this ushort code)
        {
            return code == (ushort)CloseStatusCode.Undefined ||
                   code == (ushort)CloseStatusCode.NoStatus ||
                   code == (ushort)CloseStatusCode.Abnormal ||
                   code == (ushort)CloseStatusCode.TlsHandshakeFailure;
        }

        internal static bool IsReserved(this CloseStatusCode code)
        {
            return code == CloseStatusCode.Undefined ||
                   code == CloseStatusCode.NoStatus ||
                   code == CloseStatusCode.Abnormal ||
                   code == CloseStatusCode.TlsHandshakeFailure;
        }

        internal static ushort ToUInt16(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt16(source.ToHostOrder(sourceOrder), 0);
        }

        /// <summary>
        /// Converts the order of the specified array of <see cref="byte"/> to the host byte order.
        /// </summary>
        /// <returns>
        /// An array of <see cref="byte"/> converted from <paramref name="source"/>.
        /// </returns>
        /// <param name="source">
        /// An array of <see cref="byte"/> to convert.
        /// </param>
        /// <param name="sourceOrder">
        /// One of the <see cref="ByteOrder"/> enum values, specifies the byte order of
        /// <paramref name="source"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] ToHostOrder(this byte[] source, ByteOrder sourceOrder)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ByteOrder"/> is host (this computer
        /// architecture) byte order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="order"/> is host byte order; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="order">
        /// One of the <see cref="ByteOrder"/> enum values, to test.
        /// </param>
        public static bool IsHostOrder(this ByteOrder order)
        {
            // true: !(true ^ true) or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == ByteOrder.Little));
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> is a predefined scheme.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is a predefined scheme; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        public static bool IsPredefinedScheme(this string value)
        {
            if (value == null || value.Length < 2)
                return false;

            var c = value[0];
            if (c == 'h')
                return value == "http" || value == "https";

            if (c == 'w')
                return value == "ws" || value == "wss";

            if (c == 'f')
                return value == "file" || value == "ftp";

            if (c == 'n')
            {
                c = value[1];
                return c == 'e'
                       ? value == "news" || value == "net.pipe" || value == "net.tcp"
                       : value == "nntp";
            }

            return (c == 'g' && value == "gopher") || (c == 'm' && value == "mailto");
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> is a URI string.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> may be a URI string; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        public static bool MaybeUri(this string value)
        {
            if (value == null || value.Length == 0)
                return false;

            var idx = value.IndexOf(':');
            if (idx == -1)
                return false;

            if (idx >= 10)
                return false;

            return value.Substring(0, idx).IsPredefinedScheme();
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to a <see cref="Uri"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Uri"/> converted from <paramref name="uriString"/>,
        /// or <see langword="null"/> if <paramref name="uriString"/> isn't successfully converted.
        /// </returns>
        /// <param name="uriString">
        /// A <see cref="string"/> to convert.
        /// </param>
        public static Uri ToUri(this string uriString)
        {
            Uri ret;
            Uri.TryCreate(
              uriString, uriString.MaybeUri() ? UriKind.Absolute : UriKind.Relative, out ret);

            return ret;
        }

        /// <summary>
        /// Tries to create a <see cref="Uri"/> for WebSocket with
        /// the specified <paramref name="uriString"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if a <see cref="Uri"/> is successfully created; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="uriString">
        /// A <see cref="string"/> that represents a WebSocket URL to try.
        /// </param>
        /// <param name="result">
        /// When this method returns, a <see cref="Uri"/> that represents a WebSocket URL,
        /// or <see langword="null"/> if <paramref name="uriString"/> is invalid.
        /// </param>
        /// <param name="message">
        /// When this method returns, a <see cref="string"/> that represents an error message,
        /// or <see cref="String.Empty"/> if <paramref name="uriString"/> is valid.
        /// </param>
        internal static bool TryCreateWebSocketUri(
          this string uriString, out Uri result, out string message)
        {
            result = null;

            var uri = uriString.ToUri();
            if (uri == null)
            {
                message = "An invalid URI string: " + uriString;
                return false;
            }

            if (!uri.IsAbsoluteUri)
            {
                message = "Not an absolute URI: " + uriString;
                return false;
            }

            var schm = uri.Scheme;
            if (!(schm == "ws" || schm == "wss"))
            {
                message = "The scheme part isn't 'ws' or 'wss': " + uriString;
                return false;
            }

            if (uri.Fragment.Length > 0)
            {
                message = "Includes the fragment component: " + uriString;
                return false;
            }

            var port = uri.Port;
            if (port == 0)
            {
                message = "The port part is zero: " + uriString;
                return false;
            }

            result = port != -1
                     ? uri
                     : new Uri(
                         String.Format(
                           "{0}://{1}:{2}{3}",
                           schm,
                           uri.Host,
                           schm == "ws" ? 80 : 443,
                           uri.PathAndQuery));

            message = String.Empty;
            return true;
        }

        private const string _tspecials = "()<>@,;:\\\"/[]?={} \t";

        internal static bool IsToken(this string value)
        {
            foreach (var c in value)
                if (c < 0x20 || c >= 0x7f || _tspecials.Contains(c))
                    return false;

            return true;
        }

        internal static string CheckIfValidProtocols(this string[] protocols)
        {
            return protocols.Any(protocol => protocol == null || protocol.Length == 0 || !protocol.IsToken())
                   ? "Contains an invalid value."
                   : protocols.ContainsTwice()
                     ? "Contains a value twice."
                     : null;
        }

        /// <summary>
        /// Gets the collection of the HTTP cookies from the specified HTTP <paramref name="headers"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="CookieCollection"/> that receives a collection of the HTTP cookies.
        /// </returns>
        /// <param name="headers">
        /// A <see cref="NameValueCollection"/> that contains a collection of the HTTP headers.
        /// </param>
        /// <param name="response">
        /// <c>true</c> if <paramref name="headers"/> is a collection of the response headers;
        /// otherwise, <c>false</c>.
        /// </param>
        public static CookieCollection GetCookies(this NameValueCollection headers, bool response)
        {
            var name = response ? "Set-Cookie" : "Cookie";
            return headers != null && headers.AllKeys.Contains(name)
                   ? CookieCollectionParser.Parse(headers[name], response)
                   : new CookieCollection();
        }

        /// <summary>
        /// Gets the description of the specified HTTP status <paramref name="code"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the description of the HTTP status code.
        /// </returns>
        /// <param name="code">
        /// One of <see cref="HttpStatusCode"/> enum values, indicates the HTTP status code.
        /// </param>
        public static string GetDescription(this HttpStatusCode code)
        {
            return ((int)code).GetStatusDescription();
        }

        /// <summary>
        /// Gets the description of the specified HTTP status <paramref name="code"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the description of the HTTP status code.
        /// </returns>
        /// <param name="code">
        /// An <see cref="int"/> that represents the HTTP status code.
        /// </param>
        public static string GetStatusDescription(this int code)
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
            }

            return String.Empty;
        }

        internal static bool ContainsTwice(this string[] values)
        {
            var len = values.Length;

            Func<int, bool> contains = null;
            contains = idx =>
            {
                if (idx < len - 1)
                {
                    for (var i = idx + 1; i < len; i++)
                        if (values[i] == values[idx])
                            return true;

                    return contains(++idx);
                }

                return false;
            };

            return contains(0);
        }

        internal static bool CheckWaitTime(this TimeSpan time, out string message)
        {
            message = null;

            if (time <= TimeSpan.Zero)
            {
                message = "A wait time is zero or less.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts the specified <paramref name="array"/> to a <see cref="string"/> that
        /// concatenates the each element of <paramref name="array"/> across the specified
        /// <paramref name="separator"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> converted from <paramref name="array"/>,
        /// or <see cref="String.Empty"/> if <paramref name="array"/> is empty.
        /// </returns>
        /// <param name="array">
        /// An array of T to convert.
        /// </param>
        /// <param name="separator">
        /// A <see cref="string"/> that represents the separator string.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        public static string ToString<T>(this T[] array, string separator)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            var len = array.Length;
            if (len == 0)
                return String.Empty;

            if (separator == null)
                separator = String.Empty;

            var buff = new StringBuilder(64);
            (len - 1).Times(i => buff.AppendFormat("{0}{1}", array[i].ToString(), separator));

            buff.Append(array[len - 1].ToString());
            return buff.ToString();
        }

        /// <summary>
        /// Executes the specified <c>Action&lt;int&gt;</c> delegate <paramref name="n"/> times.
        /// </summary>
        /// <param name="n">
        /// An <see cref="int"/> is the number of times to execute.
        /// </param>
        /// <param name="action">
        /// An <c>Action&lt;int&gt;</c> delegate that references the method(s) to execute.
        /// An <see cref="int"/> parameter to pass to the method(s) is the zero-based count of
        /// iteration.
        /// </param>
        public static void Times(this int n, Action<int> action)
        {
            if (n > 0 && action != null)
                for (int i = 0; i < n; i++)
                    action(i);
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> delegate <paramref name="n"/> times.
        /// </summary>
        /// <param name="n">
        /// A <see cref="long"/> is the number of times to execute.
        /// </param>
        /// <param name="action">
        /// An <see cref="Action"/> delegate that references the method(s) to execute.
        /// </param>
        public static void Times(this long n, Action action)
        {
            if (n > 0 && action != null)
                ((ulong)n).times(action);
        }

        private static void times(this ulong n, Action action)
        {
            for (ulong i = 0; i < n; i++)
                action();
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> delegate <paramref name="n"/> times.
        /// </summary>
        /// <param name="n">
        /// An <see cref="int"/> is the number of times to execute.
        /// </param>
        /// <param name="action">
        /// An <see cref="Action"/> delegate that references the method(s) to execute.
        /// </param>
        public static void Times(this int n, Action action)
        {
            if (n > 0 && action != null)
                ((ulong)n).times(action);
        }

        internal static string ToExtensionString(
      this CompressionMethod method, params string[] parameters)
        {
            if (method == CompressionMethod.None)
                return String.Empty;

            var m = String.Format("permessage-{0}", method.ToString().ToLower());
            if (parameters == null || parameters.Length == 0)
                return m;

            return String.Format("{0}; {1}", m, parameters.ToString("; "));
        }

        internal static bool IsText(this string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                    return false;

                if (c == 0x7f)
                    return false;

                if (c == '\n' && ++i < len)
                {
                    c = value[i];
                    if (!" \t".Contains(c))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="NameValueCollection"/> contains the entry with
        /// the specified both <paramref name="name"/> and <paramref name="value"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="collection"/> contains the entry with both
        /// <paramref name="name"/> and <paramref name="value"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="collection">
        /// A <see cref="NameValueCollection"/> to test.
        /// </param>
        /// <param name="name">
        /// A <see cref="string"/> that represents the key of the entry to find.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the entry to find.
        /// </param>
        public static bool Contains(this NameValueCollection collection, string name, string value)
        {
            if (collection == null || collection.Count == 0)
                return false;

            var vals = collection[name];
            if (vals == null)
                return false;

            foreach (var val in vals.Split(','))
                if (val.Trim().Equals(value, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> contains any of characters in
        /// the specified array of <see cref="char"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> contains any of <paramref name="chars"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        /// <param name="chars">
        /// An array of <see cref="char"/> that contains characters to find.
        /// </param>
        public static bool Contains(this string value, params char[] chars)
        {
            return chars == null || chars.Length == 0
                   ? true
                   : value == null || value.Length == 0
                     ? false
                     : value.IndexOfAny(chars) > -1;
        }


        private static byte[] decompress(this byte[] data)
        {
            if (data.LongLength == 0)
                return data;

            using (var input = new MemoryStream(data))
                return input.decompressToArray();
        }

        private static MemoryStream decompress(this Stream stream)
        {
            var output = new MemoryStream();
            if (stream.Length == 0)
                return output;

            stream.Position = 0;
            using (var ds = new DeflateStream(stream, CompressionMode.Decompress, true))
            {
                ds.CopyTo(output, 1024);
                output.Position = 0;

                return output;
            }
        }

        private static byte[] compress(this byte[] data)
        {
            if (data.LongLength == 0)
                //return new byte[] { 0x00, 0x00, 0x00, 0xff, 0xff };
                return data;

            using (var input = new MemoryStream(data))
                return input.compressToArray();
        }

        private static readonly byte[] _last = new byte[] { 0x00 };

        private static MemoryStream compress(this Stream stream)
        {
            var output = new MemoryStream();
            if (stream.Length == 0)
                return output;

            stream.Position = 0;
            using (var ds = new DeflateStream(output, CompressionMode.Compress, true))
            {
                stream.CopyTo(ds, 1024);
                ds.Close(); // BFINAL set to 1.
                output.Write(_last, 0, 1);
                output.Position = 0;

                return output;
            }
        }

        internal static Stream Compress(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.compress()
                   : stream;
        }

        private static byte[] compressToArray(this Stream stream)
        {
            using (var output = stream.compress())
            {
                output.Close();
                return output.ToArray();
            }
        }
        internal static byte[] Compress(this byte[] data, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? data.compress()
                   : data;
        }

        private static byte[] decompressToArray(this Stream stream)
        {
            using (var output = stream.decompress())
            {
                output.Close();
                return output.ToArray();
            }
        }


        internal static byte[] Decompress(this byte[] data, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? data.decompress()
                   : data;
        }

        internal static Stream Decompress(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.decompress()
                   : stream;
        }
        internal static bool IsCompressionExtension(this string value, CompressionMethod method)
        {
            return value.StartsWith(method.ToExtensionString());
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            using (var output = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(output, 1024);
                output.Close();

                return output.ToArray();
            }
        }

        internal static byte[] DecompressToArray(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.decompressToArray()
                   : stream.ToByteArray();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ushort"/> is in the allowable range of
        /// the WebSocket close status code.
        /// </summary>
        /// <remarks>
        /// Not allowable ranges are the following:
        ///   <list type="bullet">
        ///     <item>
        ///       <term>
        ///       Numbers in the range 0-999 are not used.
        ///       </term>
        ///     </item>
        ///     <item>
        ///       <term>
        ///       Numbers greater than 4999 are out of the reserved close status code ranges.
        ///       </term>
        ///     </item>
        ///   </list>
        /// </remarks>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is in the allowable range of the WebSocket
        /// close status code; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="ushort"/> to test.
        /// </param>
        public static bool IsCloseStatusCode(this ushort value)
        {
            return value > 999 && value < 5000;
        }
        internal static string GetMessage(this CloseStatusCode code)
        {
            return code == CloseStatusCode.ProtocolError
                   ? "A WebSocket protocol error has occurred."
                   : code == CloseStatusCode.UnsupportedData
                     ? "Unsupported data has been received."
                     : code == CloseStatusCode.Abnormal
                       ? "An exception has occurred."
                       : code == CloseStatusCode.InvalidData
                         ? "Invalid data has been received."
                         : code == CloseStatusCode.PolicyViolation
                           ? "A policy violation has occurred."
                           : code == CloseStatusCode.TooBig
                             ? "A too big message has been received."
                             : code == CloseStatusCode.MandatoryExtension
                               ? "WebSocket client didn't receive expected extension(s)."
                               : code == CloseStatusCode.ServerError
                                 ? "WebSocket server got an internal error."
                                 : code == CloseStatusCode.TlsHandshakeFailure
                                   ? "An error has occurred during a TLS handshake."
                                   : String.Empty;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IPAddress"/> represents
        /// a local IP address.
        /// </summary>
        /// <remarks>
        /// This local means NOT REMOTE for the current host.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if <paramref name="address"/> represents a local IP address;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="address">
        /// A <see cref="IPAddress"/> to test.
        /// </param>
        public static bool IsLocal(this IPAddress address)
        {
            if (address == null)
                return false;

            if (address.Equals(System.Net.IPAddress.Any))
                return true;

            if (address.Equals(System.Net.IPAddress.Loopback))
                return true;

            if (Socket.OSSupportsIPv6)
            {
                if (address.Equals(IPAddress.IPv6Any))
                    return true;

                if (address.Equals(System.Net.IPAddress.IPv6Loopback))
                    return true;
            }

            var host = Dns.GetHostName();
            var addrs = Dns.GetHostAddresses(host);
            foreach (var addr in addrs)
            {
                if (address.Equals(addr))
                    return true;
            }

            return false;
        }

        internal static string TrimEndSlash(this string value)
        {
            value = value.TrimEnd('/');
            return value.Length > 0 ? value : "/";
        }
        #endregion
#endif
    }
}
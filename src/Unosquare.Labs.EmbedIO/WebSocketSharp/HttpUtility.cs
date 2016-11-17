#if NET452
#region License
/*
 * HttpUtility.cs
 *
 * This code is derived from System.Net.HttpUtility.cs of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005-2009 Novell, Inc. (http://www.novell.com)
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

#region Authors
/*
 * Authors:
 * - Patrik Torstensson <Patrik.Torstensson@labs2.com>
 * - Wictor Wilén (decode/encode functions) <wictor@ibizkit.se>
 * - Tim Coleman <tim@timcoleman.com>
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Unosquare.Labs.EmbedIO;

namespace WebSocketSharp.Net
{
    internal sealed class HttpUtility
    {
        #region Private Methods

        private static int getChar(byte[] bytes, int offset, int length)
        {
            var val = 0;
            var end = length + offset;
            for (var i = offset; i < end; i++)
            {
                var current = GetInt(bytes[i]);
                if (current == -1)
                    return -1;

                val = (val << 4) + current;
            }

            return val;
        }

        private static int getChar(string s, int offset, int length)
        {
            var val = 0;
            var end = length + offset;
            for (var i = offset; i < end; i++)
            {
                var c = s[i];
                if (c > 127)
                    return -1;

                var current = GetInt((byte)c);
                if (current == -1)
                    return -1;

                val = (val << 4) + current;
            }

            return val;
        }
        
        public static int GetInt(byte b)
        {
            var c = (char)b;
            return c >= '0' && c <= '9'
                   ? c - '0'
                   : c >= 'a' && c <= 'f'
                     ? c - 'a' + 10
                     : c >= 'A' && c <= 'F'
                       ? c - 'A' + 10
                       : -1;
        }
        
        private static void writeCharBytes(char c, IList buffer, Encoding encoding)
        {
            if (c > 255)
            {
                foreach (var b in encoding.GetBytes(new[] { c }))
                    buffer.Add(b);

                return;
            }

            buffer.Add((byte)c);
        }

        #endregion

        #region Internal Methods
        
#if AUTHENTICATION
        internal static IPrincipal CreateUser(
          string response,
          AuthenticationSchemes scheme,
          string realm,
          string method,
          Func<IIdentity, NetworkCredential> credentialsFinder
        )
        {
            if (response == null || response.Length == 0)
                return null;

            if (credentialsFinder == null)
                return null;

            if (!(scheme == AuthenticationSchemes.Basic || scheme == AuthenticationSchemes.Digest))
                return null;

            if (scheme == AuthenticationSchemes.Digest)
            {
                if (realm == null || realm.Length == 0)
                    return null;

                if (method == null || method.Length == 0)
                    return null;
            }

            if (!response.StartsWith(scheme.ToString(), StringComparison.OrdinalIgnoreCase))
                return null;

            var res = AuthenticationResponse.Parse(response);
            if (res == null)
                return null;

            var id = res.ToIdentity();
            if (id == null)
                return null;

            NetworkCredential cred = null;
            try
            {
                cred = credentialsFinder(id);
            }
            catch
            {
            }

            if (cred == null)
                return null;

            if (scheme == AuthenticationSchemes.Basic
                && ((HttpBasicIdentity)id).Password != cred.Password
            )
            {
                return null;
            }

            if (scheme == AuthenticationSchemes.Digest
                && !((HttpDigestIdentity)id).IsValid(cred.Password, realm, method, null)
            )
            {
                return null;
            }

            return new GenericPrincipal(id, cred.Roles);
        }
#endif
        
        internal static NameValueCollection InternalParseQueryString(string query, Encoding encoding)
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
                    var name = UrlDecode(component.Substring(0, i), encoding);
                    var val = component.Length > i + 1
                              ? UrlDecode(component.Substring(i + 1), encoding)
                              : String.Empty;

                    res.Add(name, val);
                }
                else
                {
                    res.Add(null, UrlDecode(component, encoding));
                }
            }

            return res;
        }
        
#endregion

#region Public Methods
        
        public static string UrlDecode(string s, Encoding encoding = null)
        {
            if (s == null || s.Length == 0 || !s.Contains('%', '+'))
                return s;

            if (encoding == null)
                encoding = Encoding.UTF8;

            var buff = new List<byte>();
            var len = s.Length;

            for (var i = 0; i < len; i++)
            {
                var c = s[i];
                if (c == '%' && i + 2 < len && s[i + 1] != '%')
                {
                    int xchar;
                    if (s[i + 1] == 'u' && i + 5 < len)
                    {
                        // Unicode hex sequence.
                        xchar = getChar(s, i + 2, 4);
                        if (xchar != -1)
                        {
                            writeCharBytes((char)xchar, buff, encoding);
                            i += 5;
                        }
                        else
                        {
                            writeCharBytes('%', buff, encoding);
                        }
                    }
                    else if ((xchar = getChar(s, i + 1, 2)) != -1)
                    {
                        writeCharBytes((char)xchar, buff, encoding);
                        i += 2;
                    }
                    else
                    {
                        writeCharBytes('%', buff, encoding);
                    }

                    continue;
                }

                if (c == '+')
                {
                    writeCharBytes(' ', buff, encoding);
                    continue;
                }

                writeCharBytes(c, buff, encoding);
            }

            return encoding.GetString(buff.ToArray());
        }
#endregion
    }
}
#endif
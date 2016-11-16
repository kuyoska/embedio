#if NET452
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.Labs.EmbedIO.WebSocketSharp
{
    public static class CookieCollectionParser
    {
        private static string[] splitCookieHeaderValue(string value)
        {
            return new List<string>(value.SplitHeaderValue(',', ';')).ToArray();
        }

        internal static IEnumerable<string> SplitHeaderValue(
          this string value, params char[] separators)
        {
            var len = value.Length;
            var seps = new string(separators);

            var buff = new StringBuilder(32);
            var escaped = false;
            var quoted = false;

            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c == '"')
                {
                    if (escaped)
                        escaped = !escaped;
                    else
                        quoted = !quoted;
                }
                else if (c == '\\')
                {
                    if (i < len - 1 && value[i + 1] == '"')
                        escaped = true;
                }
                else if (seps.Contains(c))
                {
                    if (!quoted)
                    {
                        yield return buff.ToString();
                        buff.Length = 0;

                        continue;
                    }
                }
                else
                {
                }

                buff.Append(c);
            }

            if (buff.Length > 0)
                yield return buff.ToString();
        }

        internal static string Unquote(this string value)
        {
            var start = value.IndexOf('"');
            if (start < 0)
                return value;

            var end = value.LastIndexOf('"');
            var len = end - start - 1;

            return len < 0
                   ? value
                   : len == 0
                     ? String.Empty
                     : value.Substring(start + 1, len).Replace("\\\"", "\"");
        }

        internal static string GetValue(this string nameAndValue, char separator, bool unquote)
        {
            var idx = nameAndValue.IndexOf(separator);
            if (idx < 0 || idx == nameAndValue.Length - 1)
                return null;

            var val = nameAndValue.Substring(idx + 1).Trim();
            return unquote ? val.Unquote() : val;
        }

        internal static string GetValue(this string nameAndValue, char separator)
        {
            var idx = nameAndValue.IndexOf(separator);
            return idx > -1 && idx < nameAndValue.Length - 1
                   ? nameAndValue.Substring(idx + 1).Trim()
                   : null;
        }

        private static CookieCollection parseRequest(string value)
        {
            var cookies = new CookieCollection();

            Cookie cookie = null;
            var ver = 0;
            var pairs = splitCookieHeaderValue(value);
            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                if (pair.StartsWith("$version", StringComparison.InvariantCultureIgnoreCase))
                {
                    ver = Int32.Parse(pair.GetValue('=', true));
                }
                else if (pair.StartsWith("$path", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Path = pair.GetValue('=');
                }
                else if (pair.StartsWith("$domain", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Domain = pair.GetValue('=');
                }
                else if (pair.StartsWith("$port", StringComparison.InvariantCultureIgnoreCase))
                {
                    var port = pair.Equals("$port", StringComparison.InvariantCultureIgnoreCase)
                               ? "\"\""
                               : pair.GetValue('=');

                    if (cookie != null)
                        cookie.Port = port;
                }
                else
                {
                    if (cookie != null)
                        cookies.Add(cookie);

                    string name;
                    string val = String.Empty;

                    var pos = pair.IndexOf('=');
                    if (pos == -1)
                    {
                        name = pair;
                    }
                    else if (pos == pair.Length - 1)
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                    }
                    else
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                        val = pair.Substring(pos + 1).TrimStart(' ');
                    }

                    cookie = new Cookie(name, val);
                    if (ver != 0)
                        cookie.Version = ver;
                }
            }

            if (cookie != null)
                cookies.Add(cookie);

            return cookies;
        }

        private static CookieCollection parseResponse(string value)
        {
            var cookies = new CookieCollection();

            Cookie cookie = null;
            var pairs = splitCookieHeaderValue(value);
            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                if (pair.StartsWith("version", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Version = Int32.Parse(pair.GetValue('=', true));
                }
                else if (pair.StartsWith("expires", StringComparison.InvariantCultureIgnoreCase))
                {
                    var buff = new StringBuilder(pair.GetValue('='), 32);
                    if (i < pairs.Length - 1)
                        buff.AppendFormat(", {0}", pairs[++i].Trim());

                    DateTime expires;
                    if (!DateTime.TryParseExact(
                      buff.ToString(),
                      new[] { "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'", "r" },
                      CultureInfo.CreateSpecificCulture("en-US"),
                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                      out expires))
                        expires = DateTime.Now;

                    if (cookie != null && cookie.Expires == DateTime.MinValue)
                        cookie.Expires = expires.ToLocalTime();
                }
                else if (pair.StartsWith("max-age", StringComparison.InvariantCultureIgnoreCase))
                {
                    var max = Int32.Parse(pair.GetValue('=', true));
                    var expires = DateTime.Now.AddSeconds((double)max);
                    if (cookie != null)
                        cookie.Expires = expires;
                }
                else if (pair.StartsWith("path", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Path = pair.GetValue('=');
                }
                else if (pair.StartsWith("domain", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Domain = pair.GetValue('=');
                }
                else if (pair.StartsWith("port", StringComparison.InvariantCultureIgnoreCase))
                {
                    var port = pair.Equals("port", StringComparison.InvariantCultureIgnoreCase)
                               ? "\"\""
                               : pair.GetValue('=');

                    if (cookie != null)
                        cookie.Port = port;
                }
                else if (pair.StartsWith("comment", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Comment = pair.GetValue('=').UrlDecode();
                }
                else if (pair.StartsWith("commenturl", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.CommentUri = pair.GetValue('=', true).ToUri();
                }
                else if (pair.StartsWith("discard", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Discard = true;
                }
                else if (pair.StartsWith("secure", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.Secure = true;
                }
                else if (pair.StartsWith("httponly", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (cookie != null)
                        cookie.HttpOnly = true;
                }
                else
                {
                    if (cookie != null)
                        cookies.Add(cookie);

                    string name;
                    string val = String.Empty;

                    var pos = pair.IndexOf('=');
                    if (pos == -1)
                    {
                        name = pair;
                    }
                    else if (pos == pair.Length - 1)
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                    }
                    else
                    {
                        name = pair.Substring(0, pos).TrimEnd(' ');
                        val = pair.Substring(pos + 1).TrimStart(' ');
                    }

                    cookie = new Cookie(name, val);
                }
            }

            if (cookie != null)
                cookies.Add(cookie);

            return cookies;
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

                var current = getInt((byte)c);
                if (current == -1)
                    return -1;

                val = (val << 4) + current;
            }

            return val;
        }

        private static int getInt(byte b)
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

        public static string UrlDecode(string s, Encoding encoding)
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

        /// <summary>
        /// URL-decodes the specified <see cref="string"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that receives the decoded string,
        /// or the <paramref name="value"/> if it's <see langword="null"/> or empty.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to decode.
        /// </param>
        public static string UrlDecode(this string value)
        {
            return value != null && value.Length > 0 ? UrlDecode(value, Encoding.UTF8) : value;
        }

        internal static CookieCollection Parse(string value, bool response)
        {
            return response
                   ? parseResponse(value)
                   : parseRequest(value);
        }

    }
}
#endif
////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  hanHttpClient
// Description: C# HTTP客户端类
// Class URI:   https://github.com/fawdlstty/some_tools
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Aug 10, 2018
// remarks:     使用此类需要先添加引用System.Web
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;

namespace hanHttpLib {
	public enum hanUserAgent { Android, Chrome, Edge, Ie }
	public enum hanContentType { UrlEncode, Json, Xml, FormData }

	public class hanHttpClient {
		public hanHttpClient (hanUserAgent ua = hanUserAgent.Chrome) {
			m_ua = ua;
		}

		// 添加cookie
		public void add_cookie (string name, string value, string path = "/") {
			m_cookies.Add (new Cookie (name, value, path));
		}

		// 获取cookie
		public (string, string) get_cookie (string name) {
			return (m_cookies [name]?.Name ?? "", m_cookies [name]?.Value ?? "");
		}

		// 删除cookie
		public void del_cookie (string name) {
			var cookies = new CookieCollection ();
			foreach (Cookie cookie in m_cookies) {
				if (cookie.Name != name)
					cookies.Add (cookie);
			}
			m_cookies = cookies;
		}

		// post请求
		public byte [] post (string url, hanContentType ct = hanContentType.UrlEncode, params (string, string) [] param) {
			string boundary = $"----hanHttpClient_{System.Guid.NewGuid ().ToString ("N").Substring (0, 8)}", crlf = "\r\n";
			string content_type = new Dictionary<hanContentType, string> {
				[hanContentType.UrlEncode] = "application/x-www-form-urlencoded",
				[hanContentType.Json] = "application/json",
				[hanContentType.Xml] = "text/xml",
				[hanContentType.FormData] = $"multipart/form-data; boundary={boundary}"
			} [ct];
			Func<string, string> _my_encode = (s) => s.Replace ("\\", "\\\\").Replace ("\"", "\\\"");
			StringBuilder sb = new StringBuilder ();
			foreach (var (key, value) in param) {
				if (ct == hanContentType.UrlEncode) {
					sb.Append (sb.Length == 0 ? "" : "&");
					sb.Append ($"{HttpUtility.UrlEncode (key)}={HttpUtility.UrlEncode (value)}");
				} else if (ct == hanContentType.Json) {
					sb.Append (sb.Length == 0 ? "{" : ",");
					sb.Append ($@"""{_my_encode (key)}"":""{_my_encode (value)}""");
				} else if (ct == hanContentType.Xml) {
					sb.Append (sb.Length == 0 ? @"<?xml version=""1.0"" encoding=""UTF-8""?>" : "");
					sb.Append ($"<{HttpUtility.UrlEncode (key)}>{HttpUtility.UrlEncode (value)}</{HttpUtility.UrlEncode (key)}>");
				} else if (ct == hanContentType.FormData) {
					sb.Append ($@"--{boundary}{crlf}Content-Disposition: form-data; name=""{_my_encode (key)}""{crlf}{crlf}{value}{crlf}");
				}
			}
			sb.Append (ct == hanContentType.Json ? "}" : (ct == hanContentType.FormData ? $"--{boundary}--" : ""));
			byte [] param_data = Encoding.UTF8.GetBytes (sb.ToString ());
			return request_impl (url, "POST", param_data, content_type);
		}

		// get请求
		public byte [] get (string url) {
			return request_impl (url, "GET");
		}

		private hanUserAgent m_ua;
		private CookieCollection m_cookies = new CookieCollection ();
		public static int m_timeout_ms = 10000;

		// 清除 utf8 bom
		private static byte [] clear_bom (byte [] data) {
			if (data.Length > 3 && data [0] == '\xef' && data [1] == '\xbb' && data [2] == '\xbf') {
				using (var ms = new MemoryStream ()) {
					ms.Write (data, 3, data.Length - 3);
					data = ms.ToArray ();
				}
			}
			return data;
		}

		// 请求的实现
		private byte [] request_impl (string url, string method, byte [] param_data = null, string content_type = "") {
			// 生成请求
			var uri = new Uri (url); // url.IndexOf ('/', 8) >= 0 ? url.Substring (0, url.IndexOf ('/', 8)) : url
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (uri);
			req.Method = method;
			req.AllowAutoRedirect = true;
			req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
			req.Headers ["Accept-Encoding"] = "gzip, deflate";
			req.Headers ["Accept-Language"] = "zh-CN,zh;q=0.8";
			req.Headers ["Cache-Control"] = "max-age=0";
			req.KeepAlive = false;
			req.UserAgent = new Dictionary<hanUserAgent, string> {
				[hanUserAgent.Android] = "Mozilla/5.0 (Linux; Android 8.0; Pixel 2 Build/OPD3.170816.012) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.75 Mobile Safari/537.36",
				[hanUserAgent.Chrome] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.75 Safari/537.36",
				[hanUserAgent.Edge] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/17.17134",
				[hanUserAgent.Ie] = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko"
			} [m_ua];
			req.ContentType = content_type;
			req.Timeout = m_timeout_ms;
			req.ReadWriteTimeout = m_timeout_ms;
			req.CookieContainer = new CookieContainer ();
			foreach (Cookie c in m_cookies)
				req.CookieContainer.Add (uri, c);
			req.ContentLength = param_data.Length;
			if ((param_data?.Length ?? 0) > 0) {
				using (Stream req_stm = req.GetRequestStream ())
					req_stm.Write (param_data, 0, param_data.Length);
			}

			// 发起请求
			GC.Collect ();
			byte [] ret = null;
			using (HttpWebResponse res = (HttpWebResponse) req.GetResponse ()) {
				using (Stream res_stm = res.GetResponseStream ()) {
					using (MemoryStream dms = new MemoryStream ()) {
						int len = 0;
						byte [] bytes = new byte [1024];
						if (res.ContentEncoding == "gzip") {
							using (GZipStream gzip = new GZipStream (res_stm, CompressionMode.Decompress)) {
								while ((len = gzip.Read (bytes, 0, bytes.Length)) > 0)
									dms.Write (bytes, 0, len);
								dms.Seek (0, SeekOrigin.Begin);
								ret = dms.ToArray ();
							}
						} else if (res.ContentEncoding == "deflate") {
							using (DeflateStream deflate = new DeflateStream (res_stm, CompressionMode.Decompress)) {
								while ((len = deflate.Read (bytes, 0, bytes.Length)) > 0)
									dms.Write (bytes, 0, len);
								dms.Seek (0, SeekOrigin.Begin);
								ret = dms.ToArray ();
							}
						} else {
							using (StreamReader res_stm_rdr = new StreamReader (res_stm, Encoding.UTF8))
								ret = clear_bom (Encoding.UTF8.GetBytes (res_stm_rdr.ReadToEnd ()));
						}
					}
				}
			}
			return ret;
		}
	}

	class _hanHttpInit {
		private _hanHttpInit () {
			System.Net.ServicePointManager.DefaultConnectionLimit = 200;
			ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback ((sender, certificate, chain, errors) => { return true; });
		}
		private static _hanHttpInit m_init = new _hanHttpInit ();
	}
}

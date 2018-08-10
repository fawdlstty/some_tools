////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  hanHttpServer
// Description: C# HTTP服务器类
// Class URI:   https://github.com/fawdlstty/some_tools
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Aug 10, 2018
// remarks:     使用此类需要先添加引用System.Web、Newtonsoft.Json
//
////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace hanHttpLib {
	public class hanHttpServer {
		// 构造函数
		public hanHttpServer (params ushort[] ports) {
			m_listener = new HttpListener ();
			m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
			try {
				foreach (var port in ports)
					m_listener.Prefixes.Add ($"http://*:{port}/");
				m_listener.Start ();
			} catch (System.Net.HttpListenerException) {
				m_listener.Close ();
				m_listener = new HttpListener ();
				m_listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
				foreach (var port in ports)
					m_listener.Prefixes.Add ($"http://127.0.0.1:{port}/");
				m_listener.Start ();
			}
		}

		// 处理一次请求
		private static void _request_once (HttpListenerContext ctx) {
			HttpListenerRequest req = ctx.Request;
			HttpListenerResponse res = ctx.Response;
			try {
				// 获取请求命令
				string cmd = req.RawUrl.Substring (1);
				int _p = cmd.IndexOfAny (new char [] { '?', '#' });
				if (_p > 0)
					cmd = cmd.Substring (0, _p);

				// 获取请求内容
				var get_param = new Dictionary<string, string> ();
				foreach (string str_key in req.QueryString.AllKeys)
					get_param [str_key] = req.QueryString [str_key];
				var (post_param, post_file) = parse_form (req);

				// 获取请求者IP
				//string ip = req.UserHostAddress;
				//if (ip.LastIndexOf (':') >= 3)
				//	ip = ip.left (ip.LastIndexOf (':'));
				//ip = ip.r (1, -1);
				string ip = req.Headers ["X-Real-IP"];

				// 请求
				res.StatusCode = 200;
				byte [] result_data = Encoding.UTF8.GetBytes ("Hello World!");

				// HTTP头输入可以自定
				res.AppendHeader ("Cache-Control", "private");
				res.AppendHeader ("Content-Type", "text/html; charset=utf-8");
				res.AppendHeader ("Server", "Microsoft-IIS/7.5"); // nginx/1.9.12
				res.AppendHeader ("X-Powered-By", "ASP.NET");
				res.AppendHeader ("X-AspNet-Version", "4.0.30319");
				//res.AppendHeader ("Content-Length", "");

				// 是否启用压缩
				string [] encodings = (from p in req.Headers ["Accept-Encoding"].Split (',') select p.Trim ().ToLower ()).ToArray ();
				if (Array.IndexOf (encodings, "gzip") >= 0) {
					// 使用 gzip 压缩
					res.AppendHeader ("Content-Encoding", "gzip");
					res.AppendHeader ("Vary", "Accept-Encoding");
					using (GZipStream gzip = new GZipStream (res.OutputStream, CompressionMode.Compress))
						gzip.Write (result_data, 0, result_data.Length);
				} else if (Array.IndexOf (encodings, "deflate") >= 0) {
					// 使用 deflate 压缩
					res.AppendHeader ("Content-Encoding", "deflate");
					res.AppendHeader ("Vary", "Accept-Encoding");
					using (DeflateStream deflate = new DeflateStream (res.OutputStream, CompressionMode.Compress))
						deflate.Write (result_data, 0, result_data.Length);
				} else {
					// 不使用压缩
					res.OutputStream.Write (result_data, 0, result_data.Length);
				}
			} catch (Exception ex) {
				//Log.show_error (ex);
			} finally {
				res.Close ();
				GC.Collect ();
			}
		}

		// 循环请求
		public void run () {
			while (true) {
				try {
					var ctx = m_listener.GetContext ();
					Task.Factory.StartNew ((_ctx) => _request_once (_ctx as HttpListenerContext), ctx);
				} catch (Exception ex) {
					//Log.show_error (ex);
				}
			}
		}

		// 解析HTTP请求参数
		private static (Dictionary<string, string>, Dictionary<string, (string, byte[])>) parse_form (HttpListenerRequest req) {
			// 从流中读取一行字节数组
			Func<Stream, byte[]> _read_bytes_line = (_stm) => {
				using (var resultStream = new MemoryStream()) {
					byte last_byte = 0;
					while (true) {
						int data = _stm.ReadByte();
						resultStream.WriteByte ((byte) data);
						if (data == 10 && last_byte == 13)
							break;
						last_byte = (byte) data;
					}
					resultStream.Position = 0;
					byte[] dataBytes = new byte[resultStream.Length];
					resultStream.Read (dataBytes, 0, dataBytes.Length);
					return dataBytes;
				}
			};

			// 返回数据
			var post_param = new Dictionary<string, string> ();
			var post_file = new Dictionary<string, (string, byte[])> ();
			try {
				if (req.HttpMethod != "POST") {
					return (post_param, post_file);
				} else if (left_is (req.ContentType, "multipart/form-data;")) {
					Encoding encoding = req.ContentEncoding;
					string[] values = req.ContentType.Split (';').Skip (1).ToArray ();
					string boundary = string.Join (";", values).Replace ("boundary=", "").Trim ();
					byte[] bytes_boundary = encoding.GetBytes ($"--{boundary}\r\n");
					byte[] bytes_end_boundary = encoding.GetBytes ($"--{boundary}--\r\n");
					Stream SourceStream = req.InputStream;
					var bytes = _read_bytes_line (SourceStream);
					if (bytes == bytes_end_boundary) {
						return (post_param, post_file);
					} else if (!compare (bytes, bytes_boundary)) {
						Console.WriteLine ("Parse Error in [first read is not bytes_boundary]");
						return (post_param, post_file);
					}
					while (true) {
						bytes = _read_bytes_line (SourceStream);
						string _tmp = encoding.GetString (bytes);//Content-Disposition: form-data; name="text_"
						if (!left_is (_tmp, "Content-Disposition:")) {
							Console.WriteLine ("Parse Error in [begin block is not Content-Disposition]");
							return (post_param, post_file);
						}
						string name = substr_mid (_tmp, "name=\"", "\"");
						string filename = substr_mid (_tmp, "filename=\"", "\"");
						do {
							bytes = _read_bytes_line (SourceStream);
						} while (bytes[0] != 13 || bytes[1] != 10);
						bytes = _read_bytes_line (SourceStream);
						using (var ms = new MemoryStream ()) {
							while (!compare (bytes, bytes_boundary) && !compare (bytes, bytes_end_boundary)) {
								ms.Write (bytes);
								bytes = _read_bytes_line (SourceStream);
							}
							if (ms.Length < 2) {
								Console.WriteLine ("Parse Error in [ms.Length < 2]");
								return (post_param, post_file);
							}
							bytes = new byte[ms.Length - 2];
							if (bytes.Length > 2) {
								ms.Position = 0;
								ms.Read (bytes);
							}
							if (string.IsNullOrEmpty (filename)) {
								post_param[name] = encoding.GetString (bytes);
							} else {
								post_file[name] = (filename, bytes);
							}
						}
					}
				} else {
					using (StreamReader sr = new StreamReader (req.InputStream, Encoding.UTF8)) {
						string post_data = sr.ReadToEnd ();
						if (post_data[0] == '{') {
							JObject obj = JObject.Parse (post_data);
							foreach (var (key, val) in obj)
								post_param[HttpUtility.UrlDecode (key)] = HttpUtility.UrlDecode (val.ToString ());
						} else {
							string[] pairs = post_data.Split (new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
							foreach (string pair in pairs) {
								int p = pair.IndexOf ('=');
								if (p > 0)
									post_param[HttpUtility.UrlDecode (pair.Substring (0, p))] = HttpUtility.UrlDecode (pair.Substring (p + 1));
							}
						}
					}
				}
			} catch (Exception ex) {
				//Log.show_error (ex);
			}
			return (post_param, post_file);
		}

		private static bool compare (byte[] arr1, byte[] arr2) {
			if (arr1 == null && arr2 == null)
				return true;
			else if (arr1 == null || arr2 == null)
				return false;
			else if (arr1.Length != arr2.Length)
				return false;
			for (int i = 0; i < arr1.Length; ++i) {
				if (arr1 [i] != arr2 [i])
					return false;
			}
			return true;
		}

		private static bool left_is (string s, string s2) {
			if (string.IsNullOrEmpty (s))
				return string.IsNullOrEmpty (s2);
			if (s.Length < s2.Length)
				return false;
			return s.Substring (0, s2.Length) == s2;
		}

		private static string substr_mid (string s, string begin, string end = "") {
			if (string.IsNullOrEmpty (s) || string.IsNullOrEmpty (begin))
				return "";
			int p = s.IndexOf (begin);
			if (p == -1)
				return "";
			s = s.Substring (p + begin.Length);
			if (!string.IsNullOrEmpty (end)) {
				p = s.IndexOf (end);
				if (p >= 0)
					s = s.Substring (0, p);
			}
			return s;
		}

		private HttpListener m_listener = null;
	}
}

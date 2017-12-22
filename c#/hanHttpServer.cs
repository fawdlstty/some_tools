////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  hanHttpServer
// Description: C# HTTP服务器类
// Class URI:   https://github.com/fawdlstty/some_tools
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Oct 27, 2017
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace hanHttpLib
{
	public class hanHttpServer
	{
		HttpListener m_listener = null;

		public delegate HttpListener del_get_listener (string host);

		/// <summary>
		/// 启动服务
		/// </summary>
		public void start (ushort uPort)
		{
			del_get_listener get_listener = host =>
			{
				HttpListener listener = new HttpListener ();
				listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
				listener.Prefixes.Add (string.Format ("http://{0}:{1}/", host, m_cfg.m_server_port));
				listener.Start ();
				return listener;
			};
			try
			{
				if (m_listener != null)
					return true;
				try
				{
					m_listener = get_listener ("+");
				}
				catch (HttpListenerException)
				{
					System.Windows.Forms.MessageBox.Show ("无管理员权限，将以本地方式监听端口");
					m_listener = get_listener ("localhost");
				}
				m_listener.BeginGetContext (_on_client_request, m_listener);
				return true;
			}
			catch (Exception ex)
			{
				m_log.show_info ("Catch Error: {0}".format (ex.Message));
			}
			return false;
		}

		/// <summary>
		/// 停止服务
		/// </summary>
		public void stop ()
		{
			if (m_listener == null)
				return;
			m_listener.Stop ();
			m_listener.Close ();
			m_listener = null;
		}

		private delegate void del_write_stream (Stream stm, String data);
		private delegate string del_get_value (string key);

		/// <summary>
		/// 当客户端请求时的异步回调函数
		/// </summary>
		/// <param name="result"></param>
		private void _on_client_request (IAsyncResult result)
		{
			HttpListener _listener = (HttpListener) result.AsyncState;
			if (_listener == null || m_listener == null || !_listener.IsListening)
				return;
			HttpListenerContext ctx = _listener.EndGetContext (result);
			ThreadPool.QueueUserWorkItem (_ctx =>
			{
				_process_request ((HttpListenerContext) _ctx);
			}, ctx);
			_listener.BeginGetContext (_on_client_request, _listener);
		}

		private void _process_request (HttpListenerContext ctx)
		{
			try
			{
				HttpListenerRequest req = ctx.Request;
				HttpListenerResponse res = ctx.Response;
				string str_url = req.RawUrl.Substring (1);

				// 读取Post参数
				List<string> post_keys = new List<string> (), post_values = new List<string> ();
				using (StreamReader sr = new StreamReader (req.InputStream))
				{
					string[] pairs = sr.ReadToEnd ().Split (new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string pair in pairs)
					{
						string[] item = pair.Split (new char[] { '=' });
						if (item.Length > 1)
						{
							post_keys.Add (HttpUtility.UrlDecode (item[0]));
							post_values.Add (HttpUtility.UrlDecode (item[1]));
						}
					}
				}
				// 获取请求参数
				del_get_value get_value = (key) =>
				{
					string value = req.QueryString[key];
					if (string.IsNullOrEmpty (value))
					{
						int p = post_keys.IndexOf (key);
						if (p >= 0)
							value = post_values[p];
					}

					if (string.IsNullOrEmpty (value))
						return "";
					return value;
				};

				// 此处根据str_url处理输出，假定输入内容写在str里
				// 获取请求参数 get_value ("test")
				string str = "ok";
				res.StatusCode = 200;

				// HTTP头输入可以自定
				res.AppendHeader ("Cache-Control", "private");
				res.AppendHeader ("Content-Type", "text/html; charset=utf-8");
				//res.AppendHeader ("Server", "Microsoft-IIS/7.5"); //nginx/1.9.12
				res.AppendHeader ("X-Powered-By", "ASP.NET");
				res.AppendHeader ("X-AspNet-Version", "4.0.30319");
				//res.AppendHeader ("Content-Length", "");

				// 是否启用压缩
				string[] encoding = req.Headers["Accept-Encoding"].Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < encoding.Length; ++i)
					encoding[i] = encoding[i].Trim ();
				del_write_stream write_stream = (stream, data) =>
				{
					using (StreamWriter sw = new StreamWriter (stream, Encoding.UTF8))
						sw.Write (data);
				};
				if (Array.IndexOf (encoding, "gzip") >= 0)
				{
					// 使用 gzip 压缩
					res.AppendHeader ("Content-Encoding", "gzip");
					res.AppendHeader ("Vary", "Accept-Encoding");
					using (GZipStream gzip = new GZipStream (res.OutputStream, CompressionMode.Compress))
						write_stream (gzip, str);
				}
				else if (Array.IndexOf (encoding, "deflate") >= 0)
				{
					// 使用 deflate 压缩
					res.AppendHeader ("Content-Encoding", "deflate");
					res.AppendHeader ("Vary", "Accept-Encoding");
					using (DeflateStream deflate = new DeflateStream (res.OutputStream, CompressionMode.Compress))
						write_stream (deflate, str);
				}
				else
				{
					// 不使用压缩
					write_stream (res.OutputStream, str);
				}
				res.OutputStream.Close ();
			}
			catch (Exception)
			{
				// something is wrong...
			}
		}
	}
}

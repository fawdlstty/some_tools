////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  hanHttpClient
// Description: C# HTTP客户端类
// Class URI:   https://github.com/fawdlstty/some_tools
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Nov 16, 2017
// remarks:     使用此类需要先添加引用System.Web
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace hanHttpLib
{
	public enum hanHttp_UserAgent
	{
		Android = 0,
		Chrome,
		Ie,
	}

	public class hanHttpClient
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="strUrl">请求路径</param>
		/// <param name="strType"></param>
		/// <param name="bSimulatePC"></param>
		public hanHttpClient (hanHttp_UserAgent uaType = hanHttp_UserAgent.Chrome, string strType = "POST")
		{
			m_uaType = uaType;
			m_strType = strType.ToUpper ();

			if (System.Net.ServicePointManager.DefaultConnectionLimit != 55)
			{
				System.Net.ServicePointManager.DefaultConnectionLimit = 55;
				ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback ((sender, certificate, chain, errors) => { return true; });
			}
		}

		/// <summary>
		/// 添加参数，参数类型根据声明动态解析为POST或GET
		/// </summary>
		/// <param name="strKey">键</param>
		/// <param name="strValue">值</param>
		public void add_param (string strKey, string strValue)
		{
			if (m_sbParams.Length > 0)
				m_sbParams.Append ('&');
			m_sbParams.Append (HttpUtility.UrlEncode (strKey)).Append ("=").Append (HttpUtility.UrlEncode (strValue));
		}

		/// <summary>
		/// 添加Cookie
		/// </summary>
		/// <param name="strKey">键</param>
		/// <param name="strValue">值</param>
		/// <param name="path">路径</param>
		public void add_cookie (string strKey, string strValue, string path = "/")
		{
			add_cookie (new Cookie (strKey, strValue, path));
		}

		/// <summary>
		/// 添加Cookie
		/// </summary>
		/// <param name="cookie">cookie</param>
		public void add_cookie (Cookie cookie)
		{
			m_cookies.Add (cookie);
		}

		/// <summary>
		/// 添加一组Cookie
		/// </summary>
		/// <param name="cookies">Cookie集合</param>
		public void add_cookie (CookieCollection cookies)
		{
			if (cookies == null)
				return;
			for (int i = 0; i < cookies.Count; ++i)
				add_cookie (cookies[i]);
		}

		/// <summary>
		/// 获取一个Cookie
		/// </summary>
		/// <param name="strKey">键</param>
		/// <returns></returns>
		public Cookie get_cookie (string strKey)
		{
			return m_cookies[strKey];
		}

		/// <summary>
		/// 设置访问路径
		/// </summary>
		/// <param name="strUrl"></param>
		public void set_url (string strUrl)
		{
			m_strUrl = strUrl;

			// 获取host
			if (m_strUrl.Substring (0, 7).ToLower () != "http://" && m_strUrl.Substring (0, 8).ToLower () != "https://")
				m_strUrl.Insert (0, "http://");
			int p = m_strUrl.IndexOf ('/', 8);
			m_uri_host = new Uri (p == -1 ? m_strUrl : m_strUrl.Substring (0, p));
		}

		/// <summary>
		/// 发起请求
		/// </summary>
		/// <returns></returns>
		public bool do_request ()
		{
			m_finish = false;
			m_err_msg = "";
			m_result_data = "";
			try
			{
				// 生成新URL
				if (m_strType != "POST" && m_sbParams.Length > 0)
				{
					char ch_last = m_strUrl[m_strUrl.Length - 1];
					if (m_strUrl.IndexOf ('?') != -1 && ch_last != '?' && ch_last != '&')
						m_strUrl += '&';
					else if (m_strUrl.IndexOf ('?') == -1)
						m_strUrl += '?';
					m_strUrl += m_sbParams.ToString ();
				}

				// 创建请求对象
				HttpWebRequest req = (HttpWebRequest) WebRequest.Create (m_strUrl);
				req.AllowAutoRedirect = false;
				req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
				req.Headers["Accept-Encoding"] = "gzip, deflate";
				req.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";
				req.Headers["Cache-Control"] = "max-age=0";
				req.KeepAlive = false;
				req.Method = m_strType.ToUpper ();
				if (m_uaType == hanHttp_UserAgent.Android)
					req.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3223.8 Mobile Safari/537.36";
				else if (m_uaType == hanHttp_UserAgent.Chrome)
					req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3223.8 Safari/537.36";
				else if (m_uaType == hanHttp_UserAgent.Ie)
					req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
				req.ContentType = "application/x-www-form-urlencoded";
				req.Timeout = 30000;
				req.CookieContainer = new CookieContainer ();
				for (int i = 0; i < m_cookies.Count; ++i)
					req.CookieContainer.Add (m_uri_host, m_cookies[i]);

				// 填入附加参数项
				if (m_strType == "POST" && m_sbParams.Length > 0)
				{
					byte[] barr_params = Encoding.UTF8.GetBytes (m_sbParams.ToString ());
					req.ContentLength = barr_params.Length;
					Stream req_stm = req.GetRequestStream ();
					req_stm.Write (barr_params, 0, barr_params.Length);
					req_stm.Close ();
				}

				// 提交请求
				GC.Collect ();
				HttpWebResponse res = (HttpWebResponse) req.GetResponse ();
				if (res.StatusCode == HttpStatusCode.Moved || res.StatusCode == HttpStatusCode.Found)
				{
					// 重定向
					set_url (res.Headers["Location"]);
					return do_request ();
				}
				Stream res_stm = res.GetResponseStream ();
				using (MemoryStream dms = new MemoryStream ())
				{
					int len = 0;
					byte[] bytes = new byte[1024];

					if (res.ContentEncoding == "gzip")
					{
						GZipStream gzip = new GZipStream (res_stm, CompressionMode.Decompress);
						while ((len = gzip.Read (bytes, 0, bytes.Length)) > 0)
							dms.Write (bytes, 0, len);
						dms.Seek (0, SeekOrigin.Begin);
						m_result_data = Encoding.UTF8.GetString (dms.ToArray ());
					}
					else if (res.ContentEncoding == "deflate")
					{
						DeflateStream deflate = new DeflateStream (res_stm, CompressionMode.Decompress);
						while ((len = deflate.Read (bytes, 0, bytes.Length)) > 0)
							dms.Write (bytes, 0, len);
						dms.Seek (0, SeekOrigin.Begin);
						m_result_data = Encoding.UTF8.GetString (dms.ToArray ());
					}
					else
					{
						StreamReader res_stm_rdr = new StreamReader (res_stm, Encoding.UTF8);
						m_result_data = res_stm_rdr.ReadToEnd ();
					}
				}

				// 获取cookie
				m_cookies = res.Cookies;
				res.Close ();
				req.Abort ();
				m_finish = true;
				return true;
			}
			catch (Exception ex)
			{
				m_err_msg = ex.Message;
				m_finish = true;
				return false;
			}
		}

		/// <summary>
		/// 异步发起请求
		/// </summary>
		/// <returns></returns>
		public bool async_request ()
		{
			m_finish = false;
			try
			{
				new Thread (new ParameterizedThreadStart (t =>
				{
					((hanHttpClient) t).do_request ();
				})).Start (this);
				return true;
			}
			catch (Exception ex)
			{
				m_err_msg = ex.Message;
				m_finish = true;
				return false;
			}
		}

		// 请求变量
		private hanHttp_UserAgent m_uaType;
		private string m_strUrl;
		private Uri m_uri_host;
		private string m_strType;
		private StringBuilder m_sbParams = new StringBuilder ();

		// 返回变量
		public string m_err_msg = "";
		public string m_result_data = "";
		public bool m_finish = false;

		// 请求与返回变量
		public CookieCollection m_cookies = new CookieCollection ();
	}
}

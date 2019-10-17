using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using HttpHelper = System.Web.HttpUtility;
using RequestParams = System.Collections.Generic.Dictionary<string, object>;

public class HttpUtility
{
	/// <summary>
	/// 设置连接数限制，代理
	/// </summary>
	public static void Setup(int limit, IWebProxy proxy = null)
	{
		ServicePointManager.DefaultConnectionLimit = limit;
		WebRequest.DefaultWebProxy = proxy;
	}

	/// <summary>
	/// HTTP GET
	/// </summary>
	public static string GetText(string url, RequestParams data = null)
	{
		var req = _BuildGetRequest(url, data);
		return _GetResponseText(req);
	}

	/// <summary>
	/// HTTP GET, copy to output
	/// </summary>
	public static void GetStream(string url, Stream output, RequestParams data = null)
	{
		var req = _BuildGetRequest(url, data);
		_WriteResponseToStream(req, output);
	}

	/// <summary>
	/// HTTP POST (x-www-form-urlencoded)
	/// </summary>
	public static string PostText(string url, RequestParams data = null)
	{
		var req = _BuildPostRequest(url, data);
		return _GetResponseText(req);
	}

	/// <summary>
	/// HTTP POST (x-www-form-urlencoded), copy to output
	/// </summary>
	public static void PostStream(string url, Stream output, RequestParams data = null)
	{
		var req = _BuildPostRequest(url, data);
		_WriteResponseToStream(req, output);
	}

	/// <summary>
	/// 从text的offset处开始，寻找介于start和end之间的字符串
	/// </summary>
	public static string FindBetween(string text, string start, string end, ref int offset)
	{
		if (offset >= text.Length)
			return null;
		int p_start = text.IndexOf(start, offset);
		if (p_start < 0)
			return null;
		p_start += start.Length;
		if (p_start >= text.Length)
			return null;
		int p_end = text.IndexOf(end, p_start);
		if (p_end < 0)
			return null;
		offset = p_end + end.Length;
		return text.Substring(p_start, p_end - p_start);
	}

	/// <summary>
	/// 从text中寻找介于start和end之间的字符串
	/// </summary>
	public static string FindBetween(string text, string start, string end)
	{
		int offset = 0;
		return FindBetween(text, start, end, ref offset);
	}

	/// <summary>
	/// 构造Get请求
	/// </summary>
	static HttpWebRequest _BuildGetRequest(string url, RequestParams data)
	{
		//如果传入了data，则先构造请求url
		if (data != null && data.Count > 0)
		{
			StringBuilder sb = new StringBuilder(url);
			if (url.IndexOf('?') < 0)
				sb.Append('?');
			//build request
			foreach (var entry in data)
			{
				object v = entry.Value;
				sb.Append(HttpHelper.UrlEncode(entry.Key))
					.Append('=')
					.Append(v == null ? "" : HttpHelper.UrlEncode(v.ToString()))
					.Append('&');
			}
			//change url
			url = sb.ToString(0, sb.Length - 1); //remove last '&'
		}

		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
		req.Method = "GET";
		return req;
	}

	/// <summary>
	/// 构造Post请求 (x-www-form-urlencoded)
	/// </summary>
	static HttpWebRequest _BuildPostRequest(string url, RequestParams data)
	{
		HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
		req.Method = "POST";
		req.ContentType = "application/x-www-form-urlencoded";
		//写入数据
		if (data != null && data.Count > 0)
		{
			//Build request
			using (StreamWriter writer = new StreamWriter(req.GetRequestStream()))
			{
				bool first = true;
				foreach (var entry in data)
				{
					if (!first)
						writer.Write('&');
					else
						first = false;
					object v = entry.Value;
					writer.Write(HttpHelper.UrlEncode(entry.Key));
					writer.Write('=');
					if (v != null)
						writer.Write(HttpHelper.UrlEncode(v.ToString()));
				}
			}
		}
		return req;
	}

	/// <summary>
	/// 返回请求结果
	/// </summary>
	static string _GetResponseText(HttpWebRequest req)
	{
		using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
		{
			if (!_PreCheckResponse(resp))
				return "";

			string charSet = resp.CharacterSet;
			Encoding enc = string.IsNullOrEmpty(charSet) ? Encoding.UTF8
				: Encoding.GetEncoding(charSet);

			using (StreamReader reader = new StreamReader(resp.GetResponseStream(), enc))
				return reader.ReadToEnd();
		}
	}

	/// <summary>
	/// 将请求结果写入stream
	/// </summary>
	static void _WriteResponseToStream(HttpWebRequest req, Stream stream)
	{
		using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
		{
			if (!_PreCheckResponse(resp))
				return;

			using (Stream respStream = resp.GetResponseStream())
				respStream.CopyTo(stream);
		}
	}

	/// <summary>
	/// 检查返回的状态是否为2xx
	/// </summary>
	static bool _PreCheckResponse(HttpWebResponse resp)
	{
		int status = (int)resp.StatusCode;
		return status >= 200 && status <= 299;
	}
}

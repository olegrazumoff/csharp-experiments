using System;
using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace ProxyManager
{
	class ProxyReader
	{
		public static void Main (string[] args)
		{
			List<Proxy> proxies = ProxyService.GetProxies(); 
			foreach(Proxy proxy in proxies)
			{
				Console.WriteLine(proxy);
			}
			Console.WriteLine("SIZE: " + proxies.Count);
			List<Proxy> goodProxies = ProxyChecker.Check(proxies);
			foreach(Proxy proxy in goodProxies)
			{
				Console.WriteLine(proxy);
			}
			Console.WriteLine("SIZE: " + goodProxies.Count);
		}
	}
	
	class ProxyChecker
	{
		private static Int32 TIMEOUT = 3000;
		
		public static List<Proxy> Check (List<Proxy> proxies) 
		{
			List<Proxy> goodProxies = new List<Proxy>();
			foreach(Proxy proxy in proxies)
			{
				Console.Write(proxy + " ");
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://hello.w0r1d.net/");
				request.Proxy = new WebProxy (proxy.getIpAddress().ToString(), proxy.getPort());
				request.Timeout = TIMEOUT;
				String rawHtml = "";
				try
				{
					Stopwatch sw = new Stopwatch();
					sw.Start();
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();
					rawHtml = (new StreamReader(response.GetResponseStream())).ReadToEnd();
					response.Close();
					sw.Stop();
					Console.Write("Elapsed={0} ",sw.ElapsedMilliseconds);
				}
				catch (System.Exception e)
				{
					Console.Write("BAD PROXY: " + e.Message);
				}
				Console.WriteLine();
				if(rawHtml.Contains("A photostream of the realisation on flickr"))
				{
					goodProxies.Add(proxy);
				}
			}
			return goodProxies;	
		}
	}
	
	class ProxyService 
	{
		private static String serviceUrl = "http://www.hidemyass.com/proxy-list/"; 
		private static String requestParams = "c%5B%5D=Russian+Federation&p=&pr%5B%5D=1&a%5B%5D=0&a%5B%5D=1&a%5B%5D=2&a%5B%5D=3&a%5B%5D=4&pl=on&sp%5B%5D=3&ct%5B%5D=3&s=0&o=0&pp=3&sortBy=date";
		
		//TODO: check for expired session
		
		//request to the server to get session id
		private static String GetSessionId ()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUrl);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			response.Close();
			String sessionCookie = response.Headers["Set-Cookie"];
			return sessionCookie.Substring(0, sessionCookie.IndexOf(";"));
		}
		
		//request to the server for binding session id with particular request parameters
		private static String GetRequestUrl (String sessionId) 
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUrl);
			request.Method = "POST";
			request.ContentLength = requestParams.Length;
			request.ContentType = "application/x-www-form-urlencoded";
			request.Headers["Cookie"] = sessionId;
			Stream dataStream = request.GetRequestStream ();
			byte[] dataArray = Encoding.ASCII.GetBytes(requestParams);
			dataStream.Write (dataArray, 0, dataArray.Length);
			dataStream.Close();
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			response.Close();
			return response.ResponseUri.ToString();
		}
		
		//get proxies that match with particular request url and session id
		private static String Read (String sessionId, String requestUrl)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
			request.Headers["Cookie"] = sessionId;
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			String rawHtml = (new StreamReader(response.GetResponseStream())).ReadToEnd();
			response.Close();
			return rawHtml;
		}
		
		public static List<Proxy> GetProxies ()
		{
			String sessionId = GetSessionId();
			String requestUrl = GetRequestUrl(sessionId);
			Console.WriteLine("SESSION: " + sessionId);
			Console.WriteLine("URL: " + requestUrl);
			String html = Read(sessionId, requestUrl);
			return ProxyParser.Parse(html);
		}
	}
	
	class ProxyParser
	{
		//parse raw response html to grab proxies
		public static List<Proxy> Parse (String rawHtml)
		{
			List<Proxy> result = new List<Proxy>();
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(rawHtml);
			foreach(HtmlNode node in doc.DocumentNode.SelectNodes("//table[@id='listtable']/tr[td[2]/span]"))
 			{
				HtmlNode ipNode = node.SelectSingleNode("td[2]/span");
				Byte[] ip = ParseIpNode(ipNode);
				Int32 port = Int32.Parse(node.SelectSingleNode("td[3]").InnerText);
			    result.Add(new Proxy(ip, port));
			}
			return result;
		}
		
		private static Byte[] ParseIpNode (HtmlNode node)
		{
			Int32 i = 0;
			Byte[] ip = new Byte[4];
			HtmlNode styleNode = node.SelectSingleNode("style");
			List<String> styles = ParseStyle(styleNode.InnerText);
			foreach(HtmlNode spanNode in node.SelectNodes("*"))
			{
				HtmlAttribute styleAttr = spanNode.Attributes["style"];
				HtmlAttribute classAttr = spanNode.Attributes["class"];
				if(	spanNode.InnerText.Length != 0 &&
					!spanNode.InnerText.Equals(".") && 
				   	((classAttr != null && !styles.Contains(classAttr.Value)) || (styleAttr != null && styleAttr.Value.Contains("inline"))))
				{
					ip[i] = Byte.Parse(spanNode.InnerText);
					i++;
				}
				if(spanNode.NextSibling != null && spanNode.NextSibling.Name.Equals("#text") && !spanNode.NextSibling.InnerText.Equals("."))
				{
					String[] values = spanNode.NextSibling.InnerText.Split('.');
					foreach(String val in values)
					{
						if(val.Length != 0)
						{
							ip[i] = Byte.Parse(val);
							i++;
						}
					}
				}
			}
			return ip;
		}
		
		private static List<String> ParseStyle (String style) 
		{
			List<String> result = new List<String>();
			String[] lines = style.Split('\n');
			foreach (String line in lines)
			{
				if(line.Contains("none"))
				{
					result.Add(line.Substring(1, line.IndexOf("{") - 1));
				}
			}
			return result;
		}
		
	}
	
	class Proxy
	{
		private IPAddress ip;
		private Int32 port;
		
		public Proxy (Byte[] ip, Int32 port)
		{
			this.ip = new IPAddress(ip);
			this.port = port;
		}
		
		public IPAddress getIpAddress()
		{
			return ip;
		}
		
		public Int32 getPort()
		{
			return port;
		}
		
		public override string ToString ()
		{
			 return ip.ToString() + ":" + port.ToString();
		}
	}
}

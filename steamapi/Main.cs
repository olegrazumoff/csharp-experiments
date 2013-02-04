using System;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;
using HtmlAgilityPack;

namespace test1
{
	class MyThread 
	{
		private Int32 m_begin, m_end;
		private String sUrlTemplate;
		private String sProxy;
		
		public MyThread(Int32 begin, Int32 end, String url, String proxy) {
			m_begin = begin;
			m_end = end;
			sUrlTemplate = url;
			sProxy = proxy;
		}
		
		public void Start() {
			for(int i = m_begin; i < m_end; i++)
			{
				dump(i);
			}
		}
		
		private void dump(Int32 i)
		{
			try 
			{
				String sURL = String.Format(sUrlTemplate, i);
				Console.Write(i.ToString() + ": "/* + sURL*/);
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sURL);
				request.Proxy = new WebProxy (sProxy, 8080);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Console.WriteLine(response.StatusCode);
				response.Close();
			}
			catch(System.Exception e)
			{
				Console.WriteLine("FAIL");
			}
		}
	}
	
	
	class MainClass
	{
		public static String sURLTemplate1 = "https://api.steampowered.com/IDOTA2Match_570/GetMatchDetails/V001/?match_id={0}&key=E9C963076854A586D2D50E2BC0EF8675";
		public static String sURLTemplate2 = sURLTemplate1;
		//public static String sURLTemplate2 = "https://api.steampowered.com/IDOTA2Match_570/GetMatchDetails/V001/?match_id={0}&key=104329EE0FC82F86591B8259B1BAB637";
		
		public static String[] res = new String[100];
		
		public static void Main (string[] args)
		{
			System.Net.ServicePointManager.ServerCertificateValidationCallback += (s,ce,ca,p) => true;
			
			String[] proxies = new String[5];
			proxies[0] = "210.177.139.89";
			proxies[1] = "190.90.20.227";
			proxies[2] = "118.99.121.184";
			proxies[3] = "41.234.24.69";
			proxies[4] = "193.93.229.156";
			
			int threadnum = 5;
			int connections = 3;
			int operations = 1500;
			MyThread[] myThreads = new MyThread[threadnum * connections];
			Thread[] threads = new Thread[threadnum * connections];
			int step = operations / (threadnum * connections);
			
			Stopwatch sw = new Stopwatch();
			sw.Start();
			
			for(int i = 0; i < threadnum; i++) {
				for(int j = 0; j < connections; j++) {
					myThreads[i * connections + j] = new MyThread(step * (i * connections + j), step * ((i * connections + j) + 1), sURLTemplate1, proxies[i]);
					threads[i * connections + j] = new Thread(new ThreadStart(myThreads[i * connections + j].Start));
					threads[i * connections + j].Start();
				}
			}
			for(int i = 0; i < threadnum; i++) {
				threads[i].Join();
			}
			sw.Stop();
			Console.WriteLine("Elapsed={0}",sw.Elapsed);
		}
	}
	
	struct Point
	{
		private Int32 m_x, m_y;
		public Point(Int32 x, Int32 y) 
		{
			m_x = x;
			m_y = y;
		}
		public void Change(Int32 x, Int32 y) 
		{
			m_x = x;
			m_y = y;
		}
		public override String ToString()
		{
			return String.Format("{0}.{1}", m_x, m_y);
		}
	}
}

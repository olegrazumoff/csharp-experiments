using System;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;
using HtmlAgilityPack;
using System.Collections.Generic;
using steamapi;

namespace SteamAPI
{
	class MyThread 
	{
		private Int32 m_begin, m_end;
		private String sUrlTemplate;
		private String sProxy;
        private List<int> successResponses;
        private int attempt;
		
		public MyThread(Int32 begin, Int32 end, String url, String proxy) {
			m_begin = begin;
			m_end = end;
			sUrlTemplate = url;
			sProxy = proxy;
            successResponses = new List<int>();
		}
		
		public void Start() {
            //using (StreamWriter file = new StreamWriter("good2.txt"))
            //{
    			for(int i = m_begin; i < m_end; i++)
    			{
                    attempt = 0;
                    int? value;
                    do
                    {
                        value = dump(i);
                    }
                    while (attempt < 3 && value.HasValue && value.Value < 0);
                    if (attempt > 2)
                    {
                        return;
                    }
                    //if (value.HasValue && value.Value > 0)
                    //{
                    //    file.WriteLine(value.Value);
                    //}
    			}
            //}
		}

        public List<int> GetSuccessResponses()
        {
            return successResponses;
        }

		private int? dump(Int32 i)
		{
            String sURL = String.Format(sUrlTemplate, i);
			try 
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sURL);
                //string[] proxy = sProxy.Split(':');
				//request.Proxy = new WebProxy (proxy[0], int.Parse(proxy[1]));
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        reader.ReadToEnd();
                        successResponses.Add(i);
                        Console.WriteLine(i);
                        return i;
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        return null;
                    }
                    attempt++;
                    Console.WriteLine("attempt {0}", attempt);
                    return -1;
                }
			}
			catch(WebException e)
			{
                //if (e.Status == WebExceptionStatus.ConnectFailure || e.Status == WebExceptionStatus.Timeout || e.Status )
                //{
                //    
               //}
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    return null;
                }
                attempt++;
                Console.WriteLine("attempt {0}", attempt);
                return -1;
			}
		}
	}
	
	class MainClass
	{
        public static String sURLTemplate1 = "https://api.steampowered.com/IDOTA2Match_570/GetMatchDetails/V001/?match_id={0}&key=104329EE0FC82F86591B8259B1BAB637";
        public static List<string> good = new List<string>();
		
		public static void Main (string[] args)
		{
			System.Net.ServicePointManager.ServerCertificateValidationCallback += (s,ce,ca,p) => true;

            //jobs generation
            int iterationsPerJob = 10;
            List<int> jobs = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                jobs.Add(i * iterationsPerJob);
            }

            List<Proxy> proxies = ProxyReader.GetProxies();

            List<ProxyThread> whiteList = new List<ProxyThread>();
            List<ProxyThread> blackList = new List<ProxyThread>();

            foreach (Proxy proxy in proxies)
            {
                ProxyThread thread = new ProxyThread(jobs, proxy);
            }
            

            int count = 1;

            String[] proxies = new String[count];
			proxies[0] = "46.20.71.21:80";
		    proxies[1] = "83.69.216.226:3128";
			proxies[2] = "77.241.20.36:3128";
			proxies[3] = "109.236.220.98:8080";
			proxies[4] = "91.221.176.163:3128";
			
			int threadnum = count;
			int connections = 1000;
			int operations = 1000;
			MyThread[] myThreads = new MyThread[threadnum * connections];
			Thread[] threads = new Thread[threadnum * connections];
			int step = operations / (threadnum * connections);
			
			Stopwatch sw = new Stopwatch();
			sw.Start();
			
			for (int i = 0; i < threadnum; i++) 
            {
				for(int j = 0; j < connections; j++) 
                {
					myThreads[i * connections + j] = new MyThread(step * (i * connections + j), step * ((i * connections + j) + 1), sURLTemplate1, proxies[i]);
					threads[i * connections + j] = new Thread(new ThreadStart(myThreads[i * connections + j].Start));
					threads[i * connections + j].Start();
				}
			}
			foreach (Thread thread in threads)
            {
				thread.Join();
			}
			sw.Stop();
            int total = 0;
            foreach (MyThread thread in myThreads) 
            {
                total += thread.GetSuccessResponses().Count;
            }
            Console.WriteLine("Total time: {0}", sw.Elapsed);
            Console.WriteLine("Total OK: {0}", total);
            good = new List<string>(File.ReadAllLines("good2.txt"));
            if (good.Count != total)
            {
                Console.WriteLine("Fail - expected: {0}, actual: {1}", good.Count, total);
                return;
            }
            foreach (MyThread thread in myThreads)
            {
                foreach (int j in thread.GetSuccessResponses())
                {
                    if (!good.Contains(j.ToString()))
                    {
                        Console.WriteLine("Fail - does not contain: {0}", j);
                        return;
                    }
                }
            }
            Console.WriteLine("PASSED!");
		}
	}
}

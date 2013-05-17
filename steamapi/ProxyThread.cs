using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace steamapi
{
    class ProxyThread
    {
        private String urlTemplate;
        private Proxy proxy;
        private BlockingCollection<Int32> jobs;
        private int jobInterval;
        private List<int> successResponses = new List<int>();
        private int attempt;

        public ProxyThread(String urlTemplate, BlockingCollection<Int32> jobs, int jobInterval, Proxy proxy)
        {
            this.urlTemplate = urlTemplate;
            this.proxy = proxy;
            this.jobs = jobs;
            this.jobInterval = jobInterval;
        }

        private int GetJob()
        {
            return jobs.Take();
        }

        private void AddJob(int job)
        {
            jobs.Add(job);
        }

        public void Start()
        {
            //using (StreamWriter file = new StreamWriter("good2.txt"))
            //{
            while (true)
            {
                int job = GetJob();
                for (int i = job; i < job + jobInterval; i++)
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
                        //TODO: return other iterations to jobs queue
                        AddJob(job);
                        return;
                    }
                    //if (value.HasValue && value.Value > 0)
                    //{
                    //    file.WriteLine(value.Value);
                    //}
                }
            }
            //}
        }

        public List<int> GetSuccessResponses()
        {
            return successResponses;
        }

        private int? dump(Int32 i)
        {
            String sURL = String.Format(urlTemplate, i);
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
            catch (WebException e)
            {
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
}

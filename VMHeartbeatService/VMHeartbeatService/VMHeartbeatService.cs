using Cassia;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace VMHeartbeatService
{
    public partial class VMHeartbeatService : ServiceBase
    {
        Timer beatTimer;

        int courseID;
        int veProfileID;
        int userID;

        string roleName;

        int intervalTime;
        int maxIdleTime;
        TimeSpan maxIdleTimeSpan;

        ITerminalServicesManager manager;

        bool isStarting;

        string webApiUrl;

        private async void SetupService()
        {
            XmlTextReader configXmlReader = new XmlTextReader(@"C:\CloudSwyft\VMHeartbeatService\Config.xml");

            configXmlReader.ReadToDescendant("WebApiUrl");

            webApiUrl = configXmlReader.ReadElementContentAsString();

            isStarting = true;

            char[] dashSplit = { '-' };

            string[] nameSplit = System.Environment.MachineName.Split(dashSplit);

            string client = nameSplit[0];

            courseID = Convert.ToInt32(nameSplit[1]);
            veProfileID = Convert.ToInt32(nameSplit[2]);
            userID = Convert.ToInt32(nameSplit[3]);
            
            roleName = System.Environment.MachineName;

            GetURL(client);

            intervalTime = Convert.ToInt32(await ApiCall("GET", webApiUrl, "api/VirtualMachines/HeartbeatInterval")) * 1000;
            maxIdleTime = Convert.ToInt32(await ApiCall("GET", webApiUrl, "api/VirtualMachines/MaxIdleTime"));
            maxIdleTimeSpan = new TimeSpan(0, 0, maxIdleTime);

            // Cassia
            manager = new TerminalServicesManager();

            // Shutdown intercept
            SystemEvents.SessionEnding += ShutdownVM;

            beatTimer = new Timer(Convert.ToDouble(intervalTime));
            beatTimer.AutoReset = true;

            beatTimer.Elapsed += SendLog;

            beatTimer.Start();
        }

        private async void ShutdownVM(object sender, SessionEndingEventArgs e)
        {
            List<KeyValuePair<string, string>> urlContentList = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("roleName", roleName),
                new KeyValuePair<string, string>("timeStamp", DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt")),
                new KeyValuePair<string, string>("comment", "Stopped"),
                new KeyValuePair<string, string>("courseID", courseID.ToString()),
                new KeyValuePair<string, string>("userID", userID.ToString()),
                new KeyValuePair<string, string>("veProfileID", veProfileID.ToString())
            };

           await ApiCall("POST", webApiUrl, "api/VirtualMachineLogs", new FormUrlEncodedContent(urlContentList));
              
        }

        private async void SendLog(object sender, ElapsedEventArgs e)
        {
            //Console.WriteLine("TEST " + DateTime.Now.ToLongTimeString());

            List<KeyValuePair<string, string>> urlContentList = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("roleName", roleName),
                new KeyValuePair<string, string>("timeStamp", DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt")),
                new KeyValuePair<string, string>("courseID", courseID.ToString()),
                new KeyValuePair<string, string>("userID", userID.ToString()),  
                new KeyValuePair<string, string>("veProfileID", veProfileID.ToString())
            };
            
            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();

                Console.WriteLine("Server: " + server.ServerName);
                Console.WriteLine("Session Count: " + server.GetSessions().Count);

                foreach (ITerminalServicesSession session in server.GetSessions())
                {
                    NTAccount account = session.UserAccount;

                    if (account != null && account.ToString().IndexOf(System.Environment.MachineName) > -1)
                    {
                        Console.WriteLine("Account: " + account.ToString());
                        Console.WriteLine("Idle: " + session.IdleTime);

                        if (session.IdleTime >= maxIdleTimeSpan)
                        {
                            Console.WriteLine("Trigger shutdown");

                            urlContentList.Add(new KeyValuePair<string, string>("comment", "Stopped"));

                            await ApiCall("POST", webApiUrl, "api/VirtualMachineLogs", new FormUrlEncodedContent(urlContentList));

                        }
                        else if (isStarting)
                        {
                            isStarting = false;

                            urlContentList.Add(new KeyValuePair<string, string>("comment", "Starting"));

                            await ApiCall("POST", webApiUrl, "api/VirtualMachineLogs", new FormUrlEncodedContent(urlContentList));
                        }
                        else
                        {
                            urlContentList.Add(new KeyValuePair<string, string>("comment", "Running"));

                            await ApiCall("POST", webApiUrl, "api/VirtualMachineLogs", new FormUrlEncodedContent(urlContentList));
                        }
                    }
                }

            }
        }

        private void GetURL(String prefix)
        {
            var ret = ApiCall("GET", webApiUrl, "api/Tenants/url?prefix=" + prefix, null);
            webApiUrl = ret.Result.Replace("\"", "");
        }


        protected override void OnStart(string[] args)
        {
            SetupService();
        }

        protected override void OnStop()
        {
        }

        protected async override void OnShutdown()
        {
            List<KeyValuePair<string, string>> urlContentList = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("roleName", roleName),
                new KeyValuePair<string, string>("timeStamp", DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt")),
                new KeyValuePair<string, string>("comment", "Stopped"),
                new KeyValuePair<string, string>("courseID", courseID.ToString()),
                new KeyValuePair<string, string>("userID", userID.ToString()),
                new KeyValuePair<string, string>("veProfileID", veProfileID.ToString())
            };

            await ApiCall("POST", webApiUrl, "api/VirtualMachineLogs", new FormUrlEncodedContent(urlContentList));
             
        }
        
        private async Task<string> ApiCall(string method, string baseAddress, string url, FormUrlEncodedContent data = null)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseAddress);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = null;

            if (method == "POST")
            {
                response = await client.PostAsync(url, data);
            }
            else if (method == "GET")
            {
                response = await client.GetAsync(url);
            }
            else if (method == "DELETE")
            {
                response = await client.DeleteAsync(url);
            }

            string result = await response.Content.ReadAsStringAsync();

            return result;

        }
    }
}

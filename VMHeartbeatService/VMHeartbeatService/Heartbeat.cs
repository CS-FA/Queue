using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using Cassia;
using Microsoft.Win32;
using Timer = System.Timers.Timer;
using Newtonsoft.Json;
//using System.Web.Configuration;

namespace VMHeartbeatService
{
    public class Heartbeat
    {
        private Timer BeatTimer { get; set; }
        private int CourseId { get; set; }
        private int VeProfileId { get; set; }
        private int UserId { get; set; }
        private int MachineInstance { get; set; }
        private string RoleName { get; set; }
        private int IntervalTime { get; set; }
        private int MaxIdleTime { get; set; }
        private TimeSpan MaxIdleTimeSpan { get; set; }
        private bool IsStarting { get; set; }
        private string WebApiUrl { get; set; }
        private ITerminalServicesManager Manager { get; set; }

        bool isShutDown = false;

        public void SetupService()
        {
            //var configXmlReader = new XmlTextReader(@"C:\CloudSwyft\Heartbeat\Config.xml");

            //configXmlReader.ReadToDescendant("WebApiUrl");

            //WebApiUrl = configXmlReader.ReadElementContentAsString();
            WebApiUrl = "https://cs-heartbeat-api.cloudswyft.com/cha/api/";
            IsStarting = true;
            char[] dashSplit = { '-' };

            var nameSplit = Environment.MachineName.Split(dashSplit);            
            var client = nameSplit[0];
            GetUrl(client);
            var callResponse = ApiCall("GET", WebApiUrl, string.Format("VirtualMachineMappings/GetVmMapping?roleName={0}", Environment.MachineName));
           

            var vmMapping = JsonConvert.DeserializeObject<VirtualMachineMapping>(callResponse);

            RoleName = string.Format("{0}-{1}-{2}-{3}", client, vmMapping.CourseID, vmMapping.VEProfileID, vmMapping.UserID);

            CourseId = vmMapping.CourseID;
            VeProfileId = vmMapping.VEProfileID;
            UserId = vmMapping.UserID;
            MachineInstance = vmMapping.MachineInstance;

            IntervalTime = Convert.ToInt32(ApiCall("GET", WebApiUrl, "VirtualMachines/HeartbeatInterval")) * 1000;
            MaxIdleTime = Convert.ToInt32(ApiCall("GET", WebApiUrl, "VirtualMachines/MaxIdleTime"));
            MaxIdleTimeSpan = new TimeSpan(0, 0, MaxIdleTime);

            // Cassia
            Manager = new TerminalServicesManager();

            // Shutdown intercept
            SystemEvents.SessionEnding += ShutdownVm;

            while (true)
            {
                Thread.Sleep(IntervalTime);
                SendLog();
            }

            }

        private void ShutdownVm(object sender, SessionEndingEventArgs e)
        {

            var logs = new VirtualMachineLogVM()
            {
                roleName = RoleName,
                timeStamp = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt"),
                courseID = CourseId.ToString(),
                userID = UserId.ToString(),
                veProfileID = VeProfileId.ToString(),
                machineInstance = MachineInstance.ToString(),
                comment = "Stopped",
                machineName = Environment.MachineName
            };

            ApiCall("POST", WebApiUrl, "VirtualMachineLogs", JsonConvert.SerializeObject(logs));

        }

        private void SendLog()
        {
            if (isShutDown)
                return;

            var logs = new VirtualMachineLogVM()
            {
                roleName = RoleName,
                timeStamp = DateTime.UtcNow.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt"),
                courseID = CourseId.ToString(),
                userID = UserId.ToString(),
                veProfileID = VeProfileId.ToString(),
                machineInstance = MachineInstance.ToString(),
                machineName = Environment.MachineName
            };

            using (var server = Manager.GetLocalServer())
            {
                server.Open();

                Console.WriteLine("Server: " + server.ServerName);
                Console.WriteLine("Session Count: " + server.GetSessions().Count);

                foreach (var session in server.GetSessions())
                {
                    var account = session.UserAccount;

                    if (account == null || account.ToString().IndexOf(Environment.MachineName) <= -1) continue;
                    Console.WriteLine("Account: " + account);
                    Console.WriteLine("Idle: " + session.IdleTime);
                    
                    if (session.IdleTime >= MaxIdleTimeSpan)
                    {
                        Console.WriteLine("Trigger shutdown");
                        logs.comment = "Stopped";
                        isShutDown = true;
                        ApiCall("POST", WebApiUrl, "VirtualMachines/ShutdownHeartBeatVM", JsonConvert.SerializeObject(logs));
                        
                    }
                    else if (IsStarting)
                    {
                        IsStarting = false;
                        logs.comment = "Starting";
                    }
                    else 
                    {
                        logs.comment = "Running";
                       
                    }
                    ApiCall("POST", WebApiUrl, "VirtualMachineLogs", JsonConvert.SerializeObject(logs));


                }
            }
        }

        private void GetUrl(string prefix)
        {            
            var ret = ApiCall("GET", WebApiUrl, "/Tenant/url?prefix=" + prefix, null);
            WebApiUrl = ret.Replace("\"", "");
            //WebApiUrl = WebApiUrl;
        }

        private static string ApiCall(string method, string baseAddress, string url, string data = null)
        {
            try
            {
                var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = new HttpResponseMessage();

                switch (method)
                {
                    case "POST":
                        response = client.PostAsync(client.BaseAddress.AbsoluteUri + url, new StringContent(data, Encoding.UTF8, "application/json")).Result;
                        break;
                    case "GET":
                        response = client.GetAsync(client.BaseAddress.AbsoluteUri + url).Result;
                        break;
                    case "DELETE":
                        response = client.DeleteAsync(client.BaseAddress.AbsoluteUri + url).Result;
                        break;
                }

                if (!response.IsSuccessStatusCode) return response.ReasonPhrase;
                // by calling .Result you are performing a synchronous call
                var responseContent = response.Content;
                var responseString = responseContent.ReadAsStringAsync().Result;

                return responseString;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}


#load "AzureOperation.csx"

using System;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Configuration;
using System.Linq;
using System.Threading;

public static void Run(string myQueueItem, TraceWriter log)
{
    AzureOperation s = new AzureOperation();
    log.Info($"C# Queue trigger function processed: {myQueueItem}");
}

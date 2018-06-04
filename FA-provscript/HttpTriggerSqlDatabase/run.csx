using System.Net;
using Dapper;
using System.Data.SqlClient;
using System.Configuration;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    var successful =true;
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["SqlConnection"].ConnectionString;
        
        using(var connection = new SqlConnection(cnnString))
        {
            connection.Open();
            
            var rLog = await req.Content.ReadAsAsync<LogRequest>();
        
            connection.Execute("INSERT INTO ([CloudLabsSchedules]) VALUES (@VEProfileID, @UserId, @ScheduledBy, @DateCreated, @LabHoursRemaining, @LabHoursTotal)", rLog);
            //connection.Execute("INSERT INTO ([CloudLabsSchedules]) VALUES (392, 72, 'me', '1/1/17', 99, 22)", rLog);
            
            log.Info("Log added to database successfully!");
        }
    }
    catch
    {
        successful=false;
    }
    
    return !successful
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Unable to process your request!")
        : req.CreateResponse(HttpStatusCode.OK, "Data saved successfully!");
}

public class LogRequest
{
    public int Id{get;set;}
    public string Log{get;set;}
}
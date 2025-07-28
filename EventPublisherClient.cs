using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public static class EventPublisherClient
    {
        public static Response PublishHealthCheckNotification()
        {
            return PublishNotification("healthcheckfile", NotificationType.HealthCheck, new Dictionary<string, string>());
        }

        // POSTs json payload (with notification name, file url and additional properties map if available) to endpoint url.
        // It is expected the server responds with '201 - Accepted'. Server can process request asynchronously. Logs an error if request fails.
        // Note: Endpoint authentication is not yet supported. 
        public static Response PublishNotification(string sourceFileUrl, NotificationType type, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(Configuration.EndpointUrl))
            {
                return new Response(false, "Endpoint url is empty.");
            }
            using (var wb = new WebClient())
            {
                var url = Configuration.EndpointUrl;
                wb.Headers[HttpRequestHeader.UserAgent] = "MusicBee";
                wb.Headers[HttpRequestHeader.ContentType] = "application/json";
                wb.Headers["MusicBee_PlayerMachineName"] = Environment.MachineName;

                var fileUrlBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(sourceFileUrl));

                var dataAsJsonString = string.Join(",", data.Select(entry => "\"" + entry.Key + "\":\"" + entry.Value + "\""));

                try
                {
                    Console.WriteLine("Sending " + type.ToString() + " notification request ...");
                    var response = wb.UploadString(url, "POST", "{\"type\":\"" + type.ToString() + "\",\"filePath\":\"" + fileUrlBase64Encoded + "\",\"additionalProperties\":{" + dataAsJsonString + "}}");
                    Console.WriteLine(type.ToString() + " notification request has successfully been sent. " + response);
                    return new Response(true, "Success");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Request failed: " + e.Message);
                    return new Response(false, e.Message);
                }
            }
        }
    }
}

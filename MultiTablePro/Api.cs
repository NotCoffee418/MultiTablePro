using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MultiTablePro
{
    sealed class Api
    {        
        /// <summary>
        /// Should probably only be called by Api.Request()
        /// </summary>
        /// <param name="request"></param>
        /// <param name="postData"></param>
        /// <returns>Returns task containing json string.</returns>
        internal static dynamic ApiRequest(string request, Dictionary<string, string> postData)
        {
            string responseFromServer = "";
            try
            {
                // Prepare POST request to our API
                WebRequest wReq = WebRequest.Create("https://multitablepro.com/api/" + request);
                wReq.ContentType = "application/x-www-form-urlencoded";
                wReq.Method = "POST";
                wReq.Timeout = 5000;

                // Convert postData dictionary to string
                string postDataStr = string.Join("&", postData.Select(kvp => $"{kvp.Key}={kvp.Value}").ToArray());

                // Create the POST Data and convert it to byteArray
                byte[] byteArray = Encoding.UTF8.GetBytes(postDataStr);
                wReq.ContentLength = byteArray.Length;
                
                // Write data to the request
                using (Stream dataStream = wReq.GetRequestStream())
                {                    
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                // Get the response                
                using (WebResponse wResponse = wReq.GetResponse())
                {
                    // Log HTTP Status code
                    Logger.Log("ApiRequest HTTP Status:" + ((HttpWebResponse)wResponse).StatusDescription);

                    // Read the response stream
                    using (var reader = new StreamReader(wResponse.GetResponseStream()))
                    {
                        //Read the content
                        responseFromServer = reader.ReadToEnd();
                        Logger.Log(responseFromServer);
                    }
                }
            }
            catch (WebException ex)
            {
                // Display the approperiate error
                switch (ex.Status)
                {
                    case var xs when xs == WebExceptionStatus.Timeout || xs == WebExceptionStatus.NameResolutionFailure:
                        responseFromServer = SimulateApiError("Failed to connect to API server. You must have an internet connection to use this application.");
                        break;
                    case WebExceptionStatus.ProtocolError:
                        var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                        if (statusCode == HttpStatusCode.InternalServerError)
                            responseFromServer = SimulateApiError("Internal Server Error. This maybe due to scheduled maintenance. Please contact support if the issue persists.");
                        else responseFromServer = SimulateApiError("HTTP Error " + statusCode.ToString() + ". Please contact support.");
                        break;
                    default:
                        responseFromServer = SimulateApiError("Undefined API error (WebException): " + ex.Message + ". Please contact support if this issue persists.");
                        break;
                }
            }
            catch (Exception ex) // in case something else goes wrong
            {
                SimulateApiError("Undefined API Error (unknown): " + ex.Message);
            }

            // Convert to dynamic json object
            try
            {
                return JsonConvert.DeserializeObject(responseFromServer);
            }
            catch
            {
                Logger.Log($"API: Failed to deserialize {request} request:{Environment.NewLine}{responseFromServer}", Logger.Status.Error);
                return JsonConvert.DeserializeObject(
                    SimulateApiError("Failed to deserialize API response. This may be due to scheduled maintenance. Please try again in a few minutes and contact support if the error persists including your log file.")
                    );
            }
        }

        /// <summary>
        /// Returns an error formatted in the same way as an API error for failed web requests
        /// </summary>
        /// <param name="errorText"></param>
        /// <param name="severity">I don't see a scenario where this is not error. but maybe later.</param>
        /// <returns></returns>
        private static dynamic SimulateApiError(string errorText, Logger.Status severity = Logger.Status.Error)
        {
            

            // Create fake response object & return it as json string
            var errObj = new Dictionary<string, object>
            {
                { "result", null },
                { "errors", new string[] { errorText } }
            };
            return JsonConvert.SerializeObject(errObj);
        }
    }
}

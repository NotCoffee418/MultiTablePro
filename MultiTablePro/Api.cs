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
        public struct ApiResponse
        {
            public ApiResponse(string jsonString)
            {
                // Deserialize json & store it
                try
                {
                    dynamic dJson = JsonConvert.DeserializeObject(jsonString);
                    _result = dJson.result;
                    _errors = dJson.errors.ToArray();
                }
                catch
                {
                    Logger.Log($"API: Failed to deserialize or access dynamic object: {jsonString}", Logger.Status.Error);
                    _result = null;
                    _errors = new string[] { "Failed to deserialize API response. This may be due to scheduled maintenance. Please try again in a few minutes and contact support if the error persists including your log file." };
                }
            }

            public ApiResponse(Exception ex)
            {
                // Log the error
                Logger.Log("API: " + ex.Message, Logger.Status.Error);

                // Store the error
                _result = null;
                _errors = new string[] { ex.Message };
            }

            private readonly dynamic _result;
            private readonly string[] _errors;

            public dynamic Result
            {
                get { return _result; }
            }

            public string[] Errors
            {
                get { return _errors; }
            }
        }

        public static void test()
        {
            var x = ApiRequest("test");

        }

        /// <summary>
        /// API request returning JSON dynamic data
        /// </summary>
        /// <param name="request">function to be called server side</param>
        /// <param name="postData">post param dictionary</param>
        /// <returns>JSON dynamic</returns>
        internal static ApiResponse ApiRequest(string request, Dictionary<string, string> postData = null)
        {
            string responseFromServer = "";
            try // Outer exception handler - passes errors to ApiRespones
            {
                try // inner exception handler - provides custom error messages
                {
                    // Prepare request to our API
                    Logger.Log($"API: Creating request {request}");
                    WebRequest wReq = WebRequest.Create("https://multitablepro.com/api/" + request);
                    wReq.Timeout = 5000;

                    // Add post data if any is given
                    if (postData != null)
                    {
                        wReq.ContentType = "application/x-www-form-urlencoded";
                        wReq.Method = "POST";

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
                    }

                    // Get the response                
                    using (WebResponse wResponse = wReq.GetResponse())
                    {
                        // Log HTTP Status code
                        Logger.Log("API: HTTP Status:" + ((HttpWebResponse)wResponse).StatusDescription);

                        // Read the response stream
                        using (var reader = new StreamReader(wResponse.GetResponseStream()))
                        {
                            //Read the content
                            responseFromServer = reader.ReadToEnd();
                            Logger.Log("API: Response: " + responseFromServer);

                            // Check for empty response
                            if (responseFromServer == "")
                                throw new Exception("Empty response from server");
                        }
                    }
                }
                catch (WebException ex)
                {
                    // Display the approperiate error
                    switch (ex.Status)
                    {
                        case var xs when xs == WebExceptionStatus.Timeout || xs == WebExceptionStatus.NameResolutionFailure:
                            throw new Exception("Failed to connect to API server. You must have an internet connection to use this application.");
                        case WebExceptionStatus.ProtocolError:
                            var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                            if (statusCode == HttpStatusCode.InternalServerError)
                                throw new Exception("Internal Server Error. This maybe due to scheduled maintenance. Please contact support if the issue persists.");
                            else throw new Exception("HTTP Error " + statusCode.ToString() + ". Please contact support.");
                        default:
                            throw new Exception("Undefined API error (WebException): " + ex.Message + ". Please contact support if this issue persists.");
                    }
                }
                catch (Exception ex) // in case something else goes wrong
                {
                    throw new Exception("Undefined API Error (unknown): " + ex.Message);
                }
            }
            catch (Exception ex) // Error handler caught an exception
            {
                // Pass the exception to an ApiResponse
                return new ApiResponse(ex);
            }

            // No errors found, return ApiResponse
            return new ApiResponse(responseFromServer);
        }
    }
}

#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives; 
using Newtonsoft.Json;
using System;

public static string ENVIRONMENT_NAME   = Environment.GetEnvironmentVariable("ENVIRONMENT_NAME");
public static string ADMIN_USERNAME     = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
public static string ADMIN_PASSWORD     = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
public static string BASE_URI           = Environment.GetEnvironmentVariable("BASE_URI");
public static string COMPANY_ID         = Environment.GetEnvironmentVariable("COMPANY_ID"); 

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    HttpClient client = new HttpClient();

    switch(req.Method)
    {
        case "GET":
        {
            log.LogInformation("C# HTTP trigger function processed a GET request.");
            string AuthCode = "";
            string State = "";
            AuthCode = req.Query["code"];
            State = req.Query["state"];

            if(AuthCode != null && State != null)
            {
                string resp = "";
                try{
                    resp = await PostAuthCodeToBCAsync(JsonConvert.SerializeObject(new { code = AuthCode ,state = State}),log);
                    log.LogInformation(resp);
                    dynamic respData = JsonConvert.DeserializeObject(resp);
                    string responseStringMessage = "";
                    switch(respData?.value.ToString())
                    {
                        case "OK": 
                            responseStringMessage = "Authorization successfully passed. Please refresh the Square Settings page in Business Central. You can close this tab.";
                            break; 
                        case "FAILED":
                            responseStringMessage = "Authorization failed. Failed to retrieve access token. You can close this tab.";
                            break; 
                    }
                    return new OkObjectResult(responseStringMessage);
                }
                catch(Exception ex) 
                {
                    return new BadRequestObjectResult(ex.Message + ": " + resp); 
                }
            }
            else
            {
                return new BadRequestObjectResult("Authorization denied. You chose to deny access to the app.");
            }
            break;
        }
        case "POST" :  
        {
            log.LogInformation("C# HTTP trigger function processed a POST request.");
            try{
                string documentContents = "{\"key1\":\"value\"}";

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                string resp = await PostEventToBCAsync(JsonConvert.SerializeObject(new { inputJson = requestBody}), log);
log.LogInformation(resp);
                return new OkObjectResult(resp);
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); 
            }
        break; 
        }
        default:
        {
            return new BadRequestObjectResult($"HTTPMethod {req.Method} is not supported!"); 
        }
    }
    
}

public static async Task<string> PostEventToBCAsync(string jsonBody, ILogger log)
{
    HttpClient client = new HttpClient(); 
    string svcCredentials = EncodeTo64(ADMIN_USERNAME + ":" + ADMIN_PASSWORD));
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {svcCredentials}");
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    string postUri = $"{BASE_URI}/SquareOAuthService_GetSquareWebhookRequest?company={COMPANY_ID}";
    log.LogInformation(postUri);
    log.LogInformation(jsonBody);
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync(); 
    return responseString; 
}

public static async Task<string> PostAuthCodeToBCAsync(string jsonBody, ILogger log)
{
    HttpClient client = new HttpClient();
    string svcCredentials = EncodeTo64(ADMIN_USERNAME + ":" + ADMIN_PASSWORD));
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {svcCredentials}");
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    string postUri = $"{BASE_URI}/SquareOAuthService_GetAuthorizationCode?company={COMPANY_ID}";
    log.LogInformation(postUri);
    log.LogInformation(data.ReadAsStringAsync().Result);
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync();
    return responseString; 
}

public static string EncodeTo64(string toEncode)
{
    byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
    string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
    return returnValue;
}

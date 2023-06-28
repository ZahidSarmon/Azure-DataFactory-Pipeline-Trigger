using ADF.Web.Others;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace ADF.Web.Services;

public class TokenHelper
{
    public static string GetToken(string tenantId, string clientId, string clientSecret, string userId, string password)
    {
        return AcquireToken(tenantId, clientId, clientSecret,userId,password);
    }

    private static string AcquireToken(string tenantId, string clientId, string clientSecret, string userId, string password)
    {

        var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        var request = new RestRequest(Method.POST);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", clientId);
        request.AddParameter("client_secret", clientSecret);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("scope", "https://graph.microsoft.com/.default");
        IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();

        if (restResponse.StatusCode == HttpStatusCode.OK)
        {
            var response = JsonConvert.DeserializeObject<TokenResponse>(restResponse.Content);
            return response.access_token;
        }

        throw new Exception("Failed to get token");
    }
}

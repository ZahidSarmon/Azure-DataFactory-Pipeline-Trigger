using ADF.Web.Others;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace ADF.Web.Services;

public class ADFServices
{
    public static IEnumerable<string> GetPipelines(PipelineParam pipelineParam)
    {
        try
        {
            string base_url = "https://management.azure.com/subscriptions/";
            pipelineParam.Version = "2018-06-01";
            var client = new RestClient($"{base_url}{pipelineParam.SubscriptionId}/resourceGroups/{pipelineParam.ResourceGroupName}/providers/Microsoft.DataFactory/factories/{pipelineParam.FactoryName}/pipelines?api-version={pipelineParam.Version}");
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", "Bearer "+pipelineParam.Token);
            IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();

            if (restResponse.StatusCode == HttpStatusCode.OK)
            {
                var response = JsonConvert.DeserializeObject<PipelineResponse>(restResponse.Content);

                if (response is null) return new List<string>();

                return response.Value.Select(x => x.Name).ToList();
            }
            return new List<string>(); ;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static string GetRunId(CreateRunParam param)
    {
        try
        {
            param.Version = "2018-06-01";
            var url = $"https://management.azure.com/subscriptions/{param.SubscriptionId}/resourceGroups/{param.ResourceGroupName}/providers/Microsoft.DataFactory/factories/{param.FactoryName}/pipelines/{param.PipelineName}/createRun?api-version={param.Version}";
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", "Bearer " + param.Token);
            IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();

            if (restResponse.StatusCode == HttpStatusCode.OK)
            {
                var response = JsonConvert.DeserializeObject<CreateRunResponse>(restResponse.Content);

                if (response is null) return string.Empty;

                return response.RunId;
            }

            return string.Empty; 
        }
        catch (Exception)
        {
            throw;
        }
    }
    public static string GetToken(string teanantId, string grant_type,string client_id, string client_secret,string resource)
    {
        var url = $"https://login.microsoftonline.com/{teanantId}/oauth2/token";
        try
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter(nameof(grant_type), grant_type);
            request.AddParameter(nameof(client_id), client_id);
            request.AddParameter(nameof(client_secret), client_secret);
            request.AddParameter(nameof(resource), resource);

            IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();

            if (restResponse.StatusCode == HttpStatusCode.OK)
            {
                var response = JsonConvert.DeserializeObject<TokenResponse>(restResponse.Content);

                if (response is null) return string.Empty;

                return response.access_token;
            }
        }
        catch (Exception)
        {
            throw;
        }
        return string.Empty;
    }
}

public class PipelineParam
{
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    public string FactoryName { get; set; }
    public string Version { get; set; }
    public string Token { get; set; }
}

public class CreateRunParam
{
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    public string FactoryName { get; set; }
    public string Version { get; set; }
    public string PipelineName { get; set; }
    public string Token { get; set; }
}
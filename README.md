# Azure Data Factory Pipeline Trigger

**Objective :** Develop a web MVC application where users can initiate various pipelines retrieved from the Azure Data Factory Rest API. The application should allow users to modify the user-friendly names of the pipelines and select/deselect pipelines that can be executed.

Develop a web MVC application named ADF.Web (example project name) that encompasses the user interface, data transactions, controllers, models, and handles the REST API for Azure Data Factory services.

**Configure ADF.Web :**

Initially, it is necessary to install a few NuGet packages.

    Microsoft.EntityFrameworkCore.Design
    Microsoft.EntityFrameworkCore.Tools
    Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
    Microsoft.EntityFrameworkCore.SqlServer
    Newtonsoft.Json
    RestSharp
    Microsoft.AspNetCore.Authentication.AzureAD.UI
    Microsoft.Azure.Management.DataFactory
    Microsoft.Identity.Web
    Microsoft.Identity.Web.UI
    Microsoft.VisualStudio.Azure.Containers.Tools.Targets


Make sure to configure all your credentials in the appsettings.json file. Additionally, create two more JSON settings files: appsettings.development.json for development mode and appsettings.production.json for production mode. In the appsettings.json file, include the following information:

    "ADFConfigure": {
    "Instance": "...",
    "TenantId": "...",
    "ClientId": "...",
    "ClientSecret": "..",
    "FactoryName": "...",
    "ResourceGroupName": "..",
    "SubscriptionId": "..",
    "GrantType": "..",
    "Resource": "..",
    },

And *appsetting.development.json* and *appsetting.production.json* here 

    "AzureAd": {
        "Instance": "...",
        "Domain": "...",
        "TenantId": "...",
        "ClientId": "...",
        "CallbackPath": "...",
        "ClientSecret": "...",
    },
    "ConnectionStrings": {
        "dbConnection": "..."
    }

To retrieve JSON values from appsettings and appsettings.production/development, it is necessary to create a class property. Use the following syntax to define the property:

    public static class AppConfig{
        public static AzureAd AzureAd { get; set; }
        public static ADFConfigure ADFConfigure { get; set; }
    }
    public class AzureAd{
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string FactoryName { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string ClientCredentials { get; set; }
        public string Resource { get; set; }
    }
    public class ADFConfigure{
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string FactoryName { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string GrantType { get; set; }
        public string Resource { get; set; }
    }

Next, we should create a model to store our pipeline data. Define a model with the appropriate table name and schema name for storing the data. In this model, you can include a common set of properties by inheriting from the EntityBase class. Here is the definition of the EntityBase class:

    public abstract class EntityBase<T>{
        [Key]
        public T Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public string LastModifiedBy { get; set; }
    }

Finally, the resulting entity will look like this:

    public static class AppConstants{
        public const string Schema = "your_schema_name";
    }
    [Table(nameof(Pipeline), Schema = AppConstants.Schema)]
    public class Pipeline : EntityBase<int>{
        public string Name { get; set; }
        public string? DisplayName { get; set; } = "";
        public bool IsRunnable { get; set; }
        public DateTime? LastRunDate { get; set; }
        public string? LastRunBy { get; set; }
        public string? Status { get; set; }
        public string? LastRunId { get; set; }
    }

To facilitate data storage and updates, create a class that inherits from the DbContext class. Let's name this class "AppDbContext" as an example. Here is the definition of the AppDbContext class:

    public partial class AppDbContext : DbContext{
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
            public virtual DbSet<Pipeline> Pipelines { get; set; }
            protected override void OnModelCreating(ModelBuilder modelBuilder){
                base.OnModelCreating(modelBuilder);
                }
    }

Now, register the application services. Here is the configuration: 

    services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("dbConnection")));
    services.AddSingleton(_configuration.GetSection(nameof(AzureAd)).Get<AzureAd>());
    services.AddSingleton(_configuration.GetSection(nameof(ADFConfigure)).Get<ADFConfigure>());
    services.AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    }).AddRazorRuntimeCompilation();
    services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromDays(60);
    });
    services.AddHttpContextAccessor();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
    services.AddRazorPages().AddMicrosoftIdentityUI();
    services.AddScoped<IDataServices, DataServices>();

Here, configure the application to automatically redirect users to the Microsoft Azure Login/Authenticate service. Register the following line of code:

    services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(_configuration.GetSection(nameof(AzureAd)));

Next, make sure to include the following lines in the middleware configuration:

    app.UseAuthentication();
    app.UseAuthorization();

To execute an Azure Pipeline using the Azure Data Factory REST API, we need to generate a token. This can be achieved by using an HTTP request in RestSharp. For this purpose, we will utilize a common utility method.

    public static async Task<IRestResponse<T>> ExecuteAsyncRequest<T>(this RestClient client, IRestRequest request) where T : class, new(){
        try{
            var taskCompletionSource = new TaskCompletionSource<IRestResponse<T>>();
            client.ExecuteAsync<T>(request, restResponse =>{
                if (restResponse.ErrorException != null){
                    const string message = "Error retrieving response.";
                    throw new ApplicationException(message, restResponse.ErrorException);
                }
                taskCompletionSource.SetResult(restResponse);
            });
            return await taskCompletionSource.Task;
        }
        catch (Exception){throw;}
    }

To authenticate Azure Data Factory with Azure Data Factory credentials, we need to generate a token. This token serves the purpose of authentication.

**Token generate for only login:**

    private static string AcquireToken(string tenantId, string clientId, string clientSecret, string userId, string password){
        var client = new RestClient(url);
        var request = new RestRequest(Method.POST);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", clientId);
        request.AddParameter("client_secret", clientSecret);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("scope", scope);
        IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();
        if (restResponse.StatusCode == HttpStatusCode.OK){
            var response = JsonConvert.DeserializeObject<TokenResponse>(restResponse.Content);
            return response.access_token;
        }
        throw new Exception("Failed to get token");
    }

To make requests to the Azure Data Factory services REST API, it is necessary to generate a separate token specifically for authenticating all Azure Pipeline APIs.

**Token generate for pipeline run:**

    var client = new RestClient(url);
    var request = new RestRequest(Method.POST);
    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
    request.AddParameter(nameof(grant_type), grant_type);
    request.AddParameter(nameof(client_id), client_id);
    request.AddParameter(nameof(client_secret), client_secret);
    request.AddParameter(nameof(resource), resource);
    IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();
    if (restResponse.StatusCode == HttpStatusCode.OK){
        var response = JsonConvert.DeserializeObject<TokenResponse>(restResponse.Content);
        if (response is null) return string.Empty;
        return response.access_token;
    }

Once the pipeline token is generated, the next step is to retrieve a list of pipelines. This list will be used to execute each pipeline using the generated token.

    var client = new RestClient(url);
    var request = new RestRequest(Method.GET);
    request.AddHeader("Content-Type", "application/json");
    request.AddHeader("Authorization", "Bearer {your_generated_token}");
    IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();
    if (restResponse.StatusCode == HttpStatusCode.OK){
        var response = JsonConvert.DeserializeObject<PipelineResponse>(restResponse.Content);
        if (response is null) return new List<string>();
        return response.Value.Select(x => x.Name).ToList();
    }

To trigger each pipeline, we need to obtain the runId first. To achieve this, we make a request using the following syntax:

    var client = new RestClient(url);
    var request = new RestRequest(Method.POST);
    request.AddHeader("Content-Type", "application/json");
    request.AddHeader("Authorization", "Bearer {your_generate_token}");
    IRestResponse restResponse = RestSharpHelper.ExecuteAsyncRequest<object>(client, request).GetAwaiter().GetResult();
    if (restResponse.StatusCode == HttpStatusCode.OK){
        var response = JsonConvert.DeserializeObject<CreateRunResponse>(restResponse.Content);
        if (response is null) return string.Empty;
        return response.RunId;
    }

Subsequently, we trigger the pipeline and retrieve the updated status of the pipeline run. This can be done by making a request using the following syntax:

    ServiceClientCredentials cred = new TokenCredentials(your_token);
    var client = new DataFactoryManagementClient(cred) { SubscriptionId = your_SubscriptionId };
    PipelineRun pipelineRun = client.PipelineRuns.Get(your_ResourceGroupName, your_FactoryName, your_rundId);
    return pipelineRun.Status;

Following that, we need to create a service class responsible for various pipeline operations. This class will generate a list of pipelines, store it in the database, and return it to the user interface. Additionally, it will include methods for updating pipelines and running pipelines. Here is a simplified method outline for these transactions:

    ResponseBody UpdatePipeline(Pipeline pipeline,string user);
    ResponseBody RunPipeline(List<int> pipelines, string runBy);
    ResponseBody RunPipelineStandalone(int id, string runBy);
    ResponseBody StartPipeline(List<int> pipelines);
    Task<IEnumerable<Pipeline>> GetRunnablePipelineList();
    ResponseBody PostPipelines(string user);
    Task<IEnumerable<Pipeline>> GetPipelineList();

Finally, the controllers will appear as follows:

    [HttpGet]
    public async Task<IActionResult> GetPipelineList() => Ok(await _dataService.GetPipelineList());
    [HttpGet]
    public async Task<IActionResult> GetRunnablePipelineList() => Ok(await _dataService.GetRunnablePipelineList());
    [HttpPost]
    public IActionResult UpdatePipeLine(Pipeline pipeline) => Ok(_dataService.UpdatePipeline(pipeline, User.Identity.Name));
    [HttpGet]
    public IActionResult SyncPipeLine() => Ok(_dataService.PostPipelines(User.Identity.Name));
    [HttpPost]
    public IActionResult RunPipeline(List<int> pipelines) => Ok(_dataService.RunPipeline(pipelines, User.Identity.Name));
    [HttpPost]
    public IActionResult RunPipelineStandalone(int pipelineId) => Ok(_dataService.RunPipelineStandalone(pipelineId, User.Identity.Name));
    [HttpPost]
    public IActionResult StartPipeline(List<int> pipelines) => Ok(_dataService.StartPipeline(pipelines));



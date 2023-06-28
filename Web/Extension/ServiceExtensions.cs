using ADF.Web.Data;
using ADF.Web.Others;
using ADF.Web.Repositories;
using ADF.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace ADF.Web.Extension;

public static class ServiceExtensions
{
    public static IServiceCollection AddADFServices(this IServiceCollection services,IConfiguration _configuration)
    {
        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        #region Configure Session

        services.AddMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromDays(60);
            options.Cookie.Name = "StarterAppSession";
        });

        #endregion
        #region Reading Configuration

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString("dbConnection")));
        services.AddSingleton(_configuration.GetSection(nameof(AzureAd)).Get<AzureAd>());
        services.AddSingleton(_configuration.GetSection(nameof(ADFConfigure)).Get<ADFConfigure>());

        #endregion

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(_configuration.GetSection(nameof(AzureAd)));

        services.Configure<FormOptions>(options => { options.ValueCountLimit = int.MaxValue; });

        services.AddControllersWithViews(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        }).AddRazorRuntimeCompilation();

        services.AddHttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
        services.AddRazorPages()
             .AddMicrosoftIdentityUI();

        services.AddScoped<IDataServices, DataServices>();

        services.AddTransient(typeof(IUnitRepository<>),typeof(MasterRepository<>));

        return services;
    }
}

using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using RssFeeder.Mvc.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    var client = services.GetRequiredService<TelemetryClient>();

    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
#if DEBUG
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.Seq("http://localhost:5341")       // docker run --rm -it -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
#endif
        .WriteTo.ApplicationInsights(client, TelemetryConverter.Traces)
        ;
});

// Add services to the container
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);
builder.Services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<SerilogMvcLoggingAttribute>();
            }).AddMicrosoftIdentityUI();

builder.Services.AddSingleton<IDatabaseService>(InitializeCosmosClientInstanceAsync(builder.Configuration.GetSection("CosmosDb")));
builder.Services.AddSingleton<AppVersionInfo>();
builder.Services.AddMediatR(typeof(Program));
builder.Services.AddMemoryCache();
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


var options = new RewriteOptions()
    .AddRewrite(@"^content/rss/drudge\.xml", "api/rss/drudge-report", skipRemainingRules: true);
app.UseRewriter(options);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest);

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>
/// Creates a Cosmos DB database and a container with the specified partition key. 
/// </summary>
/// <returns></returns>
static CosmosDbService InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
{
    string? databaseName = configurationSection.GetSection("DatabaseName").Value;
    string? containerName = configurationSection.GetSection("ContainerName").Value;
    string? account = configurationSection.GetSection("Account").Value;
    string? key = configurationSection.GetSection("Key").Value;
    CosmosClient client = new CosmosClient(account, key);
    CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
    Database database = client.GetDatabase(databaseName);

    return cosmosDbService;
}

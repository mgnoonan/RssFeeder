Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
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

    builder.Services.AddControllers();
    builder.Services.AddApplicationInsightsTelemetry();

    builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);
    builder.Services.AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
        options.Filters.Add<SerilogMvcLoggingAttribute>();
    }).AddMicrosoftIdentityUI();
    builder.Services.AddRazorPages();
    builder.Services.AddHealthChecks();

    // Repositories
    builder.Services.AddSingleton<IDatabaseService>(InitializeCosmosClientInstanceAsync(builder.Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
    builder.Services.AddSingleton<AppVersionInfo>();
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    builder.Services.AddMemoryCache();
    builder.Services.AddApplicationInsightsTelemetry();
    builder.Services.AddCacheStack((provider, builder) => builder
        .AddMemoryCacheLayer()
        .WithCleanupFrequency(TimeSpan.FromMinutes(15))
    );

    builder.Services.AddMvc().AddXmlDataContractSerializerFormatters();

    var app = builder.Build();

    if (app.Environment.EnvironmentName == "Development")
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    var options = new RewriteOptions()
            .AddRewrite(@"^content/rss/drudge\.xml", "api/rss/drudge-report",
                skipRemainingRules: true);
    app.UseRewriter(options);
    app.UseHttpsRedirection();
    app.UseFileServer(enableDirectoryBrowsing: false);

    app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest);

    app.UseRouting();
#if !DEBUG
    app.UseAuthentication();
    app.UseAuthorization();
    app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Content-Security-Policy", "script-src 'self'");
    context.Response.Headers.Append("Referrer-Policy", "same-origin");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=2592000");

    await next();
});
#endif
    app.MapHealthChecks("/health");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();
    Log.Information("Here");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shutdown complete");
    Log.CloseAndFlush();
}

/// <summary>
/// Creates a Cosmos DB database and a container with the specified partition key. 
/// </summary>
/// <returns></returns>
static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
{
    string databaseName = configurationSection.GetSection("DatabaseName").Value;
    string containerName = configurationSection.GetSection("ContainerName").Value;
    string account = configurationSection.GetSection("Account").Value;
    string key = configurationSection.GetSection("Key").Value;
    CosmosClient client = new CosmosClient(account, key);
    CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
    DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
    await database.Database.CreateContainerIfNotExistsAsync(containerName, "/HostName");

    return cosmosDbService;
}

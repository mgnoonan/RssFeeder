using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Raven.Client.Documents;
using RssFeeder.Mvc.Models;
using RssFeeder.Mvc.Services;
using Serilog;

namespace RssFeeder.Mvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMicrosoftIdentityWebAppAuthentication(Configuration);
            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<SerilogMvcLoggingAttribute>();
            }).AddMicrosoftIdentityUI();
            services.AddRazorPages();
            services.AddHealthChecks();

            // Repositories
#if DEBUG
            // Setup RavenDb
            // docker run --rm -d -p 8080:8080 -p 38888:38888 ravendb/ravendb:latest
            IDocumentStore store = new DocumentStore
            {
                Urls = new[] { "http://127.0.0.1:8080/" }
                // Default database is not set
            }.Initialize();
            services.AddSingleton<IDatabaseService>(new RavenDbService(store));
#else
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
#endif
            services.AddSingleton<AppVersionInfo>();
            services.AddMediatR(typeof(Startup));
            services.AddMemoryCache();
            services.AddApplicationInsightsTelemetry();

            services.AddMvc().AddXmlDataContractSerializerFormatters();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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
                    //.AddRedirect(@"^.env", "/")
                    //.Add(RewriteRules.RedirectWordpressRequests)
                    //.Add(RewriteRules.RedirectPhpFileRequests)
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
#endif
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        /// <summary>
        /// Creates a Cosmos DB database and a container with the specified partition key. 
        /// </summary>
        /// <returns></returns>
        private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
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
    }
}

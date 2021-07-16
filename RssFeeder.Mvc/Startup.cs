using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using RssFeeder.Mvc.Models;
using Serilog;
using Microsoft.Azure.Cosmos;
using RssFeeder.Mvc.Services;
using System.Threading.Tasks;
using MediatR;

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

            // Repositories
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
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
                    .AddRedirect(@"^.env", "/")
                    .AddRedirect(@"^wp-login\.php", "/")
                    .AddRedirect(@"^xmlrpc\.php", "/")
                    .AddRedirect(@"^wp-content(.*)", "/")
                    .AddRedirect(@"^wp-admin(.*)", "/")
                    .AddRedirect(@"(.*)/wp-admin(.*)", "/")
                    .AddRedirect(@"^wp-includes(.*)", "/")
                    .AddRedirect(@"(.*)/wlwmanifest\.xml", "/")
                    .AddRewrite(@"^content/rss/drudge\.xml", "api/rss/drudge-report",
                        skipRemainingRules: true);
            app.UseRewriter(options);
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest);

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
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

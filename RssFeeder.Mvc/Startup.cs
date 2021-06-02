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
            services.AddHealthChecks()
                .AddCosmosDb(string.Format("AccountEndpoint={0};AccountKey={1};", Configuration["endpoint"], Configuration["authKey"]));

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
            services.AddScoped<IRepository<RssFeederRepository>, RssFeederRepository>();
            services.AddSingleton<AppVersionInfo>();
            services.AddMemoryCache();
            services.AddApplicationInsightsTelemetry();

            services.AddMvc().AddXmlDataContractSerializerFormatters();

            RepositoryInitializer.Initialize(Configuration);
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
                    .AddRedirect(@"(.*)/.env", "/")
                    .AddRedirect(@"(.*)/wp-login\.php", "/")
                    .AddRedirect(@"(.*)/xmlrpc\.php", "/")
                    .AddRedirect(@"(.*)/wp-content(.*)", "/")
                    .AddRedirect(@"(.*)/wp-admin(.*)", "/")
                    .AddRedirect(@"(.*)/wp-includes(.*)", "/")
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
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using GymPass.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.S3;
using Amazon.Rekognition;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Http;

namespace GymPass
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
            // Configure AWS services
            var options = Configuration.GetAWSOptions("AWS"); // gets AWSService key value pair from secrets.json that holds username and region
            IAmazonS3 client = options.CreateServiceClient<IAmazonS3>();
            services.AddAWSService<IAmazonS3>();
            services.AddAWSService<IAmazonRekognition>();

            // add controller service
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddDbContext<FacilityContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("FacilityContext")));

            //services.AddHttpsRedirection(options =>
            //{
            //    options.HttpsPort = 443;
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                name: "default",
                 pattern: "{controller=Home}/{action=Index}/{id?}");
                // pattern: "{controller}/{action}/{id?}");

                endpoints.MapRazorPages();
            });

        }
    }
}

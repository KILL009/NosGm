using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NosGm.Configuration;
using NosTale.Module.Bazaar;
using NosTale.Module.Bazaar.Controllers;
using NosTale.Module.Bazaar.Extensions;
using System;

namespace NosTale.Modules
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
            string text = "NosTale Module";

            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);

            services.AddControllers()
                .AddApplicationPart(typeof(BazaarController).Assembly)
                .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()));

            var modulesConfiguration = new ModulesConfiguration();
            Configuration.Bind(modulesConfiguration);
            services.AddOptions<ModulesConfiguration>().Bind(Configuration).ValidateDataAnnotations();
            services.Configure<KestrelServerOptions>(options => options.ListenAnyIP(modulesConfiguration.Port));

            services.AddSingleton<BazaarManager>();
            services.AddBazaarCore();

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}

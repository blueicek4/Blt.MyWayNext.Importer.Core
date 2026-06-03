using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;    
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Blt.MyWayNext.WebHook
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(opt =>
            {
                // 👇  NOME UNIVOCO SENZA SPAZI: "v1"
                const string swaggerGroup = "v1";

                opt.SwaggerDoc(swaggerGroup, new OpenApiInfo
                {
                    Title = "Attività Commerciali GPT Webhook",
                    Version = "1.3.0",
                    Description = "Servizio per consultare, analizzare e aggiornare attività commerciali tramite GPT."
                });

                opt.EnableAnnotations();

                var xmlPath = Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                if (File.Exists(xmlPath)) opt.IncludeXmlComments(xmlPath);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();
            app.UseMiddleware<RequestBodyLoggingMiddleware>();

            // --- Swagger consentito solo da localhost ---
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger") ||
                    context.Request.Path.StartsWithSegments("/docs"))
                {
                    var remoteIp = context.Connection.RemoteIpAddress;
                    if (!IPAddress.IsLoopback(remoteIp!))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Swagger disponibile solo da 127.0.0.1");
                        return;
                    }
                }
                await next();
            });

            // --- Middleware Swagger ---
            const string swaggerGroup = "v1"; // deve combaciare col nome sopra
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{swaggerGroup}/swagger.json", $"API {swaggerGroup}");
                c.RoutePrefix = "docs"; // UI su /docs

                // Nessuna espansione iniziale → la sezione Schemas resta chiusa
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/debug/routes", async context =>
                {
                    var routes = endpoints.DataSources.SelectMany(ds => ds.Endpoints);
                    foreach (var route in routes) await context.Response.WriteAsync(route.DisplayName + "\n");
                });
            });
        }
    }
}
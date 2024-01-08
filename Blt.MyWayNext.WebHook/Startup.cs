using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Blt.MyWayNext.WebHook
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Aggiunge solo il supporto per i controller (non MVC completo, poiché non servono viste)
            services.AddControllers();            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Pagina di errore per lo sviluppo
            }
            else
            {
                app.UseExceptionHandler("/Home/Error"); // Gestore delle eccezioni per la produzione
                app.UseHsts(); // HTTP Strict Transport Security
            }
            
            // Se le richieste HTTP sono ammesse insieme a HTTPS
            //app.UseHttpsRedirection(); // Reindirizza automaticamente da HTTP a HTTPS

            app.UseRouting(); // Abilita il routing

            // Poiché non è richiesta l'autenticazione, i middleware per autenticazione e autorizzazione vengono rimossi

            app.UseEndpoints(endpoints =>
            {
                // Configurazione delle route per i controller
                endpoints.MapControllers(); // Mappa solo i controller, non le viste

                endpoints.MapGet("/debug/routes", async context =>
                {
                    var routes = endpoints.DataSources.SelectMany(ds => ds.Endpoints);
                    foreach (var route in routes)
                    {
                        await context.Response.WriteAsync(route.DisplayName + "\n");
                    }
                });
            });
        }
    }
}
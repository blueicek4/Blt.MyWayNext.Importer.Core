using Blt.MyWayNext.WebHook;
using System;
using System.Threading;
using System.Timers;
using Blt.MyWayNext.WebHook.Background;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Blt.MyWayNext.WebHook
{
    class Program
    {
        static System.Timers.Timer timer;
        static int counter = 0;
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        static void Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();
            SetTimer();
            
            app.Run();
        }


        static void SetTimer()
        {

            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration config = builder.Build();
            timer = new System.Timers.Timer(Convert.ToInt32(config["AppSettings:TimerControlli"]) * 1000 * 60);

            // Collega l'evento Elapsed al tuo metodo
            timer.Elapsed += OnTimedEvent;

            // Imposta il timer per avviare l'evento all'intervallo specificato
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            counter++;

            // Esegui una serie di funzioni o controlli
            PerformRegularTasks();

            // Esegui funzioni condizionali
            if (counter % 2 == 0)
            {
                PerformConditionalTasks1();
            }
            if (counter % 3 == 0)
            {
                PerformConditionalTasks2(); 
            }
        }

        static void PerformRegularTasks()
        {
            var resp = Blt.MyWayNext.WebHook.Background.Worker.SendAppointmentConfirmationChat().GetAwaiter().GetResult();
        }

        static void PerformConditionalTasks1()
        {
            // Qui inserisci le funzioni che vuoi eseguire ogni 2 cicli
        }

        static void PerformConditionalTasks2()
        {
            // Qui inserisci le funzioni che vuoi eseguire ogni 3 cicli
        }
    }

}
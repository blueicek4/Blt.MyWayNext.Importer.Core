using Blt.MyWayNext.WebHook;
using System;
using System.Threading;
using System.Timers;
using Blt.MyWayNext.WebHook.Background;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
            string address = System.Configuration.ConfigurationManager.AppSettings["baseAddress"];
            Int32 port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["basePort"]);
            string ssl = "http";
            if (System.Configuration.ConfigurationManager.AppSettings["baseSsl"] == "true")
            {
                ssl = "https";
            }
            UriBuilder uriBuilder = new UriBuilder(ssl, address, port);
            string baseAddress = uriBuilder.ToString();


            CreateHostBuilder(args).Build().Run();
            Console.WriteLine($"Server running at {baseAddress}");
            SetTimer();
            // Keep the server running
            Console.ReadLine();
            // Start OWIN host 
        }


        static void SetTimer()
        {
            timer = new System.Timers.Timer(Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["TimerControlli"]) * 1000 * 60);

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
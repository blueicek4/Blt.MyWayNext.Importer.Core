using Blt.MyWayNext.WebHook;
using System;
using System.Threading;
using System.Timers;
using Blt.MyWayNext.WebHook.Background;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.ServiceProcess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Reflection;
using log4net.Config;
using log4net;
using System.Text;


namespace Blt.MyWayNext.WebHook
{
    class Program
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static System.Timers.Timer timer;
        static int counter = 0;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices(services =>
            {
                services.AddHostedService<WindowsBackgroundService>();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(exePath);
            IConfigurationBuilder cbuilder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration _configuration = cbuilder.Build();
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            
            log.Info("Start Application");

            try
            {
                if (_configuration["AppSettings:debug"].ToLower() == "true")
                    System.Diagnostics.Debugger.Launch();

                var isService = !(Debugger.IsAttached || args.Contains("--console"));

                if (isService)
                {
                    var builder = CreateHostBuilder(args).Build();
                    SetTimer();
                    log.Info("Webhook Server Avviato");
                    builder.Run();
                }
                else
                {
                    log.Info("Running as a console application");
                    CreateHostBuilder(args).Build().Run();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] - {ex.Message}\n");
                System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] - {ex.StackTrace}\n");
            }


        }


        static void SetTimer()
        {

            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration config = builder.Build();
            if (Convert.ToBoolean(config["AppSettings:OperazioniAutomatiche"]))
            {

                timer = new System.Timers.Timer(Convert.ToInt32(config["AppSettings:TimerControlli"]) * 1000 * 60);

                // Collega l'evento Elapsed al tuo metodo
                timer.Elapsed += OnTimedEvent;

                // Imposta il timer per avviare l'evento all'intervallo specificato
                timer.AutoReset = true;
                timer.Enabled = true;
            }
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
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration config = builder.Build();
            if (Convert.ToBoolean(config["AppSettings:OperazioniAutomatiche"]))
            {
                var resp = Blt.MyWayNext.WebHook.Background.Worker.SendAppointmentConfirmationChat().GetAwaiter().GetResult();
            }
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

    internal class WindowsBackgroundService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Implement your service logic here
            while (!stoppingToken.IsCancellationRequested)
            {
                // Service work
                await Task.Delay(1000, stoppingToken);
            }
        }


    }

    internal class CustomServiceBase : ServiceBase
    {
        private IHost _host;

        public CustomServiceBase(IHost host)
        {
            _host = host;
        }

        protected override void OnStart(string[] args)
        {
            _host.Start();
        }

        protected override void OnStop()
        {
            _host.Dispose();
        }
    }
}
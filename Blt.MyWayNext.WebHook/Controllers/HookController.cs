using Blt.MyWayNext.WebHook.Background;
using Blt.MyWayNext.Bol;
using Blt.MyWayNext.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;



namespace Webhook.Controllers
{
    [ApiController]
    [Route("api")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration _configuration;

        public WebhookController(ILogger<WebhookController> logger, IConfiguration configuration)
        {
            _logger = logger;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
        }

        [HttpPost]
        [HttpGet]
        [Route("webhook/{tipologia}/{guid}")]
        public async Task<IActionResult> ReceiveWebhook(string tipologia, string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid}");

            NameValueCollection formData;

            try
            {
                formData = await ExtractFormDataAsync();

                // Verifica del GUID
                if (IsValidGuid(guid))
                {
                    System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}");
                    Console.Write($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}\r\n");
                    // Gestisci il payload del webhook qui
                    // ...
                    var mappings = Mapping.LoadFromXml(_configuration["AppSettings:mapping"]);
                    //verifico se tra i mapping configurati c'è n'è uno con il nome uguale alla guid ed il tipo uguale alla tipologia e se esiste restituisco un valore ok, altrimenti restituisco un errore di webhook non valido
                    if (mappings.Any(m => m.name == guid && m.type == tipologia))
                    {
                        WebhookTypeEnum webhookType = (WebhookTypeEnum)Enum.Parse(typeof(WebhookTypeEnum), tipologia);
                        MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                        MyWayApiResponse result = new Blt.MyWayNext.Bol.MyWayApiResponse();
                        switch (webhookType)
                        {
                            case WebhookTypeEnum.AnagraficaTemporanea:
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporanea(formData, guid)).GetAwaiter().GetResult();

                                break;
                            case WebhookTypeEnum.AnagraficaTemporaneaIniziativa:
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporaneaIniziativa(formData, guid)).GetAwaiter().GetResult();

                                break;
                            case WebhookTypeEnum.AttivitaCommerciale:
                                result = Task.Run(async () => await myWayNext.ImportAttivitaCommerciale(formData, guid)).GetAwaiter().GetResult();

                                break;
                            case WebhookTypeEnum.AggiornaAttivitaCommerciale:
                                result = Task.Run(async () => await myWayNext.ImportAggiornaAttivitaCommerciale(formData, guid)).GetAwaiter().GetResult();

                                break;
                            default:
                                break;
                        }
                        if (result.Success)
                        {
                            return Ok(result.ErrorMessage);
                        }
                        else
                        {
                            // Operazione fallita
                            string errorMessage = result.ErrorMessage;
                            return BadRequest(errorMessage);
                        }
                    }
                    else
                    {
                        return Unauthorized("Webhook non valido!");
                    }
                }
                else
                {
                    return Unauthorized("Accesso non autorizzato.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook");
                return StatusCode(500, "Si è verificato un errore interno");
            }
                        
        }

        [HttpPost]
        [HttpGet]
        [Route("meta/{tipologia}/{guid}")]
        public async Task<IActionResult> ReceiveMeta(string tipologia, string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia}");

            NameValueCollection formData;

            try
            {
                var res1 = Request.Body.ToString();

                string jsonContent = await new StreamReader(Request.Body).ReadToEndAsync();
                MetaWebhookEvent meta = Newtonsoft.Json.JsonConvert.DeserializeObject<MetaWebhookEvent>(jsonContent);

                if (IsValidGuid(guid))
                {
                    formData = await ExtractFormDataAsync();
                    System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}");
                    Console.Write($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}\r\n");
                    // Gestisci il payload del webhook qui
                    // ...

                    var mappings = Mapping.LoadFromXml(_configuration["AppSettings:mapping"]);
                    //verifico se tra i mapping configurati c'è n'è uno con il nome uguale alla guid ed il tipo uguale alla tipologia e se esiste restituisco un valore ok, altrimenti restituisco un errore di webhook non valido
                    if (mappings.Any(m => m.name == guid && m.type == tipologia))
                    {
                        MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                        MyWayApiResponse result = new Blt.MyWayNext.Bol.MyWayApiResponse();

                        switch (tipologia)
                        {

                            case "create":
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporaneaIniziativa(formData, guid)).GetAwaiter().GetResult();
                                break;
                            default:
                                return Unauthorized("Webhook non valido!");
                                break;
                        }
                        if (result.Success)
                        {
                            return Ok(result.ErrorMessage);
                        }
                        else
                        {
                            // Operazione fallita
                            string errorMessage = result.ErrorMessage;
                            return BadRequest(errorMessage);
                        }
                    }
                    else
                    {
                        return Unauthorized("Webhook non valido!");
                    }
                }
                else
                {
                    return Unauthorized("Accesso non autorizzato.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook");
                return StatusCode(500, "Si è verificato un errore interno");
            }

        }

        private bool IsValidGuid(string guid)
        {
            // Implementa la tua logica di verifica del GUID qui
            // Per esempio, controllare se il GUID corrisponde a un valore noto
            return !string.IsNullOrEmpty(guid);
        }

        private async Task<NameValueCollection> ExtractFormDataAsync()
        {
            NameValueCollection formData = new NameValueCollection();

            if (Request.ContentType.Contains("application/x-www-form-urlencoded"))
            {
                var formCollection = await Request.ReadFormAsync();
                foreach (var key in formCollection.Keys)
                {
                    formData.Add(key, formCollection[key]);
                }
            }
            else if (Request.ContentType.Contains("application/json"))
            {
                var jsonContent = await new StreamReader(Request.Body).ReadToEndAsync();
                JObject json = JObject.Parse(jsonContent);
                formData = ConvertJsonToFormData(json);
            }
            else
            {
                throw new InvalidOperationException("Tipo di contenuto non supportato");
            }

            return formData;
        }

        private NameValueCollection ConvertJsonToFormData(JObject json)
        {
            var formData = new NameValueCollection();

            foreach (var pair in json)
            {
                formData.Add(pair.Key, pair.Value.ToString());
            }

            return formData;
        }
    }
}
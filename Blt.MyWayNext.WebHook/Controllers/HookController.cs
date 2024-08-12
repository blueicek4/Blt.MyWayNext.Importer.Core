using Blt.MyWayNext.WebHook.Background;
using Blt.MyWayNext.Bol;
using Blt.MyWayNext.Api;
using Blt.MyWayNext.Tool;
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
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using log4net;
using log4net.Config;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]



namespace Webhook.Controllers
{
    [ApiController]
    [Route("api")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IConfiguration _configuration;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public WebhookController(ILogger<WebhookController> logger, IConfiguration configuration)
        {
            _logger = logger;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            log.Info("Inizializzato Controller Webhook");
        }

        [HttpPost]
        [HttpGet]
        [Route("Webhook/{tipologia}/{guid}")]
        public async Task<IActionResult> ReceiveWebhook(string tipologia, string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            //_logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid}");
            log.Info($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid}");
            
            NameValueCollection formData;
            Request.EnableBuffering();

            try
            {
                formData = await ExtractFormDataAsync();

                log.Info($"Verifica del GUID per il Webhook {guid}");
                // Verifica del GUID
                if (IsValidGuid(guid))
                {
                    log.Info($"Trovato Guid Valido.");
                    log.Debug($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}");
                    log.Info($"Verifico Tipo {tipologia}");
                    //System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}");
                    //Console.Write($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}\r\n");
                    // Gestisci il payload del webhook qui
                    // ...
                    var mappings = Mapping.LoadFromXml(_configuration["AppSettings:mapping"]);
                    //verifico se tra i mapping configurati c'è n'è uno con il nome uguale alla guid ed il tipo uguale alla tipologia e se esiste restituisco un valore ok, altrimenti restituisco un errore di webhook non valido
                    if (mappings.Any(m => m.name == guid && m.type == tipologia))
                    {
                        log.Info($"Webhook Valido. trovato mapping per {tipologia}.");
                        WebhookTypeEnum webhookType = (WebhookTypeEnum)Enum.Parse(typeof(WebhookTypeEnum), tipologia);
                        MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                        MyWayApiResponse result = new Blt.MyWayNext.Bol.MyWayApiResponse();
                        switch (webhookType)
                        {
                            case WebhookTypeEnum.AnagraficaTemporanea:
                                log.Info($"Eseguo ImportAnagraficaTemporanea");
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporanea(formData, guid)).GetAwaiter().GetResult();
                                log.Info($"Risultato ImportAnagraficaTemporanea: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AnagraficaTemporaneaIniziativa:
                                log.Info($"Eseguo ImportAnagraficaTemporaneaIniziativa");
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporaneaIniziativa(formData, guid)).GetAwaiter().GetResult();
                                if(result.Success)
                                    log.Info($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                else
                                    log.Error($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AttivitaCommerciale:
                                log.Info($"Eseguo ImportAttivitaCommerciale");
                                result = Task.Run(async () => await myWayNext.ImportAttivitaCommerciale(formData, guid)).GetAwaiter().GetResult();
                                if (result.Success)
                                    log.Info($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                else
                                    log.Error($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AggiornaAttivitaCommerciale:
                                log.Info($"Eseguo ImportAggiornaAttivitaCommerciale");
                                result = Task.Run(async () => await myWayNext.ImportAggiornaAttivitaCommerciale(formData, guid)).GetAwaiter().GetResult();
                                if (result.Success)
                                    log.Info($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                else
                                    log.Error($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            default:
                                log.Warn($"Tipo Webhook non ancora gestito.");
                                break;
                        }
                        if (result.Success)
                        {
                            log.Info($"Operazione completata con successo per webhook con guid {guid} e tipo {tipologia}.");
                            return Ok(result.ErrorMessage);
                        }
                        else
                        {
                            // Operazione fallita
                            log.Error($"Operazione fallita per webhook con guid {guid} e tipo {tipologia}.");
                            string errorMessage = result.ErrorMessage;
                            return BadRequest(errorMessage);
                        }
                    }
                    else
                    {
                        log.Warn($"Webhook non valido con guid {guid} e tipo {tipologia}.");
                        return Unauthorized("Webhook non valido!");
                    }
                }
                else
                {   log.Error($"Accesso non autorizzato con guid {guid} e tipo {tipologia}");
                    return Unauthorized("Accesso non autorizzato.");
                }

            }
            catch (Exception ex)
            {
                log.Error($"Errore nell'elaborazione del webhook: {ex.Message}");
                //_logger.LogError(ex, "Errore nell'elaborazione del webhook");
                return StatusCode(500, "Si è verificato un errore interno");
            }

        }

        [HttpPost]
        [HttpGet]
        [Route("Data/{tipologia}")]
        public async Task<IActionResult> ReceiveData(string tipologia)
        {
            var logPath = _configuration["AppSettings:logPath"];
            _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia}");
            Request.EnableBuffering();

            NameValueCollection formData;
            Request.EnableBuffering();

            try
            {
                Request.EnableBuffering();
                string json = Task.Run(async  () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult();
                System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: {tipologia} - TipoContent: {Request.ContentType} - Content: {json}");
                Console.Write($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - Content {json}\r\n");

                MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                MyWayApiResponse result = null;
                switch (tipologia)
                {
                    case "GetAnagrafiche":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.GetAnagrafiche(formData.GetValues("anagrafica")[0].ToString().ToLower())).GetAwaiter().GetResult();
                        break;
                    case "GetIniziative":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.GetIniziative(formData.GetValues("anagrafica")[0].ToString().ToLower(), formData.GetValues("isTemporanea")[0].ToString().ToLower())).GetAwaiter().GetResult();
                        break;
                    case "GetTrattativa":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.GetTrattativa(formData.GetValues("iniziativa")[0].ToString().ToLower())).GetAwaiter().GetResult();
                        break;
                    case "GetTrattative":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.GetTrattative(formData.GetValues("anagrafica")[0].ToString().ToLower())).GetAwaiter().GetResult();
                        break;
                    case "GetStatiTrattativa":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.GetStatiTrattativa()).GetAwaiter().GetResult();
                        break;
                    case "PutTrattativa":
                        MyWayObjTrattativa trattPut = JsonConvert.DeserializeObject<MyWayObjTrattativa>(json);
                        result = Task.Run(async () => await myWayNext.PutTrattativa(trattPut)).GetAwaiter().GetResult();
                        break;
                    case "SetTrattativa":
                        MyWayObjTrattativa TrattSet = JsonConvert.DeserializeObject<MyWayObjTrattativa>(json);
                        result = Task.Run(async () => await myWayNext.SetTrattativa(TrattSet)).GetAwaiter().GetResult();
                        break;
                    case "Convert":
                        formData = await ExtractFormDataAsync();
                        result = Task.Run(async () => await myWayNext.SetConvertAnagrafica(Convert.ToInt32(formData.GetValues("idAnagraficaTmp")[0]), formData.GetValues("partitaIva")[0].ToString().ToLower())).GetAwaiter().GetResult();
                        break;
                    default:
                        break;
                }
                if ((result.Success))
                {
                    return Ok(result);
                }
                else
                {
                    // Operazione fallita
                    string errorMessage = result.ErrorMessage;
                    return BadRequest(errorMessage);
                }


                // Verifica del GUID
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook");
                return StatusCode(500, "Si è verificato un errore interno");
            }

        }

        [HttpPost]
        [HttpGet]
        [Route("Meta/{tipologia}/{guid}")]
        public async Task<IActionResult> ReceiveMeta(string tipologia, string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - guid {guid}");
            string json = await new StreamReader(Request.Body).ReadToEndAsync();
            MetaWebhookEvent webhookEvent = JsonConvert.DeserializeObject<MetaWebhookEvent>(json);
            NameValueCollection formData = Helper.ConvertToNameValueCollection(webhookEvent);
            Request.EnableBuffering();


            // Verifica del GUID
            if (IsValidGuid(guid))
            {
                System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}");
                Console.Write($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - {String.Join("\n", formData.AllKeys.SelectMany(key => formData.GetValues(key).Select(value => key + ": " + value)).ToList())}\r\n");
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
                        case "update":
                            result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporaneaIniziativa(formData, guid)).GetAwaiter().GetResult();

                            break;
                        case "delete":
                            result = Task.Run(async () => await myWayNext.ImportAttivitaCommerciale(formData, guid)).GetAwaiter().GetResult();

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

        [HttpPost]
        [HttpGet]
        [Route("Companeo/{guid}")]
        public async Task<IActionResult> ReceiveCompaneo(string guid)
        {
            //var logPath = _configuration["AppSettings:logPath"];
            //_logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: Companeo - {guid}");
            log.Info($"[{DateTime.Now}] Webhook ricevuto: Companeo - {guid}");
            NameValueCollection formData;

            try
            {
                log.Info($"Verifica del GUID per il Webhook {guid}");
                if (IsValidGuid(guid))
                {
                    Request.EnableBuffering();
                    string json = Task.Run(async () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult();
                    log.Info($"[{DateTime.Now}] Webhook ricevuto: Companeo - TipoContent: {Request.ContentType} - Content: {json}");
                    //System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: Companeo - TipoContent: {Request.ContentType} - Content: {json}");
                    //Console.Write($"[{DateTime.Now}] Webhook ricevuto: Companeo - Content {json}\r\n");

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = new Blt.MyWayNext.Bol.MyWayApiResponse();

                    formData = await ExtractFormDataAsync();
                    log.Info($"Eseguo ImportCompaneo");
                    result = Task.Run(async () => await myWayNext.ImportCompaneo(guid, formData)).GetAwaiter().GetResult();
                    
                    log.Info($"Risultato ImportCompaneo: {result.Success} - Messaggio {result.ErrorMessage}");
                    if ((result.Success))
                    {
                        log.Info($"Operazione completata con successo per webhook con guid {guid} e tipo Companeo.");
                        return Ok(result);
                    }
                    else
                    {
                        log.Error($"Operazione fallita per webhook con guid {guid} e tipo Companeo.");
                        // Operazione fallita
                        string errorMessage = result.ErrorMessage;
                        return BadRequest(errorMessage);
                    }
                }
                else
                {
                    log.Error($"Accesso non autorizzato con guid {guid} e tipo Companeo.");
                    return Unauthorized("Accesso non autorizzato.");
                }

                // Verifica del GUID
            }
            catch (Exception ex)
            {
                log.Error($"Errore nell'elaborazione del webhook: {ex.Message}");
                //_logger.LogError(ex, "Errore nell'elaborazione del webhook");
                return StatusCode(500, "Si è verificato un errore interno");
            }

        }

        [HttpPost]
        [HttpGet]
        [Route("Helpdesk/{guid}")]
        public async Task<IActionResult> ReceiveHelpdesk(string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

            NameValueCollection formData;

            try
            {
                if (IsValidGuid(guid))
                {
                    Request.EnableBuffering();
                    string json = Task.Run(async () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult();
                    System.IO.File.AppendAllText(_configuration["AppSettings:logPath"], $"[{DateTime.Now}] Webhook ricevuto: HelpDesk - TipoContent: {Request.ContentType} - Content: {json}");
                    Console.Write($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - Content {json}\r\n");

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = new Blt.MyWayNext.Bol.MyWayApiResponse();

                    formData = await ExtractFormDataAsync();
                    result = Task.Run(async () => await myWayNext.ImportCompaneo(guid, formData)).GetAwaiter().GetResult();

                    if ((result.Success))
                    {
                        return Ok(result);
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
                    return Unauthorized("Accesso non autorizzato.");
                }

                // Verifica del GUID
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
            Request.Body.Position = 0;
            NameValueCollection formData = new NameValueCollection();
            try
            {

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
                    var jsonContent = Task.Run(async () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult(); //await new StreamReader(Request.Body).ReadToEndAsync();
                    JObject json = JObject.Parse(jsonContent);
                    formData = ConvertJsonToFormData(json);
                }
                else
                {
                    try
                    {
                        var jsonContent = Task.Run(async () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult(); //await new StreamReader(Request.Body).ReadToEndAsync();
                        JObject json = JObject.Parse(jsonContent);
                        formData = ConvertJsonToFormData(json);

                    }
                    catch
                    (Exception ex)
                    {
                        log.Error(ex.Message);
                        throw new InvalidOperationException($"Tipo di contenuto non supportato\n{ex.Message}");
                    }
                }
            }
            catch
            (Exception ex)
            {
                log.Error(ex.Message);
                throw new InvalidOperationException($"Errore in trasformazione dati.\n{ex.Message}");
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
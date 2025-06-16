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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.SqlServer.Server;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Blt.MyWayNext.Importer;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;
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
                                if (result.Success)
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
                { log.Error($"Accesso non autorizzato con guid {guid} e tipo {tipologia}");
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
        [Route("Webhook/json/{tipologia}/{guid}")]
        public async Task<IActionResult> ReceiveJsonWebhook(string tipologia, string guid)
        {
            var logPath = _configuration["AppSettings:logPath"];
            //_logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid}");
            log.Info($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid}");

            Request.EnableBuffering();

            try
            {
                string jsonRaw = await new StreamReader(Request.Body).ReadToEndAsync();
                JObject originalJson = JObject.Parse(jsonRaw);
                JObject jsonData = (JObject)NormalizeJTokenKeys(originalJson);

                log.Info($"Verifica del GUID per il Webhook {guid}");
                // Verifica del GUID
                if (IsValidGuid(guid))
                {
                    log.Info($"Trovato Guid Valido.");
                    log.Debug($"[{DateTime.Now}] Webhook ricevuto: {tipologia} - {guid} - TipoContent: {Request.ContentType}\nContenuto:\n{jsonData.ToString()}");
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
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporanea(jsonData, guid)).GetAwaiter().GetResult();
                                log.Info($"Risultato ImportAnagraficaTemporanea: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AnagraficaTemporaneaIniziativa:
                                log.Info($"Eseguo ImportAnagraficaTemporaneaIniziativa");
                                result = Task.Run(async () => await myWayNext.ImportAnagraficaTemporaneaIniziativa(jsonData, guid)).GetAwaiter().GetResult();
                                if (result.Success)
                                    log.Info($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                else
                                    log.Error($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AttivitaCommerciale:
                                log.Info($"Eseguo ImportAttivitaCommerciale");
                                result = Task.Run(async () => await myWayNext.ImportAttivitaCommerciale(jsonData, guid)).GetAwaiter().GetResult();
                                if (result.Success)
                                    log.Info($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                else
                                    log.Error($"Risultato ImportAnagraficaTemporaneaIniziativa: {result.Success} - Messaggio {result.ErrorMessage}");
                                break;
                            case WebhookTypeEnum.AggiornaAttivitaCommerciale:
                                log.Info($"Eseguo ImportAggiornaAttivitaCommerciale");
                                result = Task.Run(async () => await myWayNext.ImportAggiornaAttivitaCommerciale(jsonData, guid)).GetAwaiter().GetResult();
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
                {
                    log.Error($"Accesso non autorizzato con guid {guid} e tipo {tipologia}");
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
                string json = Task.Run(async () => await new StreamReader(Request.Body).ReadToEndAsync()).GetAwaiter().GetResult();
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

        [HttpPost("crm/attivita/getbyiniziativa/{guid}")]
        [SwaggerOperation(
        OperationId = "ottieniAttivitaDaWebhook",
        Summary = "Recupera attività per una iniziativa",
        Description = "Recupera tutte le attività e le informazioni "
                    + "contestuali legate a una specifica iniziativa commerciale")]
        public async Task<IActionResult> RetrieveAttivitaXIniziativa(string guid, [FromBody] RequestAttivitaByIniziativa requestBody)
        {
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = null;
                    if(!requestBody.codiceIniziativa.Contains("/"))
                        requestBody.codiceIniziativa = requestBody.codiceIniziativa + "/25";
                    requestBody.codiceIniziativa = requestBody.codiceIniziativa.ToUpper();
                    result = Task.Run(async () => await myWayNext.GetAttivitaXIniziativa(requestBody)).GetAwaiter().GetResult();
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }
        
        [HttpGet("crm/attivita/getbyiniziativa/{guid}")]
        [SwaggerOperation(
        OperationId = "ottieniAttivitaDaWebhook",
        Summary = "Recupera attività per una iniziativa",
        Description = "Recupera tutte le attività e le informazioni "
                    + "contestuali legate a una specifica iniziativa commerciale. In modalità GET per poter essere esguito senza conferma da GPT")]
        [ProducesResponseType(typeof(IEnumerable<AttivitaDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RetrieveAttivitaXIniziativa(string guid, [FromQuery] string CodiceIniziativa)
        {
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    var requestBody = new RequestAttivitaByIniziativa
                    {
                        codiceIniziativa = CodiceIniziativa,
                    };

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = null;
                    if (!requestBody.codiceIniziativa.Contains("/"))
                        requestBody.codiceIniziativa = requestBody.codiceIniziativa + "/25";
                    requestBody.codiceIniziativa = requestBody.codiceIniziativa.ToUpper();
                    result = Task.Run(async () => await myWayNext.GetAttivitaXIniziativa(requestBody)).GetAwaiter().GetResult();
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }

        [HttpGet("crm/attivita/convertianagrafica/{guid}")]
        [SwaggerOperation(
        OperationId = "convertiAnagraficaTemporanea",
        Summary = "Converti anagrafica Temporanea in Lead",
        Description = "Inserisci la Partita IVA e la Ragione Sociale "
                    + "Così da poter generare opportunità sul Lead. In modalità GET per poter essere esguito senza conferma da GPT")]
        [ProducesResponseType(typeof(IEnumerable<AttivitaDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConvertiAnagrafica(string guid, [FromQuery] long idAnagrafica, [FromQuery] string PartitaIva, [FromQuery] string RagioneSociale)
        {
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    var updateRagSoc = await myWayNext.SetRagSocAnagrafica(idAnagrafica, RagioneSociale);
                    var result = await myWayNext.SetConvertAnagrafica(idAnagrafica, PartitaIva);
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }

        [HttpPost("crm/attivita/periodo/{guid}")]
        [SwaggerOperation(
        OperationId = "cercaAttivitaXPeriodo",
        Summary = "Cerca attività per periodo",
        Description = "Dato un intervallo temporale e il codice agente, restituisce le attività di competenza.")]
        public async Task<IActionResult> RetrieveAttivitaXPeriodo(string guid, [FromBody] GetRange range)
        {
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    // Estrai le date dinamicamente da JObject


                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = null;                    
                    result = Task.Run(async () => await myWayNext.GetAttivitaXPeriodo(range)).GetAwaiter().GetResult();
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";                       
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }


        [HttpGet("crm/attivita/periodo/{guid}")]
        [SwaggerOperation(
        OperationId = "cercaAttivitaXPeriodo",
        Summary = "Cerca attività per periodo",
        Description = "Dato un intervallo temporale e il codice agente, restituisce le attività di competenza. In modalità GET per poter essere esguito senza conferma da GPT")]

        public async Task<IActionResult> RetrieveAttivitaXPeriodo(string guid
                                                                , [FromQuery] DateTime start
                                                                , [FromQuery] DateTime end
                                                                , [FromQuery] string agente
                                                                , [FromQuery] string stato)
        { 
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    // Ricrea manualmente l’istanza di GetRange (se ti serve passare un oggetto al client interno)
                    var range = new GetRange
                    {
                        Start = start,
                        End = end,
                        Agente = agente,
                        Stato = stato
                    };

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = null;
                    result = await myWayNext.GetAttivitaXPeriodo(range);
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }


        [HttpPost("crm/attivita/update/{guid}")]
        [SwaggerOperation(
        OperationId = "updateAttivita",
        Summary = "Aggiorna un'attività e pianifica la successiva",
        Description = "Permette di aggiornare un'attività svolta e opzionalmente creare la prossima attività da svolgere.")]
        public async Task<IActionResult> UpdateAttivita(string guid, string codiceatt, [FromBody] AggiornaAttivitaCommerciale aggiornamento)
        {
            try
            {
                if (!IsValidGuid(guid))
                {
                    return Unauthorized("Accesso non autorizzato.");
                }
                else
                {
                    var logPath = _configuration["AppSettings:logPath"];
                    _logger.LogInformation($"[{DateTime.Now}] Webhook ricevuto: HelpDesk - {guid}");

                    MWNextApi myWayNext = new Blt.MyWayNext.Api.MWNextApi();
                    MyWayApiResponse result = null;
                    
                    result = await myWayNext.SetAttivitaCommerciale(aggiornamento);
                    if ((result.Success))
                    {
                        result.Code = "STD_OK";
                        return Ok(result);
                    }
                    else
                    {
                        // Operazione fallita
                        result.Code = "STD_ERR";
                        result.Success = false;
                        return BadRequest(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'elaborazione del webhook |" + ex.Message);
                return StatusCode(500, "Si è verificato un errore interno | " + ex.Message);
            }

        }

        private bool IsValidGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            try
            {
                // Carica i mapping dal file XML tramite la tua classe
                var mappings = Mapping.LoadFromXml(_configuration["AppSettings:mapping"]);

                // Controlla se il GUID è presente come attributo "name" (case insensitive)
                return mappings.Any(m => string.Equals(m.name, guid, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                log.Error("Errore durante il caricamento dei mapping da XML", ex);
                return false;
            }
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

        private async Task<JObject> ExtractJsonAsync()
        {
            Request.Body.Position = 0;
            try
            {
                string body = await new StreamReader(Request.Body).ReadToEndAsync();

                // Se il Content-Type è x-www-form-urlencoded, potresti:
                // 1. convertire form in un JSON piatto (tipo { "campo": "valore" })
                // 2. oppure lanciare eccezione se non supporti più i form
                // Per brevità, assumiamo ormai che usi solo JSON.

                var json = JObject.Parse(body);
                return json;
            }
            catch (Exception ex)
            {
                log.Error($"Errore in trasformazione dati.\n{ex.Message}");
                throw new InvalidOperationException($"Errore in trasformazione dati.\n{ex.Message}");
            }
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

        /// <summary>
        /// Rimuove diacritici (accenti) e caratteri speciali, sostituisce spazi con underscore.
        /// </summary>
        private static string NormalizeKey(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 1. Rimuove diacritici usando Normalization in combinazione con il controllo sulle categorie Unicode
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                // Filtra i caratteri "NonSpacingMark" (accenti, cediglie, ecc.)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            normalized = sb.ToString().Normalize(NormalizationForm.FormC);

            // 2. Sostituisce gli spazi con underscore
            normalized = normalized.Replace(" ", "_");

            // 3. Rimuove qualunque carattere non alfanumerico o underscore
            //    (se preferisci conservare i punti o altri simboli, adattalo di conseguenza)
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9_]", "");

            return normalized;
        }


        /// <summary>
        /// Normalizza ricorsivamente i nomi delle proprietà di un JToken.
        /// Restituisce un nuovo JToken (JObject/JArray/JValue) con i nomi "normalizzati".
        /// </summary>
        public static JToken NormalizeJTokenKeys(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var originalObj = (JObject)token;
                    var newObj = new JObject();
                    foreach (var prop in originalObj.Properties())
                    {
                        // Normalizza il nome della proprietà
                        string newName = NormalizeKey(prop.Name);
                        // Ricorsione sul valore
                        newObj[newName] = NormalizeJTokenKeys(prop.Value);
                    }
                    return newObj;

                case JTokenType.Array:
                    var originalArr = (JArray)token;
                    var newArr = new JArray();
                    foreach (var item in originalArr)
                    {
                        // Ricorsione su ogni elemento
                        newArr.Add(NormalizeJTokenKeys(item));
                    }
                    return newArr;

                default:
                    // Per valori primari (stringhe, numeri, bool) o null, restituiamo una copia identica
                    return token.DeepClone();
            }
        }

    }
}
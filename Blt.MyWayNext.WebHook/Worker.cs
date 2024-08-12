using Blt.MyWayNext.Proxy.Authentication;
using Blt.MyWayNext.Proxy.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Blt.MyWayNext.WebHook.Tools;
using Blt.MyWayNext.WebHook.Bol;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.IO;
using Blt.MyWayNext.WebHook.Api;
using log4net.Config;
using log4net;

namespace Blt.MyWayNext.WebHook.Background
{
    public static class Worker
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Invia un messaggio di conferma appuntamento su canale definito in file configurazione
        /// </summary>
        /// <returns></returns>
        public static async Task<ResponseWebhook> SendAppointmentConfirmationChat()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration cfg = builder.Build();

            var auth = await Helper.Autentication();

            if (!auth.Success)
                return new ResponseWebhook() { Success = false, ResponseContent = auth.Message };

            var httpClient = auth.Client;

            var client = new Blt.MyWayNext.Proxy.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);

            var tipiAttivita = await client.ListaGET27Async();
            // filtro i tipi attivitia a solo quelli il cui nome è incluso nell'elenco di valori passato dal cfg["AppSettings:AttivitaPromemoria"] facendo uno string split del carattere ;
            var attivitaPromemoria = tipiAttivita.Data.Where(t => cfg["AppSettings:AttivitaPromemoria"].Split(';').Contains(t.Nome)).ToList();
            string lastSchedule = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (File.Exists("lastSchedule.txt"))
            {
                try
                {
                    lastSchedule = System.IO.File.ReadAllText("lastSchedule.txt");
                    DateTime.Parse(lastSchedule);
                    log.Info($"Ultimo controllo attività schedulate: {lastSchedule}");
                }
                catch (Exception ex)
                {
                    log.Error($"Errore nel leggere il file lastSchedule.txt: {ex.Message}");
                }
            }
            else
            {
                log.Info("File lastSchedule.txt non trovato");
            }
            DateTime start = Convert.ToDateTime(lastSchedule);
            DateTime end = DateTime.Now.AddMinutes(Convert.ToInt32(cfg["AppSettings:MinutiAttivitaSchedulate"]));
            if(start > end)
            {
                log.Error("Errore: la data di inizio è maggiore della data di fine");
                return new ResponseWebhook() { Success = false, ResponseContent = "Errore: la data di inizio è maggiore della data di fine" };
            }
            var condizioniScheduler = new AttivitaSchedulerCondition() { StartDate = start, EndDate = end, Tipi = attivitaPromemoria.Where(t => t.Id.HasValue).Select(a => a.Id.Value).ToList() };
            var attivitaDaFare = await client.RicercaPOST19Async(condizioniScheduler);
           
            ResponseWebhook response = new ResponseWebhook();
            
            foreach (var attivita in attivitaDaFare.Data)
            {
                var attivitaDettaglio = await client.AttivitaGETAsync(attivita.Codice);
                string cellulare = string.Empty;
                string nome = string.Empty;
                string orario = string.Empty;
                string luogo = string.Empty;
                string agente = string.Empty;
                Dictionary<string, string> parametri = new Dictionary<string, string>();
                if (attivitaDettaglio.Data.Referente.Nome != null)
                {
                    cellulare = attivitaDettaglio.Data.Referente.CellulareAzi ?? attivitaDettaglio.Data.Referente.TelefonoAzi;
                    nome = attivitaDettaglio.Data.Referente.Nome;
                }
                else if (attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.Id != 0)
                {
                    cellulare = attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.Cellulare ?? attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.Telefono ?? attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.Telefono2;
                    nome = attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.AliasRagSoc ?? attivitaDettaglio.Data.Iniziativa.AnagraficaTemp.RagSoc;
                }
                else
                {
                    var anagrafica = (await client.IbrideGETAsync(attivitaDettaglio.Data.Iniziativa.Anagrafica.Codice, null)).Data;
                    cellulare = anagrafica.Cellulare ?? anagrafica.Telefono ?? anagrafica.Telefono2;
                    nome = anagrafica.AliasRagSoc ?? anagrafica.RagSoc;

                }

                orario = attivitaDettaglio.Data.DataOraInizio.Value.ToString("HH:mm");
                switch(attivitaDettaglio.Data.Luogo.Nome)
                    {
                    case "In Ufficio":
                        luogo = "presso la nostra sede in Via Olindo Guerrini 20, 00139 Roma";
                        break;
                    case "Smart Working":
                        luogo = "in modalità Smart Working";
                        
                        break;
                    case "Sede Cliente":
                        luogo = "presso la vostra sede";
                        break;
                    case "Online Meeting":
                        luogo = "tramite meeting online";
                        break;
                }
                agente = attivitaDettaglio.Data.Risorse.Where(r=>r.Principale.Value).First().Risorsa.RagSoc;

                parametri.Add("agente", agente);
                parametri.Add("luogo", luogo);
                parametri.Add("nome", nome);
                parametri.Add("orario", orario);
                parametri.Add("cellulare", cellulare);
                HttpClient webClient = new HttpClient();

                cfg["AppSettings:webhookAttivitaSchedulate"] = "https://webhook.site/e27620d6-bfc7-4d80-b921-5a458710b570";

                response = await SendWebhookAsync(webClient, cfg["AppSettings:WebhookAttivitaSchedulate"], parametri.ToList());
                if(!response.Success)
                {
                    log.Error($"Errore nell'invio del messaggio di conferma appuntamento: {response.ResponseContent}");
                }
                else
                {
                    attivitaDettaglio.Data.AttivitaSvolta = "Inviato Whatsapp di Reminder all'interlocutore dell'attività";
                    attivitaDettaglio.Data.AttivitaSvoltaText = "Inviato Whatsapp di Reminder all'interlocutore dell'attività";
                    attivitaDettaglio.Data.Esito.Nome = "TMK REMINDER - Positivo";
                    attivitaDettaglio.Data.Esito.Id = 40;
                    attivitaDettaglio.Data.Stato.Id = 4;
                    attivitaDettaglio.Data.Stato.Nome = "Svolta";
                    var updAttivitaCommerciale = await client.AttivitaPUTAsync(false, false, false, attivitaDettaglio.Data);
                    if(updAttivitaCommerciale.Code == "STD_OK")
                        log.Info($"Messaggio di conferma appuntamento inviato con successo a {cellulare}");
                    else
                        log.Error($"Errore nell'aggiornamento dell'attività {attivitaDettaglio.Data.Codice}: {updAttivitaCommerciale.Message}");

                }


            }
            System.IO.File.WriteAllBytes("lastSchedule.txt", System.Text.Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            return response;

        }

        /// <summary>
        /// Funzione che lancia un Webhook verso l'indirizzo passato come parametro che accetta come parametri una stringa che determina la codifica e poi una lista di coppie chiave valore e restituisce un oggetto di tipo ResponseWebhook
        /// </summary>
        /// <param name="webhook"></param>
        /// <param name="tipo"></param>
        /// <param name="Collection"></param>
        /// <returns>ResponseWebhook</returns>
        static async Task<ResponseWebhook> SendWebhookAsync(HttpClient httpClient, string url, List<KeyValuePair<string, string>> data, string encoding = "application/json")
        {
            try
            {
                HttpContent content;
                string jsonData = string.Empty;
                if (encoding.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    content = new FormUrlEncodedContent(data);
                }
                else if (encoding.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonData = JsonConvert.SerializeObject(data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                    content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                }
                else
                {
                    throw new ArgumentException("Unsupported encoding type", nameof(encoding));
                }
                var contentString = await content.ReadAsStringAsync();
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseRequestString = await response.RequestMessage.Content.ReadAsStringAsync();

                return new ResponseWebhook
                {
                    Success = response.IsSuccessStatusCode,
                    ResponseContent = responseContent,
                    StatusCode = response.StatusCode
                };
            }
            catch (Exception ex)
            {
                // Log l'errore o gestiscilo come preferisci
                return new ResponseWebhook
                {
                    Success = false,
                    ResponseContent = ex.Message,
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }

        public class ResponseWebhook
        {
            public bool Success { get; set; }
            public string ResponseContent { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            // Aggiungi altri campi se necessari
        }

    }
}
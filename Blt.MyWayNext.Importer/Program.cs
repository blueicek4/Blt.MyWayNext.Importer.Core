using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blt.MyWayNext.Business;
using Blt.MyWayNext.Authentication;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace Blt.MyWayNext.Importer
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            var cfg = System.Configuration.ConfigurationManager.AppSettings;

            var httpClient = new HttpClient();
            var autClient = new Blt.MyWayNext.Authentication.Client(cfg["baseAuthUrl"], httpClient);

            LoginUserModel login = new LoginUserModel() { Name = cfg["userName"], Password = cfg["userPassword"] };
            var res = await autClient.LoginAsync(login);

            var token = res.Data.Token;
            var aziendaId = res.Data.Utente.Aziende.First().AziendaId;

            // Imposta l'header di autorizzazione con il token
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resCompany = await autClient.SelectCompanyAsync(aziendaId);
            var bearerToken = EstraiTokenDaJson( resCompany.Data.ToString());
            
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            
            var client = new Blt.MyWayNext.Business.Client(cfg["baseBussUrl"], httpClient);
            /*
            ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null condition = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
            
            var temporanee = await client.RicercaPOST11Async( null, condition);
            var clienteNuovoResponse = await client.NuovoGET5Async();


            var nuovoCliente = clienteNuovoResponse.Data;
            nuovoCliente.AliasRagSoc = "Alias Ragione Sociale";
            nuovoCliente.RagSoc = "Impresa";
            nuovoCliente.Cognome = "Cognome";
            nuovoCliente.Nome = "Nome";
            nuovoCliente.Cellulare = "Cellulare";
                nuovoCliente.Email = "Email";
            nuovoCliente.Tipo.Id = 2;
            nuovoCliente.Indirizzo.Comune = "Non Definito";
            nuovoCliente.Indirizzo.ComuneId = 0;
            nuovoCliente.Indirizzo.Stato.Nome = "Italia";
            nuovoCliente.Indirizzo.Stato.Id = 1;

            //var anagrafica = await client.TemporaneePOSTAsync(anagraficaTemp);
            var clienti = await client.IbridePUTAsync(nuovoCliente);
            RequestIniziativa newIniziativa = new RequestIniziativa();
            newIniziativa.TipoAnagrafica = 2;
            newIniziativa.AnagraficaTempId = clienti.Data.AnagraficaTempId;
            newIniziativa.AgenteCod = "AG0001";
            newIniziativa.Oggetto = "Punto Cassa";
            var iniziativa = await client.NuovoPOST2Async(true, newIniziativa);
            iniziativa.Data.Note = "Note";
            var resp = await client.IniziativaPOSTAsync(iniziativa.Data);
            */
            var tipiAttivita =  await client.ListaGET27Async();
            // filtro i tipi attivitia a solo quelli il cui nome è incluso nell'elenco di valori passato dal cfg["AttivitaPromemoria"] facendo uno string split del carattere ;
            var attivitaPromemoria = tipiAttivita.Data.Where(t => cfg["AttivitaPromemoria"].Split(';').Contains(t.Nome)).ToList();

            var condizioniScheduler = new AttivitaSchedulerCondition() { StartDate = DateTime.Now, EndDate = DateTime.Now.AddHours(Convert.ToInt32(cfg["OreAttivitaSchedulate"])), Tipi = attivitaPromemoria.Where(t=>t.Id.HasValue).Select(a=>a.Id.Value).ToList() };
            var attivitaDaFare = await client.RicercaPOST19Async(condizioniScheduler);
            /*
            foreach(var attivita in attivitaDaFare.Data)
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
                    var anagrafica = (await client.IbrideGETAsync( attivitaDettaglio.Data.Iniziativa.Anagrafica.Codice, null)).Data;
                    cellulare = anagrafica.Cellulare ?? anagrafica.Telefono ?? anagrafica.Telefono2;
                    nome = anagrafica.AliasRagSoc ?? anagrafica.RagSoc;

                }

                orario = attivitaDettaglio.Data.DataOraInizio.Value.ToString("HH:mm");
                luogo = attivitaDettaglio.Data.Luogo.Nome;
                agente = attivitaDettaglio.Data.Risorse.First().Risorsa.RagSoc;

                parametri.Add("agente", agente);
                parametri.Add("luogo", luogo);
                parametri.Add("nome", nome);
                parametri.Add("orario", orario);
                parametri.Add("cellulare", cellulare);
                HttpClient webClient = new HttpClient();

                var resWebhook = await SendWebhookAsync(webClient, cfg["WebhookAttivitaSchedulate"], parametri.ToList());

            }                */


        }


        public static string EstraiTokenDaJson(string json)
        {
            var jObject = JObject.Parse(json);
            string token = jObject["token"].ToString();
            return token;
        }

        /// <summary>
        /// Funzione che lancia un Webhook verso l'indirizzo passato come parametro che accetta come parametri una stringa che determina la codifica e poi una lista di coppie chiave valore e restituisce un oggetto di tipo ResponseWebhook
        /// </summary>
        /// <param name="webhook"></param>
        /// <param name="tipo"></param>
        /// <param name="Collection"></param>
        /// <returns>ResponseWebhook</returns>
        public static async Task<ResponseWebhook> SendWebhookAsync(HttpClient httpClient, string url, List<KeyValuePair<string, string>> data, string encoding = "application/json")
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

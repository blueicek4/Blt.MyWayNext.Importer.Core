using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using Blt.MyWayNext.Bol;
using Blt.MyWayNext.Tool;
using Blt.MyWayNext.Proxy.Authentication;
using Blt.MyWayNext.Proxy.Business;
using log4net;
using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]



namespace Blt.MyWayNext.Business
{
    public static class Business
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static async Task<AuthenticationResponse> CrmLogin()
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                log.Debug("Inizio autenticazione CRM");
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                httpClient = new System.Net.Http.HttpClient();
                var autClient = new Blt.MyWayNext.Proxy.Authentication.Client(cfg["AppSettings:baseAuthUrl"], httpClient);

                LoginUserModel login = new LoginUserModel() { Name = cfg["AppSettings:userName"], Password = cfg["AppSettings:userPassword"] };
                var res = await autClient.LoginAsync(login);

                var token = res.Data.Token;

                Guid aziendaId = Guid.Empty;

                aziendaId = res.Data.Utente.Aziende.FirstOrDefault(a => a.Azienda.Nome == cfg["AppSettings:azienda"]).AziendaId;

                if (aziendaId == Guid.Empty)
                {
                    log.Error("Azienda non trovata");
                    return new AuthenticationResponse() { Success = false, Client = httpClient, Message = "Azienda non trovata" };
                }
                // Imposta l'header di autorizzazione con il token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resCompany = await autClient.SelectCompanyAsync(aziendaId);
                var bearerToken = Helper.EstraiTokenDaJson(resCompany.Data.ToString());
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                
                log.Debug("Ricevuto Bearer da CRM - Autenticazione effettuata con Successo. Inizializzo Client");
                var client = new Blt.MyWayNext.Proxy.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);
                var ricerca = new Blt.MyWayNext.Proxy.Business.RicercaClient(cfg["AppSettings:baseBussUrl"], httpClient);
                return new AuthenticationResponse() { Success = true, Client = httpClient, Message = "Autenticazione effettuata correttamente", Token = bearerToken, crmClient = client, crmRicerca = ricerca };

            }
            catch (Exception ex)
            {
                log.Error($"Errore durante l'autenticazione: {ex.Message}");
                return new AuthenticationResponse() { Success = false, Client = httpClient, Message = ex.Message };

            }
        }

        /// <summary>
        /// Verifica credenziali utente e resistuisce se l'autenticazione è andata a buon fine
        /// </summary>
        /// <param name="username">nome utente</param>
        /// <param name="password">password</param>
        /// <param name="company">azienda</param>
        /// <returns></returns>
        public static async Task<AuthenticationResponse> AuthUSer(string username, string password, string company = null)
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                httpClient = new System.Net.Http.HttpClient();
                var autClient = new Blt.MyWayNext.Proxy.Authentication.Client(cfg["AppSettings:baseAuthUrl"], httpClient);

                LoginUserModel login = new LoginUserModel() { Name = username, Password = password };
                var res = await autClient.LoginAsync(login);
                if (res.Code != "STD_OK")
                {
                    return new AuthenticationResponse() { Success = false, Client = httpClient, Message = res.Message };
                }

                var token = res.Data.Token;

                Guid aziendaId = Guid.Empty;

                aziendaId = res.Data.Utente.Aziende.FirstOrDefault(a => a.Azienda.Nome == (company ?? cfg["AppSettings:azienda"])).AziendaId;

                if (aziendaId == Guid.Empty)
                    return new AuthenticationResponse() { Success = false, Client = httpClient, Message = "Azienda non trovata o utente non abilitato" };

                // Imposta l'header di autorizzazione con il token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resCompany = await autClient.SelectCompanyAsync(aziendaId);
                if (resCompany.Code != "STD_OK")
                {
                    return new AuthenticationResponse() { Success = false, Client = httpClient, Message = resCompany.Message };
                }
                var logout = await autClient.LogoutAsync();

                return new AuthenticationResponse() { Success = true, Client = httpClient, Message = "Autenticazione effettuata correttamente" };

            }
            catch (Exception ex)
            {
                return new AuthenticationResponse() { Success = false, Client = httpClient, Message = ex.Message };

            }
        }

        public static async Task<MyWayApiResponse> ImportAnagraficaTemporanea(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration cfg = builder.Build();

            try
            {
                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                log.Debug($"Creazione nuova anagrafica temporanea. Invio Richiesta a NuovoGET5Async");
                var clienteNuovoResponse = await client.NuovoGET5Async();
                var nuovoCliente = clienteNuovoResponse.Data;

                log.Debug("Caricamento mapping da file xml");
                var mappings = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");

                log.Debug("Mappatura campi form con oggetto AnagraficaTemporanea");
                Helper.MapFormToObject(form, nuovoCliente, mappings);

                log.Debug("Invio richiesta di creazione anagrafica temporanea");
                var resIbride = await client.IbridePUTAsync(nuovoCliente);


                if (resIbride.Code == "STD_OK")
                {
                    log.Info($"Anagrafica Temporanea Creata correttamente.\nRagione Sociale: {nuovoCliente.RagSoc}\nAlias: {nuovoCliente.AliasRagSoc}\nNome: {nuovoCliente.Nome}\nCognome: {nuovoCliente.Cognome}\nEmail: {nuovoCliente.Email}\nTelefono: {nuovoCliente.Telefono}");
                    response.Success = true;
                    response.ErrorMessage = "Anagrafica temporanea importata correttamente";
                }
                else
                {
                    log.Error($"Errore durante la creazione dell'anagrafica temporanea: Ragione Sociale: {nuovoCliente.RagSoc} - Alias: {nuovoCliente.AliasRagSoc} - Nome: {nuovoCliente.Nome} - Cognome: {nuovoCliente.Cognome} | Messaggio: {resIbride.Message}");
                    response.Success = false;
                    response.ErrorMessage = resIbride.Message;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Errore durante la creazione dell'anagrafica temporanea: {ex.Message}");
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;
        }

        public static async Task<MyWayApiResponse> ImportAnagraficaTemporaneaIniziativa(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration cfg = builder.Build();
            try
            {

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };


                var client = authResponse.crmClient;

                //creo anagrafica
                log.Debug($"Creazione nuova anagrafica temporanea. Invio Richiesta a NuovoGET5Async");
                var clienteNuovoResponse = await client.NuovoGET5Async();
                var ObjAnagraficaTemporanea = clienteNuovoResponse.Data;
                log.Debug($"Caricamento mapping da file xml per {name}");
                var mapAnagraficaTemporanea = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");
                if (mapAnagraficaTemporanea.Count > 0)
                {
                    var condAnagrafiche = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                    log.Debug($"Recupero Elenco anagrafiche per cercare se il contatto esiste già");
                    var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagrafiche);
                    bool isAnagraficaTemp = false;
                    long anagraficaId = 0;
                    int tipoAnagrafica = 0;
                    string referenteId = string.Empty;
                    bool newContatto = true;
                    log.Debug($"Verifico se esiste un contatto nel CRM con lo stesso Numero di Telefono");
                    if (ObjAnagraficaList.Data.Any(c => Helper.GetMapValueFromType(form, mapAnagraficaTemporanea, "phone").Any(l => l.ToString() == c.Cellulare) 
                                                        || Helper.GetMapValueFromType(form, mapAnagraficaTemporanea, "email").Any(l=>l.ToString() == c.Email)))
                    {
                        response.Success = false;
                        response.ErrorMessage = "Anagrafica già presente\n";
                        var a = ObjAnagraficaList.Data.FirstOrDefault(c => Helper.GetMapValueFromType(form, mapAnagraficaTemporanea, "phone").Any(l => l.ToString() == c.Cellulare)
                                                        || Helper.GetMapValueFromType(form, mapAnagraficaTemporanea, "email").Any(l => l.ToString() == c.Email));
                        isAnagraficaTemp = a.Temporanea;
                        anagraficaId = a.Id;
                        tipoAnagrafica = a.TipoAnagrafica;
                        log.Warn($"Trovata Anagrafica già presente: ID: {a.Id}\nRagione Sociale Presente: {a.RagSoc} - Alias Presente: {a.AliasRagSoc} ");
                    }
                    else
                    {
                        log.Debug($"Anagrafica non presente, procedo con la creazione");
                        Helper.MapFormToObject(form, ObjAnagraficaTemporanea, mapAnagraficaTemporanea);
                        log.Debug($"Invio richiesta di creazione anagrafica temporanea");
                        var resIbride = await client.IbridePUTAsync(ObjAnagraficaTemporanea);
                        isAnagraficaTemp = resIbride.Data.Temporanea;
                        if (resIbride.Data.AnagraficaTempId != 0)
                        {
                            anagraficaId = resIbride.Data.AnagraficaTempId;
                            tipoAnagrafica = 2;
                        }
                        else
                        {
                            anagraficaId = Convert.ToInt32(resIbride.Data.CodiceId);
                            tipoAnagrafica = 1;
                        }

                        log.Debug($"Anagrafica Temporanea Creata correttamente.\nRagione Sociale: {ObjAnagraficaTemporanea.RagSoc}\nAlias: {ObjAnagraficaTemporanea.AliasRagSoc}\nNome: {ObjAnagraficaTemporanea.Nome}\nCognome: {ObjAnagraficaTemporanea.Cognome}\nEmail: {ObjAnagraficaTemporanea.Email}\nTelefono: {ObjAnagraficaTemporanea.Telefono}");
                        log.Debug($"Creo Contatto per Anagrafica");
                        var objContatto = await client.NuovoGET3Async(String.Empty);
                        var mapContatto = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "Contatto");
                        log.Debug($"Mappatura campi form con oggetto Contatto");
                        Helper.MapFormToObject(form, objContatto.Data, mapContatto);
                        var associazione = new RequestAddReferentWithAss() { Referente = objContatto.Data, Associa = new RequestAssociaReferente() { TypeAssociation = 1, KeyAss = anagraficaId.ToString(), ReferenteCod = objContatto.Data.Codice } };
                        log.Debug($"Invio richiesta di creazione contatto per anagrafica");
                        var respContatto = await client.ReferentiPUTAsync(associazione);
                        if (respContatto.Code == "STD_OK")
                        {
                            log.Debug("Contatto creato correttamente");
                            referenteId = respContatto.Data.Codice;
                        }
                        else
                        {
                            log.Error($"Errore durante la creazione del contatto: {respContatto.Message}");
                            response.Success = false;
                            response.ErrorMessage += respContatto.Message;
                        }
                    }
                    log.Debug($"Carico mappatura iniziativa per {name}");
                    var mapCreaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "CreaIniziativa");
                    if (mapCreaIniziativa.Count == 0)
                    {
                       
                        response.Success = true;
                        response.ErrorMessage += "Mapping CreaIniziativa non presente, impossibile proseguire";
                        return response;
                    }
                    log.Debug($"Verifico se sono già presenti Iniziative Commerciale");
                    var ReqIniziativa = new RequestIniziativa();
                    if (isAnagraficaTemp)
                    {
                        ReqIniziativa.AnagraficaTempId = anagraficaId;
                        ReqIniziativa.TipoAnagrafica = tipoAnagrafica;
                    }
                    else
                    {
                        ReqIniziativa.ClienteCod = anagraficaId.ToString();
                        ReqIniziativa.TipoAnagrafica = tipoAnagrafica;
                    }
                 
                    ReqIniziativa.Oggetto = $"{Helper.GetMapValue(form, mapCreaIniziativa, "Oggetto").ToString()} | {DateTime.Now.ToShortDateString()}";
                    log.Debug($"Invio richiesta:\nAnagrafica: {ReqIniziativa.AnagraficaTempId}\nTipo: {ReqIniziativa.TipoAnagrafica}\nCodice Cliente: {ReqIniziativa.ClienteCod}\nOggetto: {ReqIniziativa.Oggetto}");
                    log.Debug($"Invio richiesta di iniziative commerciali");
                    var ObjInziativaList = await client.AnagraficaPOSTAsync(ReqIniziativa);

                    
                    if (ObjInziativaList.Data.Count > 0)
                    {
                        log.Debug($"Già presente una iniziativa per il contatto, modifico oggetto iniziativa ");
                        newContatto = false;
                    }
                    else
                    {
                                                log.Debug($"Nessuna iniziativa presente, procedo con la creazione");
                    }


                    //creo iniziativa
                    RequestIniziativa ObjCreaIniziativa = new RequestIniziativa();
                    if (tipoAnagrafica > 1)
                    {
                        ObjCreaIniziativa.AnagraficaTempId = anagraficaId;
                    }
                    else
                    {
                        ObjCreaIniziativa.ClienteCod = anagraficaId.ToString();
                    }
                    ObjCreaIniziativa.TipoAnagrafica = tipoAnagrafica;
                    ObjCreaIniziativa.AgenteCod = cfg["AppSettings:agenteCRMLead"];

                    Helper.MapFormToObject(form, ObjCreaIniziativa, mapCreaIniziativa);
                    if (!newContatto)
                        ObjCreaIniziativa.Oggetto = $"{ObjCreaIniziativa.Oggetto} | {DateTime.Now.ToShortDateString()} | {Helper.GetMapValue(form, mapAnagraficaTemporanea, "AliasRagSoc")}";

                    log.Debug($"Invio Iniziativa Commerciale:\nAnagrafica: {ObjCreaIniziativa.AnagraficaTempId}\nTipo: {ObjCreaIniziativa.TipoAnagrafica}\nCodice Cliente: {ObjCreaIniziativa.ClienteCod}\nOggetto: {ObjCreaIniziativa.Oggetto}");

                    var ObjAggiornaIniziativa = await client.NuovoPOST2Async(true, ObjCreaIniziativa);

                    var mapAggiornaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AggiornaIniziativa");
                    if (mapAggiornaIniziativa.Count > 0)
                    {
                        log.Debug($"Mappatura campi form con oggetto AggiornaIniziativa");
                        Helper.MapFormToObject(form, ObjAggiornaIniziativa.Data, mapAggiornaIniziativa);
                        log.Debug($"Invio richiesta di aggiornamento iniziativa commerciale");
                        var resp = await client.IniziativaPOSTAsync(ObjAggiornaIniziativa.Data);

                        if (resp.Code == "STD_OK")
                        {
                            log.Debug($"Iniziativa commerciale creata correttamente\nCodice: {resp.Data.Codice}\nOggetto: {resp.Data.Oggetto}");
                            var mapAttivitaCommerciale = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AttivitaCommerciale");
                            if (mapAttivitaCommerciale.Count > 0)
                            {
                                log.Debug($"Creo attività commerciale");
                                RequestAttivita ReqAttivita = new RequestAttivita();
                                if (ObjAnagraficaTemporanea.Temporanea)
                                {
                                    ReqAttivita.AnagraficaTempId = ObjAnagraficaTemporanea.AnagraficaTempId;
                                    ReqAttivita.TipoAnagrafica = 2;
                                }
                                else
                                {
                                    ReqAttivita.ClienteCod = ObjAnagraficaTemporanea.AnagraficaCodice;
                                    ReqAttivita.TipoAnagrafica = 1;
                                }
                                ReqAttivita.IniziativaCod = resp.Data.Codice;
                                ReqAttivita.AgenteCod = resp.Data.Responsabile.Codice;
                                ReqAttivita.Start = DateTime.Now;
                                ReqAttivita.TipoId = Convert.ToInt32(Helper.GetMapValue(null, mapAttivitaCommerciale, "TipoId").ToString()); // Convert.ToInt32(MapAttivita.FirstOrDefault(m => m.ObjectProperty == "TipoId").DefaultValue);
                                var ObjAttivita = await client.NuovoPOSTAsync(ReqAttivita);
                                Helper.MapFormToObject(form, ObjAttivita.Data, mapAttivitaCommerciale);
                                if (!String.IsNullOrWhiteSpace(referenteId))
                                    ObjAttivita.Data.Referente.Codice = referenteId;
                                var ObjAttivitaSalvata = await client.AttivitaPUTAsync(false, false, false, ObjAttivita.Data);
                                if (ObjAttivitaSalvata.Code == "STD_OK")
                                {
                                    log.Debug($"Attivita commerciale creata correttamente\nCodice: {ObjAttivitaSalvata.Data.Codice}\nOggetto: {ObjAttivitaSalvata.Data.DaFare}");
                                    response.Success = true;
                                    response.ErrorMessage += "Iniziativa commerciale creata correttamente con Attività commerciale annessa\n";
                                }
                                else
                                {
                                    log.Error($"Errore durante la creazione dell'attività commerciale: {ObjAttivitaSalvata.Message}");
                                    response.Success = false;
                                    response.ErrorMessage += ObjAttivitaSalvata.Message;
                                }
                            }
                            else
                            {
                                response.Success = true;
                                response.ErrorMessage += "Iniziativa commerciale creata correttamente su cliente già esistente";
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage += resp.Message;
                        }
                    }
                    else
                    {
                        response.Success = true;
                        response.ErrorMessage += "Mapping AggiornaIniziativa non presente, Non creo iniziativa";
                    }


                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage += "Mapping Anagrafica non presente, impossibile proseguire";
                }
            }
            catch (Exception ex)
            {
                log.Error($"Errore durante la creazione dell'anagrafica temporanea: {ex.Message}");
                response.Success = false;
                response.ErrorMessage += ex.Message;

            }

            return response;
        }

        public static async Task<MyWayApiResponse> ImportAttivitaCommerciale(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };


                var client = authResponse.crmClient;
                var mapAnagrafica = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");
                var mapIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "IniziativaCommerciale");
                var MapAttivita = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AttivitaCommerciale");

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                var cellulare = Helper.GetMapValueFromType(form, mapAnagrafica, "phone");
                var ObjAnagrafica = ObjAnagraficaList.Data.FirstOrDefault(c => cellulare.Any(l => l.ToString() == c.Cellulare));// form[mapAnagrafica.FirstOrDefault(m => m.ObjectProperty == "Cellulare").FormKey]);
                if (ObjAnagrafica == null || String.IsNullOrWhiteSpace(ObjAnagrafica.RagSoc))
                {
                    response.Success = false;
                    response.ErrorMessage = "Anagrafica temporanea non esistente";
                    return response;
                }

                var CondIniziativa = new ViewProperties_1OfOfIniziativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                CondIniziativa.Condition = new IniziativaViewCondition();
                if (ObjAnagrafica.Temporanea)
                    CondIniziativa.Condition.AnagraficaTempId = ObjAnagrafica.Id;
                else
                    CondIniziativa.Condition.AnagraficaCod = ObjAnagrafica.Codice;

                var ReqIniziativa = new RequestIniziativa();
                if (ObjAnagrafica.Temporanea)
                {
                    ReqIniziativa.AnagraficaTempId = ObjAnagrafica.Id;
                    ReqIniziativa.TipoAnagrafica = 2;
                }
                else
                {
                    ReqIniziativa.ClienteCod = ObjAnagrafica.Codice;
                    ReqIniziativa.TipoAnagrafica = 1;
                }
                ReqIniziativa.Oggetto = Helper.GetMapValue(form, mapIniziativa, "Cellulare").ToString();

                var ObjInziativaList = await client.AnagraficaPOSTAsync(ReqIniziativa);
                //var ObjInziativaList = await client.RicercaPOST19Async(null, CondIniziativa );
                var ObjIniziativa = ObjInziativaList.Data.FirstOrDefault();

                string codiceIniziativa = ObjIniziativa.Codice;

                var ObjAttivitaList = await client.ListaGET28Async(codiceIniziativa);

                RequestAttivita ReqAttivita = new RequestAttivita();
                if (ObjAnagrafica.Temporanea)
                {
                    ReqAttivita.AnagraficaTempId = ObjAnagrafica.Id;
                    ReqAttivita.TipoAnagrafica = 2;
                }
                else
                {
                    ReqAttivita.ClienteCod = ObjAnagrafica.Codice;
                    ReqAttivita.TipoAnagrafica = 1;
                }
                ReqAttivita.IniziativaCod = codiceIniziativa;
                ReqAttivita.AgenteCod = Helper.GetMapValue(null, MapAttivita, "AgenteCod").ToString();  // MapAttivita.FirstOrDefault(m => m.ObjectProperty == "AgenteCod").DefaultValue;
                ReqAttivita.Start = DateTime.Now;
                ReqAttivita.TipoId = Convert.ToInt32(Helper.GetMapValue(null, MapAttivita, "TipoId").ToString()); // Convert.ToInt32(MapAttivita.FirstOrDefault(m => m.ObjectProperty == "TipoId").DefaultValue);

                var ObjAttivita = await client.NuovoPOSTAsync(ReqAttivita);
                Helper.MapFormToObject(form, ObjAttivita.Data, MapAttivita);

                var ObjAttivitaSalvata = await client.AttivitaPUTAsync(false, false, false, ObjAttivita.Data);

                if (ObjAttivitaSalvata.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = "Attivita commerciale importata correttamente";
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = ObjAttivitaSalvata.Message;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayApiResponse> ImportAggiornaAttivitaCommerciale(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };


                var client = authResponse.crmClient;
                var mapAnagrafica = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");
                var mapIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "IniziativaCommerciale");
                var MapAttivita = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AttivitaCommerciale");

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                var ObjAnagrafica = ObjAnagraficaList.Data.FirstOrDefault(c => c.Cellulare == Helper.GetMapValue(form, mapAnagrafica, "Cellulare").ToString());// form[mapAnagrafica.FirstOrDefault(m => m.ObjectProperty == "Cellulare").FormKey]);
                if (ObjAnagrafica == null || String.IsNullOrWhiteSpace(ObjAnagrafica.RagSoc))
                {
                    response.Success = false;
                    response.ErrorMessage = "Anagrafica temporanea non esistente";

                }

                var CondIniziativa = new ViewProperties_1OfOfIniziativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                CondIniziativa.Condition = new IniziativaViewCondition();
                if (ObjAnagrafica.Temporanea)
                    CondIniziativa.Condition.AnagraficaTempId = ObjAnagrafica.Id;
                else
                    CondIniziativa.Condition.AnagraficaCod = ObjAnagrafica.Codice;

                var ReqIniziativa = new RequestIniziativa();
                if (ObjAnagrafica.Temporanea)
                {
                    ReqIniziativa.AnagraficaTempId = ObjAnagrafica.Id;
                    ReqIniziativa.TipoAnagrafica = 2;
                }
                else
                {
                    ReqIniziativa.ClienteCod = ObjAnagrafica.Codice;
                    ReqIniziativa.TipoAnagrafica = 1;
                }
                ReqIniziativa.Oggetto = Helper.GetMapValue(form, mapIniziativa, "Cellulare").ToString();

                var ObjInziativaList = await client.AnagraficaPOSTAsync(ReqIniziativa);
                //var ObjInziativaList = await client.RicercaPOST19Async(null, CondIniziativa );
                var ObjIniziativa = ObjInziativaList.Data.FirstOrDefault();

                string codiceIniziativa = ObjIniziativa.Codice;

                var ObjAttivitaList = await client.ListaGET28Async(codiceIniziativa);

                if (ObjAttivitaList.Data.Count > 1)
                {

                    var iniAttivita = ObjAttivitaList.Data.FirstOrDefault();
                    var attivita = await client.AttivitaGETAsync(iniAttivita.Codice);

                    Helper.MapFormToObject(form, attivita.Data, MapAttivita);

                    var objAttivitaAggiornata = await client.AttivitaPUTAsync(false, false, false, attivita.Data);
                }
                else
                {
                    RequestAttivita ReqAttivita = new RequestAttivita();
                    if (ObjAnagrafica.Temporanea)
                    {
                        ReqAttivita.AnagraficaTempId = ObjAnagrafica.Id;
                        ReqAttivita.TipoAnagrafica = 2;
                    }
                    else
                    {
                        ReqAttivita.ClienteCod = ObjAnagrafica.Codice;
                        ReqAttivita.TipoAnagrafica = 1;
                    }
                    ReqAttivita.IniziativaCod = codiceIniziativa;
                    ReqAttivita.AgenteCod = Helper.GetMapValue(null, MapAttivita, "AgenteCod").ToString();  // MapAttivita.FirstOrDefault(m => m.ObjectProperty == "AgenteCod").DefaultValue;
                    ReqAttivita.Start = DateTime.Now;
                    ReqAttivita.TipoId = Convert.ToInt32(Helper.GetMapValue(null, MapAttivita, "TipoId").ToString()); // Convert.ToInt32(MapAttivita.FirstOrDefault(m => m.ObjectProperty == "TipoId").DefaultValue);

                    var ObjAttivita = await client.NuovoPOSTAsync(ReqAttivita);
                    Helper.MapFormToObject(form, ObjAttivita.Data, MapAttivita);

                    var ObjAttivitaSalvata = await client.AttivitaPUTAsync(false, false, false, ObjAttivita.Data);

                    if (ObjAttivitaSalvata.Code == "STD_OK")
                    {
                        response.Success = true;
                        response.ErrorMessage = "Attivita commerciale importata correttamente";
                    }
                    else
                    {
                        response.Success = false;
                        response.ErrorMessage = ObjAttivitaSalvata.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayApiResponse> ImportCompaneo(string name, string url)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            log.Debug($"Importazione Companeo: {name} - Url: {url}");
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                HttpClient _httpClient = new HttpClient();
                _httpClient.Timeout = new TimeSpan(0, 0, 30);
                log.Debug($"Eseguo download pagina web companeo");
                HttpResponseMessage web = null;
                for(int r=0; r < 5; r++)
                {
                    try
                    {
                        web = await _httpClient.GetAsync(url);
                        if(web.ReasonPhrase == "OK")
                        {                            
                            break;
                        }
                        log.Debug($"Tentativo {r} di 4: Esito: {web.ReasonPhrase}");
                    }
                    catch (Exception ex) 
                    {
                        Thread.Sleep(10 * 1000);
                    }

                }
                if(web == null)
                {
                    response.ErrorMessage = "Impossibile scaricare la scheda del contatto";
                    response.Success = false;
                    return response;
                }
                log.Debug("Pagina scaricata con successo");
                web.EnsureSuccessStatusCode();
                string htmlContent = await web.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                var details = new LeadDetails
                {
                    UserId = int.Parse(doc.DocumentNode.SelectSingleNode("//input[@id='user_id']").GetAttributeValue("value", "0")),
                    LeadId = int.Parse(doc.DocumentNode.SelectSingleNode("//td[@id='lead_id']").InnerText.Trim()),
                    TransferDate = DateTime.Parse(doc.DocumentNode.SelectSingleNode("//td[@id='lead_send_date']").InnerText.Trim()),
                    LeadOpenDate = DateTime.Parse(doc.DocumentNode.SelectSingleNode("//td[@id='lead_open_date']").InnerText.Trim()),
                    Offer = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'Offerta :')]/following-sibling::td").InnerText.Trim(),
                    CompanyName = doc.DocumentNode.SelectSingleNode("//td[@id='user_company_name']/strong").InnerText.Trim(),
                    Gender = doc.DocumentNode.SelectSingleNode("//td[@id='user_gender']").InnerText.Trim(),
                    FirstName = doc.DocumentNode.SelectSingleNode("//td[@id='user_firstname']").InnerText.Trim(),
                    LastName = doc.DocumentNode.SelectSingleNode("//td[@id='user_lastname']").InnerText.Trim(),
                    Email = doc.DocumentNode.SelectSingleNode("//a[starts-with(@href, 'mailto:')]").InnerText.Trim(),
                    Phone = doc.DocumentNode.SelectSingleNode("//td[@id='user_phone']").InnerText.Trim(),
                    Address = doc.DocumentNode.SelectSingleNode("//td[@id='user_address']").InnerText.Trim(),
                    ZipCode = doc.DocumentNode.SelectSingleNode("//td[@id='user_zipcode']").InnerText.Trim(),
                    City = doc.DocumentNode.SelectSingleNode("//td[@id='user_city']").InnerText.Trim()
                };

                log.Debug($"Anagrafica scaricata con successo: Azienda: {details.CompanyName} | Tel: {details.Phone}");
                var questionsNode = doc.DocumentNode.SelectSingleNode("//div[@id='info_quest']");

                if (questionsNode != null)
                {
                    // Seleziona tutti i paragrafi <p> che contengono le domande e le risposte.
                    var questionParagraphs = questionsNode.SelectNodes(".//p");

                    if (questionParagraphs != null)
                    {
                        foreach (var p in questionParagraphs)
                        {
                            var question = new LeadQuestionario();

                            // La domanda è contenuta direttamente nel testo del <p>, prima del primo <br>.
                            question.Domanda = p.ChildNodes[0].InnerText.Trim();

                            // La risposta è contenuta nel primo <span> senza classe "aste".
                            var answerNode = p.SelectSingleNode(".//span[not(contains(@class, 'aste'))]");
                            if (answerNode != null)
                            {
                                question.Risposta = answerNode.InnerText.Trim();
                                // Pulisci per rimuovere i tag HTML rimanenti.
                                question.Risposta = HtmlEntity.DeEntitize(question.Risposta);
                            }

                            details.Domande.Add(question);
                        }
                    }
                }

                var json = JsonConvert.SerializeObject(details, Formatting.Indented);

                log.Debug($"Invio dati a Zapier");
                string webHookUrl = "https://hooks.zapier.com/hooks/catch/16363745/3nrmw3h/";
                var responsejson = Task.Run(async () => await Tool.Helper.SendWebhookAsync(new HttpClient(), webHookUrl, json)).GetAwaiter().GetResult();
                log.Debug($"Risposta Zapier: {responsejson.ResponseContent}");
                var formData = new NameValueCollection();

                foreach (var pair in JObject.Parse(json))
                {
                    formData.Add(pair.Key, pair.Value.ToString());
                }
                log.Debug($"Invio dati a MyWay");
                response = await ImportAnagraficaTemporaneaIniziativa(formData, name);



            }
            catch (Exception ex)
            {
                response.Success = false;
            }

            return response;

        }

        public static async Task<MyWayAnagraficheResponse> GetAnagrafiche(string idAnagrafica)
        {
            MyWayAnagraficheResponse response = new MyWayAnagraficheResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayAnagraficheResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;
                var ricerca = authResponse.crmRicerca;

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                condAnagraficaTemporanea.Condition = new AnagraficaIbridaViewCondition() { OnlyClienti = false, Tipo = 2 };
                var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                var ObjAnagraficaListResult = ObjAnagraficaList.Data.Where(c => (c.RagSoc ?? "").ToLower().Contains(idAnagrafica) || (c.AliasRagSoc ?? "").ToLower().Contains(idAnagrafica) || (c.Cellulare ?? "").ToLower().Contains(idAnagrafica) || (c.Email ?? "").ToLower().Contains(idAnagrafica)).ToList();

                if (ObjAnagraficaListResult == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "Non è stata trovata nessuna anagrafica assegnata con i parametri di ricerca inseriti";

                }
                else
                {
                    response.Anagrafiche= ObjAnagraficaListResult;
                    response.Success = true;
                    response.ErrorMessage = $"Trovate {ObjAnagraficaListResult.Count} Anagrafiche";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;
        }

        public static async Task<MyWayAnagraficaResponse> GetAnagrafica(long idAnagrafica)
        {
            MyWayAnagraficaResponse response = new MyWayAnagraficaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayAnagraficaResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                condAnagraficaTemporanea.Condition = new AnagraficaIbridaViewCondition() { OnlyClienti = false, Tipo = 2 };
                var ObjAnagrafica = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);

                var ObjAnagraficaResult = ObjAnagrafica.Data.FirstOrDefault(c => c.Id == idAnagrafica );

                if (ObjAnagraficaResult == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "Non è stata trovata nessuna anagrafica assegnata con i parametri di ricerca inseriti";

                }
                else
                {
                    response.Anagrafica = ObjAnagraficaResult;
                    response.Success = true;
                    response.ErrorMessage = $"Trovata 1 Anagrafica";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;
        }

        public static async Task<MyWayIniziativaResponse> GetIniziativeCommerciali(string codAnagrafica, string isTemporanea)
        {
            MyWayIniziativaResponse response = new MyWayIniziativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayIniziativaResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                AnagraficaIbridaView ObjAnagraficaListResult = new AnagraficaIbridaView();
                var condIniziativeCommerciali = new ViewProperties_1OfOfIniziativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                if (Convert.ToBoolean(isTemporanea))
                {
                    ObjAnagraficaListResult = ObjAnagraficaList.Data.FirstOrDefault(c => c.Id == Convert.ToInt64(codAnagrafica));
                }
                else
                {
                    ObjAnagraficaListResult = ObjAnagraficaList.Data.FirstOrDefault(c => c.Codice == codAnagrafica);
                }
                if (ObjAnagraficaListResult != null)
                {
                    
                    if (Convert.ToBoolean(isTemporanea))
                    {

                        condIniziativeCommerciali.Condition = new IniziativaViewCondition() { AnagraficaTempId = ObjAnagraficaListResult.Id };
                    }
                    else
                    {
                        condIniziativeCommerciali.Condition = new IniziativaViewCondition() { AnagraficaCod = ObjAnagraficaListResult.Codice };
                    }
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = "Nessuna Anagrafica Esistente";

                }


                var objIniziativeResp = await client.ApiCommercialiIniziativaRicercaPost(null, condIniziativeCommerciali);



                if (objIniziativeResp.Data == null || objIniziativeResp.Data.Count < 1)
                {
                    response.Success = false;
                    response.ErrorMessage = "Nessuna Iniziativa Esistente";

                }
                else
                {
                    response.IniziativeCommerciale = objIniziativeResp.Data.ToList();
                    response.Success = true;
                    response.ErrorMessage = "Iniziative commerciali esistenti";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayTrattativaResponse> GetTrattativeCommerciali(string codAnagrafica)
        {
            MyWayTrattativaResponse response = new MyWayTrattativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayTrattativaResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var condTrattativeCommerciali = new ViewProperties_1OfOfTrattativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();

                condTrattativeCommerciali.Condition = new TrattativaViewCondition() { AnagraficaCod = codAnagrafica };

                var objTrattiveResp = await client.ApiCommercialiTrattativeRicercaPost(null, condTrattativeCommerciali);

                var trattative = objTrattiveResp.Data.Select(o => new MyWayObjTrattativa(o)).ToList();
                foreach (var tr in trattative)
                {
                    var resp = await client.ApiCommercialiTrattativeGet(tr.TrattativaCod);
                    if (resp.Code == "STD_OK" && resp.Data != null)
                    {
                        tr.IniziativaCod = resp.Data.IniziativaAssociata.Codice;
                        tr.Accessoria = resp.Data.Accessoria.Value;
                        tr.Stato = resp.Data.Stato.Nome;
                    }
                    
                }



                if (objTrattiveResp.Data == null || objTrattiveResp.Data.Count < 1)
                {
                    response.Success = false;
                    response.ErrorMessage = "Nessuna Trattativa Esistente";

                }
                else
                {
                    response.OpportunitaCommerciale = trattative;
                    response.Success = true;
                    response.ErrorMessage = "Iniziative commerciali esistenti";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayStatiResponse> GetStatiTrattiva()
        {
            MyWayStatiResponse response = new MyWayStatiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayStatiResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var objStatiResp = await client.ListaGET26Async();


                if (objStatiResp.Data == null || objStatiResp.Data.Count < 1)
                {
                    response.Success = false;
                    response.ErrorMessage = "Nessuna Trattativa Esistente";

                }
                else
                {
                    response.Stati = objStatiResp.Data.Select(o => new MyWayStatoTrattiva(o)).ToList();
                    response.Success = true;
                    response.ErrorMessage = "Iniziative commerciali esistenti";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayTrattativaResponse> GetTrattativaCommerciale(string codTrattativa)
        {
            MyWayTrattativaResponse response = new MyWayTrattativaResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayTrattativaResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var condTrattativeCommerciali = new ViewProperties_1OfOfTrattativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                
                condTrattativeCommerciali.Condition = new TrattativaViewCondition() { IniziativaCod = codTrattativa};

                var objTrattiveResp = await client.ApiCommercialiTrattativeRicercaPost(null, condTrattativeCommerciali);



                if (objTrattiveResp.Data == null || objTrattiveResp.Data.Count < 1)
                {
                    response.Success = false;
                    response.ErrorMessage = "Nessuna Trattativa Esistente";

                }
                else
                {
                    response.OpportunitaCommerciale = objTrattiveResp.Data.Select(o=> new MyWayObjTrattativa(o)).ToList();
                    response.Success = true;
                    response.ErrorMessage = "Iniziative commerciali esistenti";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayApiResponse> SetTrattativaCommerciale(MyWayObjTrattativa trattativa)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayTrattativaResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;
                
                var objTrattativa = await client.ApiCommercialiTrattativeGet(trattativa.TrattativaCod);

                var objTrattativeUpdate = trattativa.UpdateTrattativa(objTrattativa.Data);
                
                objTrattativeUpdate.Revisione++;

                var objTrattiveResp = await client.ApiCommercialiTrattativePost( true, objTrattativeUpdate);
                

                if (objTrattiveResp.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = objTrattiveResp.Data.Codice;
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = objTrattiveResp.Message;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayApiResponse> PutTrattativaCommerciale(MyWayObjTrattativa trattativa)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                RequestTrattativa body = new RequestTrattativa();
                body.IniziativaCod = trattativa.IniziativaCod;
                body.AnagraficaCod = trattativa.AnagraficaCod;
                body.AgenteCod = trattativa.AgenteCod;
                body.TrattativaMasterCod = trattativa.TrattativaMasterCod;

                var objNuovaTrattativa = await client.ApiCommercialiTrattativeNuovoPost(body);


                objNuovaTrattativa.Data.Valore = Convert.ToDouble(trattativa.Valore);
                objNuovaTrattativa.Data.DataPrevista = trattativa.DataPrevista;
                objNuovaTrattativa.Data.Accessoria = trattativa.Accessoria;
                objNuovaTrattativa.Data.PercentualeChiusura = trattativa.PercentualeChiusura;
                objNuovaTrattativa.Data.Nome = trattativa.Nome;
                objNuovaTrattativa.Data.Stato = new StatoTrattativaMinDto() { Id = Convert.ToInt32(trattativa.StatoId), Nome = trattativa.Stato };




                var objNuovaTrattativaResp = await client.ApiCommercialiTrattativePut(objNuovaTrattativa.Data);



                if (objNuovaTrattativaResp.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = objNuovaTrattativaResp.Data.Codice;
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = objNuovaTrattativaResp.Message;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }

        public static async Task<MyWayApiResponse> SetAnagraficaLead(long idAnagraficaTmp, string partitaIva)
        {
            MyWayApiResponse response = new MyWayApiResponse();

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var authResponse = await CrmLogin();

                if (!authResponse.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = authResponse.Message };

                var client = authResponse.crmClient;

                var conv = new InfoConvertTempToAnagrafica();

                var result = GetAnagrafica(idAnagraficaTmp).GetAwaiter().GetResult();

                conv.RagSoc = result.Anagrafica.RagSoc;
                conv.PIva = partitaIva;
                conv.AnagraficaTempId = idAnagraficaTmp;
                conv.AliasRagSoc = result.Anagrafica.AliasRagSoc;

                

                var objConvertiResp = await client.ConvertiAsync(conv);



                if (objConvertiResp.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = "Anagrafica convertita correttamente";
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = objConvertiResp.Message;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;

        }
    }
}

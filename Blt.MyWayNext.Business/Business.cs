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
using Blt.MyWayNext.Bol;
using Blt.MyWayNext.Tool;
using Blt.MyWayNext.Proxy.Authentication;
using Blt.MyWayNext.Proxy.Business;



namespace Blt.MyWayNext.Business
{
    public static class Business
    {
        public static async Task<AuthenticationResponse> CrmLogin()
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

                LoginUserModel login = new LoginUserModel() { Name = cfg["AppSettings:userName"], Password = cfg["AppSettings:userPassword"] };
                var res = await autClient.LoginAsync(login);

                var token = res.Data.Token;

                Guid aziendaId = Guid.Empty;

                aziendaId = res.Data.Utente.Aziende.FirstOrDefault(a => a.Azienda.Nome == cfg["AppSettings:azienda"]).AziendaId;

                if (aziendaId == Guid.Empty)
                    return new AuthenticationResponse() { Success = false, Client = httpClient, Message = "Azienda non trovata" };

                // Imposta l'header di autorizzazione con il token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resCompany = await autClient.SelectCompanyAsync(aziendaId);
                var bearerToken = Helper.EstraiTokenDaJson(resCompany.Data.ToString());
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                var client = new Blt.MyWayNext.Proxy.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);
                return new AuthenticationResponse() { Success = true, Client = httpClient, Message = "Autenticazione effettuata correttamente", Token = bearerToken, crmClient = client };

            }
            catch (Exception ex)
            {
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

                var clienteNuovoResponse = await client.NuovoGET5Async();
                var nuovoCliente = clienteNuovoResponse.Data;
                var mappings = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");

                Helper.MapFormToObject(form, nuovoCliente, mappings);
                var resIbride = await client.IbridePUTAsync(nuovoCliente);


                if (resIbride.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = "Anagrafica temporanea importata correttamente";
                }
                else
                {
                    response.Success = false;
                    response.ErrorMessage = resIbride.Message;
                }
            }
            catch (Exception ex)
            {
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
                var clienteNuovoResponse = await client.NuovoGET5Async();
                var ObjAnagraficaTemporanea = clienteNuovoResponse.Data;
                var mapAnagraficaTemporanea = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");
                if (mapAnagraficaTemporanea.Count > 0)
                {
                    var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                    var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                    bool isAnagraficaTemp = false;
                    long anagraficaId = 0;
                    int tipoAnagrafica = 0;
                    string referenteId = string.Empty;
                    bool newContatto = false;
                    if (ObjAnagraficaList.Data.Any(c => c.Cellulare == Helper.GetMapValue(form, mapAnagraficaTemporanea, "Cellulare").ToString()))
                    {
                        response.Success = false;
                        response.ErrorMessage = "Anagrafica già presente\n";
                        var a = ObjAnagraficaList.Data.FirstOrDefault(c => c.Cellulare == Helper.GetMapValue(form, mapAnagraficaTemporanea, "Cellulare").ToString());
                        isAnagraficaTemp = a.Temporanea;
                        anagraficaId = a.Id;
                        tipoAnagrafica = a.TipoAnagrafica;
                    }
                    else
                    {
                        Helper.MapFormToObject(form, ObjAnagraficaTemporanea, mapAnagraficaTemporanea);
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

                        var objContatto = await client.NuovoGET3Async(String.Empty);
                        var mapContatto = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "Contatto");
                        Helper.MapFormToObject(form, objContatto.Data, mapContatto);
                        var associazione = new RequestAddReferentWithAss() { Referente = objContatto.Data, Associa = new RequestAssociaReferente() { TypeAssociation = 1, KeyAss = anagraficaId.ToString(), ReferenteCod = objContatto.Data.Codice } };
                        var respContatto = await client.ReferentiPUTAsync(associazione);
                        if (respContatto.Code == "STD_OK")
                        {
                            referenteId = respContatto.Data.Codice;
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage += respContatto.Message;
                        }
                    }
                    if (!newContatto)
                    {
                        var mapCreaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "CreaIniziativa");
                        if (mapCreaIniziativa.Count == 0)
                        {
                            response.Success = true;
                            response.ErrorMessage += "Mapping CreaIniziativa non presente, impossibile proseguire";
                            return response;
                        }
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
                        ReqIniziativa.Oggetto = Helper.GetMapValue(form, mapCreaIniziativa, "Oggetto").ToString();

                        var ObjInziativaList = await client.AnagraficaPOSTAsync(ReqIniziativa);
                        if (ObjInziativaList.Data.Count > 0)
                        {
                            response.Success = true;
                            response.ErrorMessage += "Iniziativa commerciale aggiornata correttamente\n";
                        }
                        else
                        {

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
                            var ObjAggiornaIniziativa = await client.NuovoPOST2Async(true, ObjCreaIniziativa);

                            var mapAggiornaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AggiornaIniziativa");
                            if (mapAggiornaIniziativa.Count > 0)
                            {
                                Helper.MapFormToObject(form, ObjAggiornaIniziativa.Data, mapAggiornaIniziativa);
                                var resp = await client.IniziativaPOSTAsync(ObjAggiornaIniziativa.Data);

                                if (resp.Code == "STD_OK")
                                {
                                    var mapAttivitaCommerciale = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AttivitaCommerciale");
                                    if (mapAttivitaCommerciale.Count > 0)
                                    {
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
                                            response.Success = true;
                                            response.ErrorMessage += "Iniziativa commerciale creata correttamente con Attività commerciale annessa\n";
                                        }
                                        else
                                        {
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
                    }
                    else
                    {

                        response.Success = true;
                        response.ErrorMessage += "Anagrafica già presente";
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

        public static async Task<MyWayAnagraficheResponse> GetAnagrafiche(string anagrafica)
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

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                var ObjAnagraficaList = await client.RicercaPOST12Async(null, condAnagraficaTemporanea);
                var ObjAnagraficaListResult = ObjAnagraficaList.Data.Where(c => c.RagSoc.Contains(anagrafica) || c.AliasRagSoc.Contains(anagrafica) || c.Cellulare.Contains(anagrafica) || c.Email.Contains(anagrafica));

                if (ObjAnagraficaList == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "Anagrafica temporanea non esistente";

                }
                else
                {
                    response.Anagrafiche= ObjAnagraficaListResult.Select(a => new AnagraficaDto()
                    {
                        AliasRagSoc = a.AliasRagSoc,
                        Cellulare = a.Cellulare,
                        Email = a.Email,
                        RagSoc = a.RagSoc,
                        Codice = a.Codice,
                        Telefono = a.Telefono,
                        PIva = a.PIva
                    }).ToList();
                    response.Success = true;
                    response.ErrorMessage = "Anagrafica temporanea esistente";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;

            }

            return response;
        }

        public static async Task<MyWayIniziativaResponse> GetIniziativeCommerciali(string name)
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

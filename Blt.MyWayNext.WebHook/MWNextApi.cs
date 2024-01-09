using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Specialized;
using Blt.MyWayNext.Authentication;
using Blt.MyWayNext.Business;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Blt.MyWayNext.WebHook.Bol;
using Blt.MyWayNext.WebHook.Tools;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;

namespace Blt.MyWayNext.WebHook.Api
{
    public class MWNextApi
    {
        public async Task<MyWayApiResponse> ImportAnagraficaTemporanea(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var auth= await Autentication();

                if(!auth.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = auth.Message };

                var httpClient = auth.Client;

                var client = new Blt.MyWayNext.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);
                ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null condition = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();


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

        public async Task<MyWayApiResponse> ImportAnagraficaTemporaneaIniziativa(NameValueCollection form, string name)
        {
            MyWayApiResponse response = new MyWayApiResponse();
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                var auth = await Autentication();

                if (!auth.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = auth.Message };

                var httpClient = auth.Client;

                var client = new Blt.MyWayNext.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);
                ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null condition = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();

                //creo anagrafica
                var clienteNuovoResponse = await client.NuovoGET5Async();
                var ObjAnagraficaTemporanea = clienteNuovoResponse.Data;
                var mapAnagraficaTemporanea = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name , "AnagraficaTemporanea");

                Helper.MapFormToObject(form, ObjAnagraficaTemporanea, mapAnagraficaTemporanea);                
                var resIbride = await client.IbridePUTAsync(ObjAnagraficaTemporanea);

                
                //creo iniziativa
                RequestIniziativa ObjCreaIniziativa = new RequestIniziativa();
                if (resIbride.Data.AnagraficaTempId != 0)
                {
                    ObjCreaIniziativa.AnagraficaTempId = resIbride.Data.AnagraficaTempId;
                    ObjCreaIniziativa.TipoAnagrafica = 2;
                }
                else
                {
                    ObjCreaIniziativa.ClienteCod = resIbride.Data.CodiceId;
                    ObjCreaIniziativa.TipoAnagrafica = 1;
                }
                
                ObjCreaIniziativa.AgenteCod = cfg["AppSettings:agenteCRMLead"];               
                var mapCreaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "CreaIniziativa");
                Helper.MapFormToObject(form, ObjCreaIniziativa, mapCreaIniziativa);
                var ObjAggiornaIniziativa = await client.NuovoPOST2Async(true, ObjCreaIniziativa);

                var mapAggiornaIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AggiornaIniziativa");
                Helper.MapFormToObject(form, ObjAggiornaIniziativa.Data, mapAggiornaIniziativa);
                var resp = await client.IniziativaPOSTAsync(ObjAggiornaIniziativa.Data);

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


        public async Task<MyWayApiResponse> ImportAttivitaCommerciale(NameValueCollection form, string name)
        { 
            MyWayApiResponse response = new MyWayApiResponse();
            
            try
            {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                                .SetBasePath(Directory.GetCurrentDirectory())
                                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration cfg = builder.Build();

                var auth = await Autentication();

                if (!auth.Success)
                    return new MyWayApiResponse() { Success = false, ErrorMessage = auth.Message };

                var httpClient = auth.Client;

                var client = new Blt.MyWayNext.Business.Client(cfg["AppSettings:baseBussUrl"], httpClient);
                var mapAnagrafica = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");
                var mapIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "IniziativaCommerciale");
                var MapAttivita = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AttivitaCommerciale");

                var condAnagraficaTemporanea = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                var ObjAnagraficaList = await client.RicercaPOST11Async(null, condAnagraficaTemporanea);
                var ObjAnagrafica = ObjAnagraficaList.Data.FirstOrDefault(c => c.Cellulare == form[mapAnagrafica.FirstOrDefault(m => m.ObjectProperty == "Cellulare").FormKey]);
               
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
                ReqIniziativa.Oggetto = form[mapIniziativa.FirstOrDefault(m => m.ObjectProperty == "Oggetto").FormKey];
                
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
                ReqAttivita.AgenteCod = cfg["AppSettings:agenteCRMLead"];
                ReqAttivita.Start = DateTime.Now;
                ReqAttivita.TipoId = 12;

                var ObjAttivita = await client.NuovoPOSTAsync(ReqAttivita);
                Helper.MapFormToObject(form, ObjAttivita.Data, MapAttivita);
                ObjAttivita.Data.Esito.Id = 1;
                ObjAttivita.Data.Esito.Nome = "Positivo";
                ObjAttivita.Data.Stato.Id = 4;
                ObjAttivita.Data.Stato.Nome = "Completata";
                

                var ObjAttivitaSalvata = await client.AttivitaPUTAsync(false, false, false, ObjAttivita.Data);

                if (ObjAttivitaSalvata.Code == "STD_OK")
                {
                    response.Success = true;
                    response.ErrorMessage = "Anagrafica temporanea importata correttamente";
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

        async Task<AuthenticationResponse> Autentication()
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration cfg = builder.Build();

                httpClient  = new System.Net.Http.HttpClient();
                var autClient = new Blt.MyWayNext.Authentication.Client(cfg["AppSettings:baseAuthUrl"], httpClient);

                LoginUserModel login = new LoginUserModel() { Name = cfg["AppSettings:userName"], Password = cfg["AppSettings:userPassword"] };
                var res = await autClient.LoginAsync(login);

                var token = res.Data.Token;
                var aziendaId = res.Data.Utente.Aziende.First().AziendaId;

                // Imposta l'header di autorizzazione con il token
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resCompany = await autClient.SelectCompanyAsync(aziendaId);
                var bearerToken = Helper.EstraiTokenDaJson(resCompany.Data.ToString());
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                return new AuthenticationResponse() { Success = true, Client = httpClient, Message = "Autenticazione effettuata correttamente", Token = bearerToken };

            }
            catch (Exception ex)
            {
                return new AuthenticationResponse() { Success = false, Client = httpClient, Message = ex.Message };

            }
        }
    }

    public class MyWayApiResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        // Aggiungi qui altri campi di risposta se necessario
    }
    public class AuthenticationResponse
    {
        public bool Success { get; set; }
        public HttpClient Client { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
    }

}
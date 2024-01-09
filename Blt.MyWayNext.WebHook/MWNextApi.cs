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


                var clienteNuovoResponse = await client.NuovoGET5Async();
                var nuovoCliente = clienteNuovoResponse.Data;
                var mappings = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name , "AnagraficaTemporanea");

                Helper.MapFormToObject(form, nuovoCliente, mappings);
                if(String.IsNullOrWhiteSpace(nuovoCliente.RagSoc) )
                {
                    nuovoCliente.AliasRagSoc = nuovoCliente.Cognome + " " + nuovoCliente.Nome;
                    nuovoCliente.RagSoc = nuovoCliente.Cognome + " " + nuovoCliente.Nome;
                }
                var resIbride = await client.IbridePUTAsync(nuovoCliente);

                RequestIniziativa newIniziativa = new RequestIniziativa();
                newIniziativa.TipoAnagrafica = 2;
                newIniziativa.AnagraficaTempId = resIbride.Data.AnagraficaTempId;
                newIniziativa.AgenteCod = cfg["AppSettings:agenteCRMLead"];
                var mappingsNewIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "NewIniziativa");
                Helper.MapFormToObject(form, newIniziativa, mappingsNewIniziativa);
                var iniziativa = await client.NuovoPOST2Async(true, newIniziativa);

                var mappingIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "Iniziativa");
                Helper.MapFormToObject(form, iniziativa.Data, mappingIniziativa);

                var resp = await client.IniziativaPOSTAsync(iniziativa.Data);

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
                ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null condition = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();


                var clienteNuovoResponse = await client.NuovoGET5Async();
                var nuovoCliente = clienteNuovoResponse.Data;
                var mappings = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");

                Helper.MapFormToObject(form, nuovoCliente, mappings);
                if (String.IsNullOrWhiteSpace(nuovoCliente.RagSoc))
                {
                    nuovoCliente.AliasRagSoc = nuovoCliente.Cognome + " " + nuovoCliente.Nome;
                    nuovoCliente.RagSoc = nuovoCliente.Cognome + " " + nuovoCliente.Nome;
                }
                var mappingsAnagraficaTemporanea = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "AnagraficaTemporanea");


                ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null condition = new ViewProperties_1OfOfAnagraficaIbridaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                condition.Filters = new List<Filter>();
                condition.Filters.Add(new Filter() { Field = "Cellulare", CompareOperator = CompareOperator._0, Value = new List<string>() { form[mappingsAnagraficaTemporanea.FirstOrDefault(m => m.ObjectProperty == "Cellulare").DefaultValue] }});
                var resRicerca = await client.RicercaPOST11Async(null, condition);
                var AnagraficaTempId = resRicerca.Data.Where(r => r.NumeroIniAperte > 0).FirstOrDefault().Id;

                var condizioniRicerca = new ViewProperties_1OfOfIniziativaViewConditionAndEntitiesAnd_0AndCulture_neutralAndPublicKeyToken_null();
                condizioniRicerca.Condition.AnagraficaTempId = AnagraficaTempId;
                var ricercaIniziativa = client.RicercaPOST19Async(null, condizioniRicerca );
                ricercaIniziativa.Result.Data.Where(i=>i.Oggetto == form[mappingsAnagraficaTemporanea.FirstOrDefault(m => m.ObjectProperty == "Oggetto").ObjectProperty])

                condizioniRicerca.Condition.AnagraficaTempId
                ricercaIniziativa.Result.Data.Where(r=>r.RagSoc)
                var resIbride = await client.IbridePUTAsync(nuovoCliente);
                RequestIniziativa newIniziativa = new RequestIniziativa();
                newIniziativa.TipoAnagrafica = 2;
                newIniziativa.AnagraficaTempId = resIbride.Data.AnagraficaTempId;
                newIniziativa.AgenteCod = cfg["AppSettings:agenteCRMLead"];
                
                var mappingsNewIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "NewIniziativa");
                Helper.MapFormToObject(form, newIniziativa, mappingsAnagraficaTemporanea);
                var iniziativa = await client.NuovoPOST2Async(true, newIniziativa);

                var mappingIniziativa = FieldMapping.LoadFromXml(cfg["AppSettings:mapping"], name, "Iniziativa");
                Helper.MapFormToObject(form, iniziativa.Data, mappingIniziativa);

                var resp = await client.IniziativaPOSTAsync(iniziativa.Data);

                var reqAttivitàCommerciale = new RequestAttivita();
                reqAttivitàCommerciale.AnagraficaTempId = resIbride.Data.AnagraficaTempId;
                reqAttivitàCommerciale.IniziativaCod = resp.Data.Codice;
                reqAttivitàCommerciale.AgenteCod = cfg["AppSettings:agenteCRMLead"];
                reqAttivitàCommerciale.TipoId = 1;
                

                var attivitaCommerciale = await client.NuovoPOSTAsync(reqAttivitàCommerciale);
                attivitaCommerciale.Data.AttivitaSvoltaText
                var respAttComm = await client.AttivitaPUTAsync(true, false, false, attivitaCommerciale.Data);
                
                respAttComm.Additional

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
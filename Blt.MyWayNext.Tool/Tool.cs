using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Blt.MyWayNext.Bol;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using Blt.MyWayNext.Proxy.Authentication;
using System.Runtime;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;
using log4net.Config;
using System.Reflection;
using System.Globalization;

namespace Blt.MyWayNext.Tool
{
    public static class Helper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static DataTable SqlSelect(string query, object[] pars)
        {
            try
            {
                IConfiguration _configuration;

                IConfigurationBuilder builder = new ConfigurationBuilder()
                                                    .SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                _configuration = builder.Build();
                SqlConnection con;
                string connectionName = _configuration["AppSettings:SqlConnection"].ToString();
                con = new SqlConnection(connectionName);


                DataTable sqlquery = new DataTable();
                SqlDataAdapter cmd1 = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd1.SelectCommand = cmd;
                if (pars != null)
                {
                    int i = 0;

                    foreach (object par in pars)
                    {
                        SqlParameter gp = new SqlParameter("@param" + i.ToString(), par);
                        cmd.Parameters.Add(gp);

                        i++;
                    }
                }
                //cmd1.SelectCommand.CommandTimeout = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SqlTimeout"]);

                cmd1.Fill(sqlquery);

                sqlquery.TableName = "SqlQuery";

                return sqlquery;
            }
            catch (Exception e)
            {
                return new DataTable();
            }

        }

        public static async Task<AuthenticationResponse> Autentication()
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

                return new AuthenticationResponse() { Success = true, Client = httpClient, Message = "Autenticazione effettuata correttamente", Token = bearerToken };

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

        public static async Task<ResponseWebhook> SendWebhookAsync(HttpClient httpClient, string url, string jsonData)
        {
            try
            {
                HttpContent content;
                content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
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


        public static void SetProperty(object obj, string[] propertyPath, string value, FieldMapping mapping)
        {
            if (obj == null || propertyPath.Length == 0) return;

            var propertyInfo = obj.GetType().GetProperty(propertyPath[0]);
            if (propertyInfo == null) return;

            if (propertyPath.Length > 1)
            {
                var subObj = propertyInfo.GetValue(obj);
                if (subObj == null)
                {
                    // Se la sotto proprietà è null, prova a crearne una nuova istanza se il tipo lo permette
                    var subObjType = propertyInfo.PropertyType;
                    if (!subObjType.IsAbstract && !subObjType.IsInterface && subObjType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        subObj = Activator.CreateInstance(subObjType);
                        propertyInfo.SetValue(obj, subObj);
                    }
                    else
                    {
                        // Se non è possibile creare una nuova istanza, salta questa proprietà
                        return;
                    }
                }

                SetProperty(subObj, propertyPath.Skip(1).ToArray(), value, mapping);
            }
            else
            {
                object convertedValue = ConvertToType(value, mapping);
                propertyInfo.SetValue(obj, convertedValue);
            }
        }

        /// <summary>
        /// Funzione che restituisce un oggetto di tipo JObject a partire da una stringa json formattata come NameValueCollection, iterando nei nodi figli, il percorso deve essere di tipo "nodo1.nodo2.nodo3"
        /// </summary>
        /// <param name="form">elenco coppie chiave / valori da mappare</param>
        /// <param name="objectToMap">istanza oggetto da popolare</param>
        /// <param name="mappings">elenco mappatura da eseguire</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void MapFormToObject(NameValueCollection form, object objectToMap, List<FieldMapping> mappings)
        {
            try
            {
                if (objectToMap == null) throw new ArgumentNullException(nameof(objectToMap));

                log.Debug($"Inizio valorizzazione {objectToMap.ToString()} usando la mappatura connessa all'oggetto {form} ");
                log.Debug($"Analizzo Campi non Aggregati");
                foreach (var mapping in mappings.Where(m => !m.Aggregate))
                {

                    var propertyPath = mapping.ObjectProperty.Split('.');
                    string value = GetValue(form, mapping, mappings);
                    SetProperty(objectToMap, propertyPath, value, mapping);
                    log.Debug($"Valorizzato {mapping.ObjectProperty} con {value} di tipo {mapping.DataType}");

                }
                // Gestione dei campi aggregati
                foreach (var group in mappings.Where(m => m.Aggregate).GroupBy(m => m.ObjectProperty))
                {
                    var aggregatedParts = new List<string>();

                    foreach (var mapping in group)
                    {
                        if (form.AllKeys.Contains(mapping.FormKey) || !string.IsNullOrEmpty(mapping.DefaultValue))
                        {
                            string value = GetValue(form, mapping, mappings);
                            if (!string.IsNullOrEmpty(value))
                            {
                                string separator = ConvertEscapeSequences(mapping.AggregateSeparator);
                                
                                aggregatedParts.Add(value + separator);
                            }
                        }
                    }

                    string aggregatedValue = string.Join("", aggregatedParts).TrimEnd();

                    if (!string.IsNullOrEmpty(aggregatedValue))
                    {
                        var propertyPath = group.Key.Split('.');
                        SetProperty(objectToMap, propertyPath, aggregatedValue, group.First());
                        log.Debug($"Valorizzato {group.Key} con {aggregatedValue} di tipo {group.First().DataType}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static string GetValue(NameValueCollection form, FieldMapping mapping)
        {
            string value;
            if (form.AllKeys.Contains(mapping.FormKey) && !string.IsNullOrEmpty(form[mapping.FormKey]))
            {
                value = form[mapping.FormKey];
                if (!string.IsNullOrEmpty(mapping.AggregatePrefix))
                {
                    value = mapping.AggregatePrefix + value;
                }
            }
            else
            {
                value = GetDefaultValue(form, mapping.DefaultValue, mapping);
            }
            return value;
        }

        public static string GetValue(NameValueCollection form, FieldMapping mapping, List<FieldMapping> fieldMappings)
        {
            string value;
            if (form.AllKeys.Contains(mapping.FormKey) && !string.IsNullOrEmpty(form[mapping.FormKey]))
            {
                value = form[mapping.FormKey];
                if (!string.IsNullOrEmpty(mapping.AggregatePrefix))
                {
                    value = mapping.AggregatePrefix + value;
                }
            }
            else
            {
                value = Helper.GetDefaultValue(form, mapping, fieldMappings);
            }
            return value;
        }

        public static object GetMapValue(NameValueCollection form, List<FieldMapping> mapping, string property)
        {
            var map = mapping.Where(m => m.ObjectProperty == property).FirstOrDefault();
            if (map == null)
            {
                return string.Empty;
            }
            string value;
            if (form != null && form.AllKeys.Contains(map.FormKey) && !string.IsNullOrEmpty(form[map.FormKey]))
            {
                value = form[map.FormKey];
                if (!string.IsNullOrEmpty(map.AggregatePrefix))
                {
                    value = map.AggregatePrefix + value;
                }
            }
            else
            {
                value = GetDefaultValue(form, map, mapping);
            }
            return ConvertToType(value, map);
        }

        public static List<object> GetMapValueFromType(NameValueCollection form, List<FieldMapping> mapping, string type)
        {
            List<object> list = new List<object>();
            var maps = mapping.Where(m => m.DataType == type).ToList();
            if (maps == null  || maps.Count < 1)
            {
                return list;
            }
            string value;
            foreach (var map in maps)
            {
                if (form != null && form.AllKeys.Contains(map.FormKey) && !string.IsNullOrEmpty(form[map.FormKey]))
                {
                    value = form[map.FormKey];
                    if (!string.IsNullOrEmpty(map.AggregatePrefix))
                    {
                        value = map.AggregatePrefix + value;
                    }
                }
                else
                {
                    value = GetDefaultValue(form, map, mapping);
                }
                list.Add(ConvertToType(value, map));
            }
            return list;
        }

        public static object GetMapName(NameValueCollection form, List<FieldMapping> mapping, string name)
        {
            var map = mapping.Where(m => m.FormKey == name).FirstOrDefault();
            if (map == null)
            {
                return string.Empty;
            }
            string value;
            if (form != null && form.AllKeys.Contains(map.FormKey) && !string.IsNullOrEmpty(form[map.FormKey]))
            {
                value = form[map.FormKey];
                if (!string.IsNullOrEmpty(map.AggregatePrefix))
                {
                    value = map.AggregatePrefix + value;
                }
            }
            else
            {
                value = GetDefaultValue(form, map, mapping);
            }
            return ConvertToType(value, map);
        }

        public static string GetDefaultValue(NameValueCollection form, string defaultValue, FieldMapping map)
        {
            if (string.IsNullOrEmpty(defaultValue))
                return defaultValue;

            var matches = Regex.Matches(defaultValue, @"\$(\S+)");//\$([a-zA-Z]+)");
            if (matches.Count > 0)
                log.Debug($"Analizzo {defaultValue}. Trovate {matches.Count} variabili da sostituire");

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string replacement = Helper.GetMapName(form, new List<FieldMapping> { map }, key).ToString() ?? "";
                string tempValue = defaultValue.Replace(match.Value, replacement);
                log.Debug($"Valore originale: {defaultValue}. Trovata variabile {match.Value}. Sostituisco con {replacement}. Nuovo valore {tempValue}");
                defaultValue = tempValue;
            }

            return defaultValue;
        }

        public static string GetDefaultValue(NameValueCollection form, FieldMapping map, List<FieldMapping> mapping)
        {
            if (form != null && form.AllKeys.Any(f => f == map.FormKey) && string.IsNullOrWhiteSpace(form[map.FormKey]) && String.IsNullOrWhiteSpace(map.DefaultValue))
                return form[map.FormKey];
            string result = map.DefaultValue ?? String.Empty;
            var matches = Regex.Matches(map.DefaultValue, @"\$(\S+)");//\$([a-zA-Z]+)");

            if (matches.Count > 0)
                log.Debug($"Analizzo {map.DefaultValue}. Trovate {matches.Count} variabili da sostituire");
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string replacement = Helper.GetMapName(form, mapping, key).ToString() ?? "";
                string tempValue = result.Replace(match.Value, replacement);
                log.Debug($"Valore originale: {result}. Trovata variabile {match.Value}. Sostituisco con {replacement}. Nuovo valore {tempValue}");
                result = tempValue;

            }

            return result;
        }

        public static string ConvertEscapeSequences(string input)
        {
            if (input == null) return null;

            return input.Replace("\\n", "\n")   // Nuova linea
                        .Replace("\\t", "\t")   // Tab
                        .Replace("\\r", "\r")   // Ritorno a capo
                        .Replace("\\\"", "\"")  // Doppio apice
                        .Replace("\\\\", "\\"); // Backslash
        }
        public static object ConvertToType(string value,  FieldMapping mapping)
        {
            // Gestione dei casi comuni (tipi primitivi, stringhe, ecc.)
            switch (mapping.DataType.ToLower())
            {
                case "int":
                case "int32":
                case "integer":
                case "system.int32":
                    return int.TryParse(value, out int intValue) ? intValue : default(int);
                case "long":
                case "int64":
                case "system.int64":
                    return long.TryParse(value, out long longValue) ? longValue : default(long);
                case "bool":
                case "boolean":
                case "system.boolean":
                    return bool.TryParse(value, out bool boolValue) ? boolValue : default(bool);
                case "double":
                case "system.double":
                    return double.TryParse(value, out double doubleValue) ? doubleValue : default(double);
                case "phone":
                    return FormatPhoneNumber(value);
                case "string":
                case "system.string":
                    return ApplyStringFormat(value, mapping.FormatString);
                case "datetime":
                    return DateTime.TryParse(value, out DateTime dateValue) ? dateValue : DateTime.Now;
                case "datetimeoffset":
                    return DateTimeOffset.TryParse(value, out DateTimeOffset dateOffValue) ? dateOffValue : DateTimeOffset.Now;
                case "email":
                    // verifica se la stringa è compatibile con una email
                    if (Regex.IsMatch(value, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
                    {
                        return value;
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(value))
                            throw new InvalidOperationException($"Il valore '{value}' non è un indirizzo email valido");
                        else
                            return value;
                    }

                // Aggiungi qui altri tipi se necessario
                default:
                    // Per tipi non gestiti direttamente, prova a usare il metodo ChangeType
                    var type = Type.GetType(mapping.DataType);
                    if (type == null)
                        throw new InvalidOperationException($"Tipo non riconosciuto: {mapping.DataType}");

                    try
                    {
                        return Convert.ChangeType(value, type);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Impossibile convertire il valore '{value}' in tipo '{mapping.DataType}'", ex);
                    }
            }
        }
        public static string EstraiTokenDaJson(string json)
        {
            var jObject = JObject.Parse(json);
            string token = jObject["token"].ToString();
            return token;
        }

        public static string FormatPhoneNumber(string input)
        {
            // Rimuove tutti i caratteri non numerici, eccetto il segno '+'
            string numericOnly = Regex.Replace(input, "[^0-9+]", "");

            // Controlla e converte il prefisso internazionale da 00 a +
            if (numericOnly.StartsWith("00"))
            {
                numericOnly = "+" + numericOnly.Substring(2);
            }
            else if (!numericOnly.StartsWith("+"))
            {
                // Aggiunge il prefisso italiano se non è presente un prefisso internazionale
                numericOnly = "+39" + numericOnly;
            }

            return numericOnly;
        }

        public static async Task<T> DeserializeJson<T>(Stream body)
        {
            try
            {
                string jsonString = await new StreamReader(body).ReadToEndAsync();
                // Deserializza la stringa JSON nell'oggetto specificato dal tipo generico T
                T obj = JsonConvert.DeserializeObject<T>(jsonString);
                return obj;
            }
            catch (JsonException jsonEx)
            {
                // Gestisci l'eccezione relativa alla deserializzazione JSON
                throw new InvalidOperationException($"Errore durante la deserializzazione: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                // Gestisci eventuali altre eccezioni impreviste
                throw new InvalidOperationException($"Errore imprevisto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Funzione che lancia un Webhook verso l'indirizzo passato come parametro che accetta come parametri una stringa che determina la codifica e poi una lista di coppie chiave valore e restituisce un oggetto di tipo ResponseWebhook
        /// </summary>
        /// <param name="webhook"></param>
        /// <param name="tipo"></param>
        /// <param name="Collection"></param>
        /// <returns>ResponseWebhook</returns>
        public class ResponseWebhook
        {
            public bool Success { get; set; }
            public string ResponseContent { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            // Aggiungi altri campi se necessari
        }

        public static NameValueCollection ConvertToNameValueCollection(MetaWebhookEvent webhookEvent)
        {
            
            var collection = new NameValueCollection();
            if (webhookEvent == null)
                return collection;
            // Aggiungi tutti i campi principali dell'evento
            collection.Add("id", webhookEvent.Id);
            collection.Add("externalId", webhookEvent.ExternalId);
            collection.Add("schemaId", webhookEvent.SchemaId);
            collection.Add("eventType", webhookEvent.EventType);
            collection.Add("createdTimestamp", webhookEvent.CreatedTimestamp.ToString());
            collection.Add("updatedTimestamp", webhookEvent.UpdatedTimestamp.ToString());

            // Aggiungi i campi dinamici
            foreach (var field in webhookEvent.Fields.Where(f=>f.Id.ToLower() != "fields"))
            {
                collection.Add(field.Id, field.Value);
            }

            // Gestisci i contactFields separatamente se necessario
            // Nota: questa parte potrebbe essere ridondante se i contactFields sono già inclusi nei Fields
            // e quindi potrebbe essere omessa se si desidera evitare duplicati.
            foreach (var item in webhookEvent.ContactFields)
            {
                collection.Add(item.Key, item.Value);
            }

            return collection;
        }



        /*##########################################################################################
        ############################################################################################
        ##########################################################################################*/

        /// <summary>
        /// Mappa i campi di un JObject dentro l'oggetto `objectToMap` usando una lista di FieldMapping.
        /// I percorsi del JSON sono specificati in FieldMapping.FormKey con notazione "nodo.nodofiglio".
        /// L'oggetto di destinazione è specificato in FieldMapping.ObjectProperty (stesso concetto di prima).
        /// </summary>
        public static void MapJsonToObject(JToken json, object objectToMap, List<FieldMapping> mappings)
        {
            try
            {
                if (objectToMap == null) throw new ArgumentNullException(nameof(objectToMap));

                log.Debug($"Inizio valorizzazione {objectToMap} usando la mappatura su JToken ");
                log.Debug($"Analizzo Campi non Aggregati");

                // Campi non aggregati
                foreach (var mapping in mappings.Where(m => !m.Aggregate))
                {
                    var propertyPath = mapping.ObjectProperty.Split('.');

                    // Ricavo il valore dal JSON (o default)
                    string value = GetValue(json, mapping, mappings);

                    // Imposto la property su objectToMap
                    SetProperty(objectToMap, propertyPath, value, mapping);
                    log.Debug($"Valorizzato {mapping.ObjectProperty} con {value} di tipo {mapping.DataType}");
                }

                // Campi aggregati
                foreach (var group in mappings.Where(m => m.Aggregate).GroupBy(m => m.ObjectProperty))
                {
                    var aggregatedParts = new List<string>();

                    foreach (var mapping in group)
                    {
                        string value = GetValue(json, mapping, mappings);
                        if (!string.IsNullOrEmpty(value))
                        {
                            string separator = ConvertEscapeSequences(mapping.AggregateSeparator);
                            aggregatedParts.Add(value + separator);
                        }
                    }

                    string aggregatedValue = string.Join("", aggregatedParts).TrimEnd();
                    if (!string.IsNullOrEmpty(aggregatedValue))
                    {
                        var propertyPath = group.Key.Split('.');
                        SetProperty(objectToMap, propertyPath, aggregatedValue, group.First());
                        log.Debug($"Valorizzato {group.Key} con {aggregatedValue} di tipo {group.First().DataType}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Ricava una singola stringa dal JSON a partire da mapping.FormKey (dot notation).
        /// Se non esiste, prova a usare defaultValue.
        /// </summary>
        public static string GetValue(JToken json, FieldMapping mapping, List<FieldMapping> fieldMappings)
        {
            if (!String.IsNullOrWhiteSpace(mapping.FormKey))
            {
                // Cerco il token
                var token = json.SelectToken(mapping.FormKey);

                if (token != null && !string.IsNullOrEmpty(token.ToString()))
                {
                    // Ho trovato un valore effettivo
                    string value = token.ToString();
                    if (!string.IsNullOrEmpty(mapping.AggregatePrefix))
                    {
                        value = mapping.AggregatePrefix + value;
                    }
                    return value;
                }
                else
                {
                    // Uso la logica di default
                    return GetDefaultValue(json, mapping, fieldMappings);
                }
            }
            else
            {
                return GetDefaultValue(json, mapping, fieldMappings);
            }
        }

        /// <summary>
        /// Restituisce i valori (object) di tutti i FieldMapping che hanno DataType = type
        /// presi dal JSON (dot notation in `FormKey`).
        /// </summary>
        public static List<object> GetMapValueFromType(JToken json, List<FieldMapping> fieldMappings, string type)
        {
            List<object> list = new List<object>();
            var maps = fieldMappings.Where(m => m.DataType == type).ToList();
            if (maps == null || maps.Count < 1)
                return list;

            foreach (var map in maps)
            {
                var token = json.SelectToken(map.FormKey);
                if (token != null && !string.IsNullOrEmpty(token.ToString()))
                {
                    string val = token.ToString();
                    if (!string.IsNullOrEmpty(map.AggregatePrefix))
                        val = map.AggregatePrefix + val;

                    list.Add(ConvertToType(val, map));
                }
                else
                {
                    string defaultVal = GetDefaultValue(json, map, fieldMappings);
                    list.Add(ConvertToType(defaultVal, map));
                }
            }
            return list;
        }

        /// <summary>
        /// Cerca un FieldMapping in base alla proprietà e restituisce il valore corrispondente dal JSON.
        /// </summary>
        public static object GetMapValue(JToken json, List<FieldMapping> mapping, string property)
        {
            
            var map = mapping.FirstOrDefault(m => m.ObjectProperty == property);
            if (map == null || String.IsNullOrWhiteSpace(map.FormKey))
                return ConvertToType(GetDefaultValue(json, map, mapping), map);


            var token = json.SelectToken(map.FormKey);
            if (token != null && !string.IsNullOrEmpty(token.ToString()))
            {
                string val = token.ToString();
                if (!string.IsNullOrEmpty(map.AggregatePrefix))
                    val = map.AggregatePrefix + val;

                return ConvertToType(val, map);
            }
            else
            {
                string defaultVal = GetDefaultValue(json, map, mapping);
                return ConvertToType(defaultVal, map);
            }
        }

        /// <summary>
        /// Cerca un FieldMapping in base a FormKey (anziché ObjectProperty) e restituisce il valore dal JSON.
        /// </summary>
        public static object GetMapName(JToken json, List<FieldMapping> mapping, string name)
        {
            var map = mapping.FirstOrDefault(m => m.FormKey == name);
            if (map == null)
                return string.Empty;

            var token = json.SelectToken(map.FormKey);
            if (token != null && !string.IsNullOrEmpty(token.ToString()))
            {
                string val = token.ToString();
                if (!string.IsNullOrEmpty(map.AggregatePrefix))
                    val = map.AggregatePrefix + val;

                return ConvertToType(val, map);
            }
            else
            {
                string defaultVal = GetDefaultValue(json, map, mapping);
                return ConvertToType(defaultVal, map);
            }
        }

        /// <summary>
        /// Gestisce la logica di default (variabili $... e quant'altro) 
        /// </summary>
        public static string GetDefaultValue(JToken json, FieldMapping map, List<FieldMapping> mapping)
        {
            // Se non abbiamo default e non c'è nulla, restituiamo string vuota
            if (string.IsNullOrEmpty(map.DefaultValue))
                return string.Empty;

            string result = map.DefaultValue;
            // Rimpiazzo di eventuali token "$qualcosa"
            var matches = Regex.Matches(map.DefaultValue, @"\$(\S+)");
            if (matches.Count > 0)
                log.Debug($"Analizzo {map.DefaultValue}. Trovate {matches.Count} variabili da sostituire");

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string replacement = GetMapName(json, mapping, key).ToString() ?? "";
                string tempValue = result.Replace(match.Value, replacement);
                log.Debug($"Valore originale: {result}. Trovata variabile {match.Value}. Sostituisco con {replacement}. Nuovo valore {tempValue}");
                result = tempValue;
            }

            return result;
        }

        // Variante semplificata
        public static string GetDefaultValue(JToken json, string defaultValue, FieldMapping map)
        {
            if (string.IsNullOrEmpty(defaultValue))
                return defaultValue;

            var matches = Regex.Matches(defaultValue, @"\$(\S+)");
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string replacement = GetMapName(json, new List<FieldMapping> { map }, key).ToString() ?? "";
                defaultValue = defaultValue.Replace(match.Value, replacement);
            }
            return defaultValue;
        }


        public static string ApplyStringFormat(string input, string format)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(format))
                return input;

            switch (format.ToLower())
            {
                case "lower":
                    return input.ToLower();
                case "upper":
                    return input.ToUpper();
                case "capital":
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
                case "pascal":
                    return string.Join("", input
                        .Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLower()));
                default:
                    return input.ToUpper() ; // Se non è un formato valido, restituisci l'originale
            }
        }
    }
}

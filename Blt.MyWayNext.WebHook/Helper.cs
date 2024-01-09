using Blt.MyWayNext.Business;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Blt.MyWayNext.WebHook.Bol;
using Blt.MyWayNext.Authentication;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Blt.MyWayNext.WebHook.Tools
{
    public static class Helper
    {
        public static void MapFormToObject(NameValueCollection form, object objectToMap, List<FieldMapping> mappings)
        {
            if (objectToMap == null) throw new ArgumentNullException(nameof(objectToMap));

            foreach (var mapping in mappings.Where(m=>!m.Aggregate))
            {
                
                if (form.AllKeys.Contains(mapping.FormKey))
                {
                    var propertyPath = mapping.ObjectProperty.Split('.');
                    SetProperty(objectToMap, propertyPath, GetValue(form, mapping), mapping.DataType);
                }
                else
                {
                    var propertyPath = mapping.ObjectProperty.Split('.');
                    SetProperty(objectToMap, propertyPath, GetValue(form, mapping), mapping.DataType);
                }
            }
            //parte da sistemare
            foreach (var group in mappings.Where(m => m.Aggregate).GroupBy(m => m.ObjectProperty))
            {
                var aggregatedParts = new List<string>();

                foreach (var mapping in group)
                {
                    if (form.AllKeys.Contains(mapping.FormKey) || !string.IsNullOrEmpty(mapping.DefaultValue))
                    {
                        string value = GetValue(form, mapping);
                        if (!string.IsNullOrEmpty(value)) 
                        {
                            string separator = ConvertEscapeSequences(mapping.AggregateSeparator);
                            string prefixedValue = mapping.AggregatePrefix + value;
                            aggregatedParts.Add(prefixedValue + separator);
                        }
                    }
                }

                string aggregatedValue = string.Join("", aggregatedParts).TrimEnd();

                if (!string.IsNullOrEmpty(aggregatedValue))
                {
                    var propertyPath = group.Key.Split('.');
                    SetProperty(objectToMap, propertyPath, aggregatedValue, group.First().DataType);
                }
            }
        }

        public static void SetProperty(object obj, string[] propertyPath, string value, string dataType)
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

                SetProperty(subObj, propertyPath.Skip(1).ToArray(), value, dataType);
            }
            else
            {
                object convertedValue = ConvertToType(value, dataType);
                propertyInfo.SetValue(obj, convertedValue);
            }
        }

        public static string GetValue(NameValueCollection form, FieldMapping mapping)
        {
            string value;
            if (form.AllKeys.Contains(mapping.FormKey) && !string.IsNullOrEmpty(form[mapping.FormKey]))
            {
                value = form[mapping.FormKey];
            }
            else
            {
                value = GetDefaultValue(form, mapping.DefaultValue);
            }
            return value;
        }

        public static string GetDefaultValue(NameValueCollection form, string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue))
                return defaultValue;

            var matches = Regex.Matches(defaultValue, @"\$(\w+)");
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string replacement = form[key] ?? "";
                defaultValue = defaultValue.Replace(match.Value, replacement);
            }

            return defaultValue;
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
        public static object ConvertToType(string value, string dataType)
        {
            // Gestione dei casi comuni (tipi primitivi, stringhe, ecc.)
            switch (dataType.ToLower())
            {
                case "int":
                case "system.int32":
                    return int.TryParse(value, out int intValue) ? intValue : default(int);
                case "long":
                case "system.int64":
                    return long.TryParse(value, out long longValue) ? longValue : default(long);
                case "bool":
                case "system.boolean":
                    return bool.TryParse(value, out bool boolValue) ? boolValue : default(bool);
                case "double":
                case "system.double":
                    return double.TryParse(value, out double doubleValue) ? doubleValue : default(double);
                case "string":
                case "system.string":
                    return value;
                // Aggiungi qui altri tipi se necessario
                default:
                    // Per tipi non gestiti direttamente, prova a usare il metodo ChangeType
                    var type = Type.GetType(dataType);
                    if (type == null)
                        throw new InvalidOperationException($"Tipo non riconosciuto: {dataType}");

                    try
                    {
                        return Convert.ChangeType(value, type);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Impossibile convertire il valore '{value}' in tipo '{dataType}'", ex);
                    }
            }
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

                if (encoding.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    content = new FormUrlEncodedContent(data);
                }
                else if (encoding.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonData = JsonConvert.SerializeObject(data);
                    content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                }
                else
                {
                    throw new ArgumentException("Unsupported encoding type", nameof(encoding));
                }

                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

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
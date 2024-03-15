using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Blt.MyWayNext.Proxy.Authentication;
using Blt.MyWayNext.Proxy.Business;
using Newtonsoft.Json;

namespace Blt.MyWayNext.Bol
{
    public class Mapping
    {
        public string name { get; set; }
        public string type { get; set; }

        public static List<Mapping> LoadFromXml(string filePath)
        {
            var mappings = new List<Mapping>();
            var doc = XDocument.Load(filePath);
            foreach (var mapping in doc.Descendants("Mapping"))
            {
                mappings.Add(new Mapping
                {
                    name = mapping.Attribute("name").Value,
                    type = mapping.Attribute("type").Value
                });
            }

            return mappings;
        }
    }
    public class FieldMapping
    {
        public string FormKey { get; set; }
        public string ObjectProperty { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string ObjectType { get; set; }

        public Boolean Aggregate { get; set; }
        public string AggregateSeparator { get; set; }
        public string AggregatePrefix { get; set; }

        // Carica le mappature da un file XML
        public static List<FieldMapping> LoadFromXml(string filePath, string mappingName)
        {
            var mappings = new List<FieldMapping>();
            var doc = XDocument.Load(filePath);
            //recupero il nodo xml "mapping" con l'attributo "name" uguale a quello passato come parametro mappingName
            var root = doc.Descendants("Mapping").Where(x => x.Attribute("name").Value == mappingName).FirstOrDefault();


            foreach (var field in root.Descendants("Field"))
            {
                mappings.Add(new FieldMapping
                {
                    FormKey = field.Attribute("name").Value,
                    ObjectProperty = field.Attribute("property").Value,
                    DefaultValue = field.Attribute("default")?.Value,
                    DataType = field.Attribute("type").Value,
                    ObjectType = field.Attribute("object")?.Value,
                    //se aggregate è true allora imposto a valore booleano, se l'attributo non è definito allo stesso modo imposto a false
                    Aggregate = field.Attribute("aggregate")?.Value == "true" ? true : false,
                    AggregateSeparator = field.Attribute("separator")?.Value,
                    AggregatePrefix = field.Attribute("prefix")?.Value

                });
            }

            return mappings;
        }
        public static List<FieldMapping> LoadFromXml(string filePath, string mappingName, string objectType)
        {
            var mappings = new List<FieldMapping>();
            var doc = XDocument.Load(filePath);
            //recupero il nodo xml "mapping" con l'attributo "name" uguale a quello passato come parametro mappingName
            var root = doc.Descendants("Mapping").Where(x => x.Attribute("name").Value == mappingName).FirstOrDefault();


            foreach (var field in root.Descendants("Field").Where(x => x.Attribute("object").Value.Contains(objectType)))
            {
                mappings.Add(new FieldMapping
                {
                    FormKey = field.Attribute("name").Value,
                    ObjectProperty = field.Attribute("property").Value,
                    DefaultValue = field.Attribute("default")?.Value,
                    DataType = field.Attribute("type").Value,
                    ObjectType = field.Attribute("object")?.Value,
                    //se aggregate è true allora imposto a valore booleano, se l'attributo non è definito allo stesso modo imposto a false
                    Aggregate = field.Attribute("aggregate")?.Value == "true" ? true : false,
                    AggregateSeparator = field.Attribute("separator")?.Value,
                    AggregatePrefix = field.Attribute("prefix")?.Value
                });
            }

            return mappings;
        }
    }

    public enum WebhookTypeEnum
    {
        AnagraficaTemporanea = 1,
        AnagraficaTemporaneaIniziativa = 2,
        IniziativaCommerciale = 3,
        AttivitaCommerciale = 4,
        AggiornaAttivitaCommerciale = 5,
        Disponibilita = 6,
        AnagraficaIbrida = 7,
        AnagraficaIbridaIniziativa = 8,
        Comunicazione = 9
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
        public Proxy.Business.Client crmClient { get; set; }
    }

    public class MyWayAnagraficheResponse : MyWayApiResponse
    {
        public List<AnagraficaIbridaView> Anagrafiche { get; set; }
        public MyWayAnagraficheResponse()
        {
            Anagrafiche = new List<AnagraficaIbridaView>();
        }
    }

    public class MyWayIniziativaResponse : MyWayApiResponse
    {
        public List<IniziativaView> IniziativeCommerciale { get; set; }
        public MyWayIniziativaResponse()
        {
            IniziativeCommerciale = new List<IniziativaView>();
        }
    }

    public class MyWayTrattativaResponse : MyWayApiResponse
    {
        public List<MyWayObjTrattativa> OpportunitaCommerciale { get; set; }
        public MyWayTrattativaResponse()
        {
            OpportunitaCommerciale = new List<MyWayObjTrattativa>();
        }
    }

    public class MyWayObjTrattativa
    {
        public string IniziativaCod { get; set; }
        public string AnagraficaCod { get; set; }
        public string AgenteCod { get; set; }
        public string TrattativaCod { get; set; }
        public string TrattativaMasterCod { get; set; }
        public Decimal? Valore { get; set; }
        public DateTime? DataPrevista { get; set; }
        public Boolean  Accessoria {  get; set; }
        public int PercentualeChiusura { get; set; }
        public string Nome { get; set; }

        public TrattativaDto UpdateTrattativa(TrattativaDto trattativa)
        {
            trattativa.DataPrevista = DataPrevista;
            trattativa.Valore = Convert.ToDouble(Valore);
            trattativa.Nome = Nome;
            trattativa.PercentualeChiusura = PercentualeChiusura;

            return trattativa;
        }
        public MyWayObjTrattativa() { }
        
        public MyWayObjTrattativa( TrattativaDto trattativa)

        {
            MyWayObjTrattativa tr = new MyWayObjTrattativa();
            tr.TrattativaCod = trattativa.Codice;
            tr.IniziativaCod = trattativa.IniziativaAssociata.Codice;
            tr.AnagraficaCod = trattativa.Anagrafica.Codice;
            tr.Accessoria = trattativa.Accessoria.Value;
            tr.Valore = Convert.ToDecimal(trattativa.Valore ?? 0);
            tr.TrattativaMasterCod = trattativa.TrattativaAccessoria.Codice;
            tr.DataPrevista = trattativa.DataPrevista.Value.DateTime;
            tr.AgenteCod = trattativa.Agente.Codice;
            tr.PercentualeChiusura = trattativa.PercentualeChiusura;
            tr.Nome = trattativa.Nome;
        }
        public MyWayObjTrattativa(TrattativaView trattativa)

        {
            MyWayObjTrattativa tr = new MyWayObjTrattativa();
            tr.TrattativaCod = trattativa.Codice;          
            tr.AnagraficaCod = trattativa.Anagrafica;
            tr.Accessoria = trattativa.IsAccessoria;
            tr.Valore = Convert.ToDecimal(trattativa.Valore ?? 0);           
            tr.DataPrevista = trattativa.DataPrevista.Value.Date;
            tr.AgenteCod = trattativa.Agente;
            tr.PercentualeChiusura = trattativa.PercentualeChiusura;
            tr.Nome = trattativa.Nome;
        }

    }

    public class MetaWebhookEvent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("externalId")]
        public string ExternalId { get; set; }

        [JsonProperty("schemaId")]
        public string SchemaId { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("fields")]
        public List<Field> Fields { get; set; }

        [JsonProperty("relationships")]
        public Relationships Relationships { get; set; }

        [JsonProperty("createdTimestamp")]
        public DateTime CreatedTimestamp { get; set; }

        [JsonProperty("updatedTimestamp")]
        public DateTime UpdatedTimestamp { get; set; }
        [JsonProperty("contactFields")]
        public Dictionary<string, string> ContactFields
        {
            get
            {
                var contactFields = new Dictionary<string, string>();
                foreach (var field in this.Fields)
                {
                    if (field.Id == "fields" && !string.IsNullOrWhiteSpace(field.Value))
                    {

                        // Supponendo che i valori siano separati da virgola e le coppie chiave-valore da "="
                        var entries = field.Value.Split(',');
                        foreach (var entry in entries)
                        {
                            var parts = entry.Split('=');
                            if (parts.Length == 2)
                            {
                                contactFields[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                }
                return contactFields;
            }

        }
    }

    public class Field
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
    }

    public class Relationships
    {
        [JsonProperty("primary-contact")]
        public List<string> PrimaryContact { get; set; }
    }

}
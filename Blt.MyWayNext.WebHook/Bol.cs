using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Blt.MyWayNext.WebHook.Bol
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


            foreach (var field in root.Descendants("Field").Where(x=>x.Attribute("object").Value.Contains(objectType)))
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
        Comunicazione = 9,
        Contatto = 10
    }
}
using OfficeOpenXml;
using System.Data;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Forms;
using static Blt.ExcelToJson.Form1;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml.FormulaParsing.Excel.Functions;

namespace Blt.ExcelToJson
{
    public partial class Form1 : Form
    {
        ExcelHelper excelHelper;
        public delegate void ProgressUpdateHandler();

        public Form1()
        {
            InitializeComponent();
            excelHelper = new ExcelHelper();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuthentication.Checked)
            {
                txtUsernameK.Enabled = true;
                txtUsernameV.Enabled = true;
                txtPasswordK.Enabled = true;
                txtPasswordV.Enabled = true;
                txtTokenK.Enabled = true;
            }
            else
            {
                txtUsernameK.Enabled = false;
                txtUsernameV.Enabled = false;
                txtPasswordK.Enabled = false;
                txtPasswordV.Enabled = false;
                txtTokenK.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var res = openExcel.ShowDialog();
            if (res == DialogResult.OK)
            {
                txtFile.Text = openExcel.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cmbSheets.Items.Clear();
            var SheetNames = new List<string>();
            SheetNames = excelHelper.GetExcelSheetNames(txtFile.Text);
            foreach (var sheet in SheetNames)
            {
                cmbSheets.Items.Add(sheet);
            }
        }

        private void cmbSheets_SelectedIndexChanged(object sender, EventArgs e)
        {
            var columnNames = excelHelper.GetColumnNamesWithValues(txtFile.Text, cmbSheets.SelectedItem.ToString()); // Usa la funzione modificata precedentemente
            ListColumns.Items.Clear();
            foreach (var columnName in columnNames)
            {
                ListColumns.Items.Add(columnName, false); // Aggiunge il nome della colonna alla lista con un checkbox non selezionato
            }
        }

        public void UpdateProgressBar()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateProgressBar));
            }
            else
            {
                prgImport.Value = Math.Min(prgImport.Value + 1, prgImport.Maximum);
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            var dtDati = excelHelper.ExtractDataToDataTable(txtFile.Text, cmbSheets.SelectedItem.ToString(), ListColumns.CheckedItems.Cast<string>().ToList(), 30);
            prgImport.Maximum = dtDati.Rows.Count;
            await excelHelper.SendDataAsJson(dtDati, txtUrl.Text, UpdateProgressBar);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Configurator configurator = new Configurator();
            
            configurator.grdMapping.AutoGenerateColumns = false;
            PopolaDataGrid(configurator.grdMapping, ListColumns.CheckedItems.Cast<string>().ToList());
            configurator.ShowDialog();
        }

        public void PopolaDataGrid(DataGridView dataGridView, List<string> selectedExcelColumns)
        {
            dataGridView.Columns.Clear();
            dataGridView.AutoGenerateColumns = false;

            var properties = typeof(Field).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsEnum)
                {
                    AddComboBoxColumn(dataGridView, prop.Name, prop.PropertyType);
                }
                else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type enumType = prop.PropertyType.GetGenericArguments()[0];
                    if (enumType.IsEnum)
                    {
                        // Creare tre colonne ComboBox per la selezione multipla
                        for (int i = 1; i <= 3; i++)
                        {
                            AddComboBoxColumn(dataGridView, prop.Name + i, enumType);
                        }
                    }
                }
                else
                {
                    dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = prop.Name, HeaderText = prop.Name });
                }
            }
            int index = 0;
            // Aggiunta delle righe basate sulle colonne Excel selezionate
            foreach (var columnName in selectedExcelColumns)
            {
                
                dataGridView.Rows.Add();
                dataGridView.Rows[index].Cells[0].Value = columnName;
                index++;
            }
        }

        private void AddComboBoxColumn(DataGridView dataGridView, string columnName, Type enumType)
        {
            var comboBoxColumn = new DataGridViewComboBoxColumn
            {
                Name = columnName,
                HeaderText = columnName,
                DataSource = Enum.GetValues(enumType),
                ValueType = enumType
            };
            dataGridView.Columns.Add(comboBoxColumn);
        }

    }

    public class ExcelHelper
    {
        public List<string> GetExcelSheetNames(string filePath)
        {
            var sheetNames = new List<string>();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    sheetNames.Add(worksheet.Name);
                }
            }
            return sheetNames;
        }

        public List<string> GetColumnNamesWithValues(string filePath, string sheetName)
        {
            var columnNames = new List<string>();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    bool hasValue = false;
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Inizia dalla seconda riga
                    {
                        if (worksheet.Cells[row, col].Value != null)
                        {
                            hasValue = true;
                            break;
                        }
                    }

                    if (hasValue)
                    {
                        // Assumendo che la prima riga contenga i nomi delle colonne
                        columnNames.Add((worksheet.Cells[1, col].Value ?? string.Empty).ToString());
                    }
                }
            }
            return columnNames;
        }

        public DataTable ExtractDataToDataTable(string filePath, string sheetName, List<string> columns, int maxEmptyRows)
        {
            var dt = new DataTable();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                foreach (var column in columns)
                {
                    dt.Columns.Add(column);
                }

                int emptyRowCount = 0;
                for (int rowNum = 2; rowNum <= worksheet.Dimension.End.Row; rowNum++)
                {
                    bool isRowEmpty = true;
                    var row = dt.NewRow();
                    foreach (var column in columns)
                    {
                        var cellValue = worksheet.Cells[rowNum, columns.IndexOf(column) + 1].Value;
                        row[column] = cellValue;
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            isRowEmpty = false;
                        }
                    }

                    if (isRowEmpty)
                    {
                        emptyRowCount++;
                        if (emptyRowCount >= maxEmptyRows)
                        {
                            // Elimina le ultime righe vuote
                            for (int i = 0; i < emptyRowCount; i++)
                            {
                                dt.Rows.RemoveAt(dt.Rows.Count - 1);
                            }
                            break;
                        }
                        else
                        {
                            dt.Rows.Add(row);
                        }
                    }
                    else
                    {
                        emptyRowCount = 0;
                        dt.Rows.Add(row);
                    }
                }
            }
            return dt;
        }

        public async Task SendDataAsJson(DataTable dataTable, string url, ProgressUpdateHandler onProgressUpdate)
        {
            using (var client = new HttpClient())
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var rowData = dataTable.Columns.Cast<DataColumn>()
                 .ToDictionary(col => col.ColumnName, col => row[col]);

                    var json = JsonConvert.SerializeObject(rowData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    if(response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        onProgressUpdate.Invoke();
                    }
                }
            }
        }

        public ExcelHelper()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

    }
    public class Field
    {
        public string name { get; set; }
        public string property { get; set; }
        public TypeEnum type { get; set; }
        public List<ObjectEnum> @object { get; set; }
        public bool aggregate { get; set; }
        public string prefix { get; set; }
        public string separator { get; set; }
        public string @default { get; set; }

    }
    public static class Helper
    {
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
        /// <summary>
        /// Funzione che lancia un Webhook verso l'indirizzo passato come parametro che accetta come parametri una stringa che determina la codifica e poi una lista di coppie chiave valore e restituisce un oggetto di tipo ResponseWebhook
        /// </summary>
        /// <param name="webhook"></param>
        /// <param name="tipo"></param>
        /// <param name="Collection"></param>
        /// <returns>ResponseWebhook</returns>

    }

    public enum TypeEnum
    {
        @string,
        @int,
        @bool,
        @double,
        @object,
        @phone
    }
    public enum ObjectEnum
    {
        AttivitaCommerciale,
        CreaIniziativa,
        AnagraficaTemporanea,
        AggiornaIniziativa,
        Contatto
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

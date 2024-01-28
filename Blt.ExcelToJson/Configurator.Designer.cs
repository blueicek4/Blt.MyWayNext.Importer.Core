namespace Blt.ExcelToJson
{
    partial class Configurator
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            grdMapping = new DataGridView();
            btnSaveConfig = new Button();
            textBox2 = new TextBox();
            button1 = new Button();
            comboBox1 = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)grdMapping).BeginInit();
            SuspendLayout();
            // 
            // grdMapping
            // 
            grdMapping.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grdMapping.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grdMapping.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grdMapping.Location = new Point(12, 12);
            grdMapping.Name = "grdMapping";
            grdMapping.RowHeadersWidth = 62;
            grdMapping.Size = new Size(1394, 543);
            grdMapping.TabIndex = 0;
            // 
            // btnSaveConfig
            // 
            btnSaveConfig.Location = new Point(1151, 576);
            btnSaveConfig.Name = "btnSaveConfig";
            btnSaveConfig.Size = new Size(255, 34);
            btnSaveConfig.TabIndex = 1;
            btnSaveConfig.Text = "Salva Configurazione";
            btnSaveConfig.UseVisualStyleBackColor = true;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(427, 579);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(528, 31);
            textBox2.TabIndex = 3;
            // 
            // button1
            // 
            button1.Location = new Point(276, 576);
            button1.Name = "button1";
            button1.Size = new Size(145, 34);
            button1.TabIndex = 4;
            button1.Text = "Genera Guid";
            button1.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(12, 576);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(229, 33);
            comboBox1.TabIndex = 5;
            // 
            // Configurator
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1418, 622);
            Controls.Add(comboBox1);
            Controls.Add(button1);
            Controls.Add(textBox2);
            Controls.Add(btnSaveConfig);
            Controls.Add(grdMapping);
            Name = "Configurator";
            Text = "Configurator";
            ((System.ComponentModel.ISupportInitialize)grdMapping).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public DataGridView grdMapping;
        private Button btnSaveConfig;
        private TextBox textBox2;
        private Button button1;
        private ComboBox comboBox1;
    }
}
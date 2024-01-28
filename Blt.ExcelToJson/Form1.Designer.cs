namespace Blt.ExcelToJson
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtFile = new TextBox();
            button1 = new Button();
            button2 = new Button();
            ListColumns = new CheckedListBox();
            txtUrl = new TextBox();
            cmbSheets = new ComboBox();
            prgImport = new ProgressBar();
            txtChar = new TextBox();
            txtUsernameV = new TextBox();
            txtPasswordV = new TextBox();
            txtTokenV = new TextBox();
            txtUsernameK = new TextBox();
            txtPasswordK = new TextBox();
            txtTokenK = new TextBox();
            btnStart = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            chkAuthentication = new CheckBox();
            openExcel = new OpenFileDialog();
            button3 = new Button();
            SuspendLayout();
            // 
            // txtFile
            // 
            txtFile.Location = new Point(130, 14);
            txtFile.Name = "txtFile";
            txtFile.Size = new Size(405, 31);
            txtFile.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 1;
            button1.Text = "Apri";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(598, 12);
            button2.Name = "button2";
            button2.Size = new Size(190, 34);
            button2.TabIndex = 2;
            button2.Text = "Leggi File";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // ListColumns
            // 
            ListColumns.FormattingEnabled = true;
            ListColumns.Location = new Point(12, 139);
            ListColumns.Name = "ListColumns";
            ListColumns.ScrollAlwaysVisible = true;
            ListColumns.Size = new Size(281, 340);
            ListColumns.TabIndex = 3;
            // 
            // txtUrl
            // 
            txtUrl.Location = new Point(345, 381);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(443, 31);
            txtUrl.TabIndex = 4;
            // 
            // cmbSheets
            // 
            cmbSheets.FormattingEnabled = true;
            cmbSheets.Location = new Point(12, 86);
            cmbSheets.Name = "cmbSheets";
            cmbSheets.Size = new Size(281, 33);
            cmbSheets.TabIndex = 5;
            cmbSheets.SelectedIndexChanged += cmbSheets_SelectedIndexChanged;
            // 
            // prgImport
            // 
            prgImport.Location = new Point(12, 499);
            prgImport.Name = "prgImport";
            prgImport.Size = new Size(776, 34);
            prgImport.TabIndex = 6;
            // 
            // txtChar
            // 
            txtChar.Location = new Point(598, 86);
            txtChar.MaxLength = 1;
            txtChar.Name = "txtChar";
            txtChar.Size = new Size(190, 31);
            txtChar.TabIndex = 7;
            // 
            // txtUsernameV
            // 
            txtUsernameV.Location = new Point(345, 204);
            txtUsernameV.MaxLength = 1;
            txtUsernameV.Name = "txtUsernameV";
            txtUsernameV.Size = new Size(190, 31);
            txtUsernameV.TabIndex = 8;
            // 
            // txtPasswordV
            // 
            txtPasswordV.Location = new Point(598, 204);
            txtPasswordV.MaxLength = 1;
            txtPasswordV.Name = "txtPasswordV";
            txtPasswordV.Size = new Size(190, 31);
            txtPasswordV.TabIndex = 9;
            // 
            // txtTokenV
            // 
            txtTokenV.Location = new Point(345, 322);
            txtTokenV.MaxLength = 1;
            txtTokenV.Name = "txtTokenV";
            txtTokenV.Size = new Size(443, 31);
            txtTokenV.TabIndex = 10;
            // 
            // txtUsernameK
            // 
            txtUsernameK.Location = new Point(345, 145);
            txtUsernameK.MaxLength = 1;
            txtUsernameK.Name = "txtUsernameK";
            txtUsernameK.Size = new Size(190, 31);
            txtUsernameK.TabIndex = 11;
            // 
            // txtPasswordK
            // 
            txtPasswordK.Location = new Point(598, 145);
            txtPasswordK.MaxLength = 1;
            txtPasswordK.Name = "txtPasswordK";
            txtPasswordK.Size = new Size(190, 31);
            txtPasswordK.TabIndex = 12;
            // 
            // txtTokenK
            // 
            txtTokenK.Location = new Point(345, 263);
            txtTokenK.Name = "txtTokenK";
            txtTokenK.Size = new Size(190, 31);
            txtTokenK.TabIndex = 13;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(598, 440);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(190, 34);
            btnStart.TabIndex = 14;
            btnStart.Text = "Avvia Caricamento";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(598, 58);
            label1.Name = "label1";
            label1.Size = new Size(129, 25);
            label1.TabIndex = 15;
            label1.Text = "Carattere Testo";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(345, 120);
            label2.Name = "label2";
            label2.Size = new Size(124, 25);
            label2.TabIndex = 16;
            label2.Text = "Username Key";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(598, 120);
            label3.Name = "label3";
            label3.Size = new Size(120, 25);
            label3.TabIndex = 17;
            label3.Text = "Password Key";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(345, 179);
            label4.Name = "label4";
            label4.Size = new Size(138, 25);
            label4.TabIndex = 18;
            label4.Text = "Username Value";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(598, 179);
            label5.Name = "label5";
            label5.Size = new Size(134, 25);
            label5.TabIndex = 19;
            label5.Text = "Password Value";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(345, 238);
            label6.Name = "label6";
            label6.Size = new Size(91, 25);
            label6.TabIndex = 20;
            label6.Text = "Token Key";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(345, 294);
            label7.Name = "label7";
            label7.Size = new Size(105, 25);
            label7.TabIndex = 21;
            label7.Text = "Token Value";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(345, 353);
            label8.Name = "label8";
            label8.Size = new Size(34, 25);
            label8.TabIndex = 22;
            label8.Text = "Url";
            // 
            // chkAuthentication
            // 
            chkAuthentication.AutoSize = true;
            chkAuthentication.Location = new Point(345, 88);
            chkAuthentication.Name = "chkAuthentication";
            chkAuthentication.Size = new Size(154, 29);
            chkAuthentication.TabIndex = 23;
            chkAuthentication.Text = "Autenticazione";
            chkAuthentication.UseVisualStyleBackColor = true;
            chkAuthentication.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // openExcel
            // 
            openExcel.Filter = "\"Excel|*.xlsx|Tutti i File|*.*\"";
            // 
            // button3
            // 
            button3.Location = new Point(345, 440);
            button3.Name = "button3";
            button3.Size = new Size(190, 34);
            button3.TabIndex = 24;
            button3.Text = "Crea Configurazione";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 545);
            Controls.Add(button3);
            Controls.Add(chkAuthentication);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnStart);
            Controls.Add(txtTokenK);
            Controls.Add(txtPasswordK);
            Controls.Add(txtUsernameK);
            Controls.Add(txtTokenV);
            Controls.Add(txtPasswordV);
            Controls.Add(txtUsernameV);
            Controls.Add(txtChar);
            Controls.Add(prgImport);
            Controls.Add(cmbSheets);
            Controls.Add(txtUrl);
            Controls.Add(ListColumns);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(txtFile);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFile;
        private Button button1;
        private Button button2;
        private CheckedListBox ListColumns;
        private TextBox txtUrl;
        private ComboBox cmbSheets;
        private ProgressBar prgImport;
        private TextBox txtChar;
        private TextBox txtUsernameV;
        private TextBox txtPasswordV;
        private TextBox txtTokenV;
        private TextBox txtUsernameK;
        private TextBox txtPasswordK;
        private TextBox txtTokenK;
        private Button btnStart;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private CheckBox chkAuthentication;
        private OpenFileDialog openExcel;
        private Button button3;
    }
}

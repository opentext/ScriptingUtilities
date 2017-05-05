namespace DOKuStar.Runtime.HotFolders.Connectors.LookupListUpdaterConnectors
{
    partial class LookupListUpdaterConfigurationControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.tables_comboBox = new System.Windows.Forms.ComboBox();
            this.btn_loadProject = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_profileName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 5);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Table definition:";
            // 
            // tables_comboBox
            // 
            this.tables_comboBox.FormattingEnabled = true;
            this.tables_comboBox.Location = new System.Drawing.Point(131, 4);
            this.tables_comboBox.Name = "tables_comboBox";
            this.tables_comboBox.Size = new System.Drawing.Size(256, 24);
            this.tables_comboBox.TabIndex = 1;
            // 
            // btn_loadProject
            // 
            this.btn_loadProject.Location = new System.Drawing.Point(403, 4);
            this.btn_loadProject.Name = "btn_loadProject";
            this.btn_loadProject.Size = new System.Drawing.Size(113, 23);
            this.btn_loadProject.TabIndex = 2;
            this.btn_loadProject.Text = "Load project...";
            this.btn_loadProject.UseVisualStyleBackColor = true;
            this.btn_loadProject.Click += new System.EventHandler(this.btn_loadProject_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Profilename";
            // 
            // txt_profileName
            // 
            this.txt_profileName.Location = new System.Drawing.Point(131, 48);
            this.txt_profileName.Name = "txt_profileName";
            this.txt_profileName.Size = new System.Drawing.Size(256, 22);
            this.txt_profileName.TabIndex = 4;
            // 
            // LookupListUpdaterConfigurationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txt_profileName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_loadProject);
            this.Controls.Add(this.tables_comboBox);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "LookupListUpdaterConfigurationControl";
            this.Size = new System.Drawing.Size(519, 185);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox tables_comboBox;
        private System.Windows.Forms.Button btn_loadProject;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_profileName;
    }
}

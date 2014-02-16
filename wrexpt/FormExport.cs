using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;


namespace wrexpt
{
    public partial class FormExport : Form
    {
        public FormExport()
        {
            InitializeComponent();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            try
            {
                String wrDbPath = this.textBoxWRDb.Text;
                String wrXmlPath = this.textBoxExport.Text;

                if (!File.Exists(wrDbPath))
                {
                    throw new Exception("Invalid file: " + wrDbPath);
                }

                Program.ExportDB(wrDbPath, wrXmlPath, this.textBoxPassword.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, "Export complete", "Kamino", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter     = "WebReplay Database (*.sdf)|*.sdf|All files (*.*)|*.*";
            ofd.DefaultExt = "sdf";

            String wrDbDir = Program.GetDBPath();
            if (wrDbDir != null)
            {
                ofd.InitialDirectory = wrDbDir;
            }

            DialogResult answer = ofd.ShowDialog(this);
            if (answer == DialogResult.OK)
            {
                this.textBoxWRDb.Text = ofd.FileName;
            }
        }

        private void FormExport_Load(object sender, EventArgs e)
        {
            String srDbDir  = Program.GetDBPath();
            String wrDbFile = (srDbDir != null ? Path.Combine(srDbDir, "WR.sdf") : null);
            if (wrDbFile != null)
            {
                this.textBoxWRDb.Text = wrDbFile;
            }

            String xmlFile = GetExportPath();
            if (xmlFile != null)
            {
                this.textBoxExport.Text = xmlFile;
            }
        }

        private string GetExportPath()
        {
            try
            {
                String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                return Path.Combine(desktopPath, "WR.xml");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void checkBoxMask_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkBoxMask.Checked)
            {
                this.textBoxPassword.PasswordChar = '*';
            }
            else
            {
                this.textBoxPassword.PasswordChar = '\0';
            }
        }
    }
}

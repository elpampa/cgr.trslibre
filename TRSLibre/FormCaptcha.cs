using System;
using System.Windows.Forms;
using WatiN.Core;
using Form = System.Windows.Forms.Form;

namespace TRSLibre
{
    public partial class FormCaptcha : Form
    {
        public string Captcha { get; set; }
        private IE _ie;
        public Form1 form1;

        public FormCaptcha(IE ie)
        {
            InitializeComponent();
            _ie = ie;
            Captcha = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(txtCaptcha.Text.Trim() == "")
            {
                MessageBox.Show("Debe ingresar manualmente el valor del captcha.");
                return;
            }

            Captcha = txtCaptcha.Text;
            Close();
        }

        private void FormCaptcha_Load(object sender, EventArgs e)
        {
            txtCaptcha.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form1.StopNavigation = true;
            Captcha = "-1-1-1-";
            Close();
        }
    }
}

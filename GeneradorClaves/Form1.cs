using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneradorClaves
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA1.Create();  //or use SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
               
                if (txtOperador.Text.Trim() == "" || txtOperador.Text.Trim() == "")
                {
                    MessageBox.Show("Los campos operador y fechas son requeridos");
                    return;
                }

                var hashedKey = string.Empty;

                var fechas = txtFechas.Text.Split(',');

                foreach(var fecha in fechas){
                    hashedKey += GetHashString(fecha + txtOperador.Text);
                }

                txtResultado.Text = hashedKey;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ha ocurrido el siguiente error:" + ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.XPath;
using WatiN.Core;
using Form = System.Windows.Forms.Form;

namespace TRSLibre
{
      
    public partial class Form1 : Form
    {
        private IE _ie;
        private const string RootUrl = "http://www.srt.gob.ar/index.php/historial-de-contratos-3/";
        public static bool StopNavigation = false;
        private string _pathToFile = String.Empty;
        private string _fileName = String.Empty;
        private int P;
        private int NP;
        private readonly string _operador = "DEFAULT";

        public static string Hash(string password, string salt)
        {
            var bytes = Encoding.Unicode.GetBytes(password);
            var src = Convert.FromBase64String(salt);
            var dst = new byte[src.Length + bytes.Length];
            Buffer.BlockCopy(src, 0, dst, 0, src.Length);
            Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);
            var algorithm = HashAlgorithm.Create("SHA1");
            var inArray = algorithm.ComputeHash(dst);
            return Convert.ToBase64String(inArray);
        }

        public Form1()
        {
            InitializeComponent();
            _operador = (ConfigurationManager.AppSettings["Operador"] == null) ? _operador : _operador = ConfigurationManager.AppSettings["Operador"];
            StartPosition = FormStartPosition.Manual;
            foreach (var scrn in Screen.AllScreens)
            {
                if (scrn.Bounds.Contains(this.Location))
                {
                    Location = new Point(scrn.Bounds.Right - this.Width, scrn.Bounds.Top);
                    return;
                }
            }
        }


        private string OpenCaptcha()
        {
            var formCaptcha = new FormCaptcha(_ie);
            formCaptcha.ShowDialog();
            return formCaptcha.Captcha;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!ValidApplication())
            {
                MessageBox.Show("La versión de esta aplicación ha vencido. Por favor consulte con el administrador");
                Environment.Exit(0);
            }

            var openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName = openFileDialog1.SafeFileName;
                lblFileName.Text = "Archivo: " + openFileDialog1.FileName;
                _pathToFile = openFileDialog1.FileName; // @"D:\cuits.pase1.txt";

                var arrLines = File.ReadAllLines(_pathToFile);
                var lineNumber = 0;
                lblNP.Text = "0";
                lblP.Text = "0";
                P = 0;
                NP = 0;

                foreach (var line in arrLines)
                {
                    var data = arrLines[lineNumber].Split('\t');

                    // No procesado
                    if (data.Count() == 1)
                    {
                        lblNP.Text = (Convert.ToInt32(lblNP.Text) + 1).ToString();
                        NP = Convert.ToInt16(lblNP.Text);
                    }
                    else
                    {
                        lblP.Text = (Convert.ToInt32(lblP.Text) + 1).ToString();
                        P = Convert.ToInt16(lblP.Text);
                    }

                    lineNumber++;
                }

                lblP.Text = "Procesados :" + lblP.Text;
                lblNP.Text = "No Procesados :" + lblNP.Text;

            }
        }


        private string GetData(string cuit)
        {
            var link = _ie.Frames[0].Link(Find.ByText("Buscar Contratos"));
            //_ie.GoTo(link.Url);

            _ie.Frames[0].Frames[0].TextField(Find.ById("cuit")).Value = cuit;
           // _ie.TextField(Find.ById("cuit")).Value = cuit;
            var captchaValue = OpenCaptcha();

            if (captchaValue == "-1-1-1-")
            {
                return "-1";
            }

            _ie.Frames[0].Frames[0].TextField(Find.ById("captchanet_response_field")).Value = captchaValue;

            var btnFind = _ie.Frames[0].Frames[0].Button(Find.ByClass("BotonVerde"));
            if (btnFind.Exists)
                btnFind.Click();

            // SACAR DATOS
            try
            {
                if (_ie.Frames[0].Frames[0].Divs[0].Text.StartsWith("Error"))
                {
                    _ie.GoTo(RootUrl);
                    return GetData(cuit);
                }
            }
            catch
            {
                if (_ie.Frames[0].Frames[0].Divs[1].Text.StartsWith("Error"))
                {
                    _ie.GoTo(RootUrl);
                    return GetData(cuit);
                }
            }

            var tableResult = _ie.Frames[0].Frames[0].Table(Find.ByClass("resultados"));

            var count = 0;
            var dataRow = string.Empty;
            // CUIT, DENOMINACION, CIIU principal, CIIU secundario 1,CIIU secundario  2
            // 5
            if (tableResult.TableRows.Count > 0){
                foreach (var tableRow in tableResult.TableRows)
                {
                    dataRow += "\t" + tableRow.Text.Split(':')[1].Replace("\r\n", string.Empty); ;
                    count++;
                }
                if (count < 5)
                {
                    while (5 - count != 0)
                    {
                        dataRow += "\t";
                        count += 1;
                    }
                }
            }
            else{
                dataRow += "\t\t\t\t\t";
            }

            var tables = _ie.Frames[0].Frames[0].Tables;

            // ULTIMA ASEGURADORA
            Table tableArt = null;
            // corte de control para útlima aseguradora
            foreach (var table in tables)
            {
                if (table.Text.Contains("Aseguradora"))
                {
                    tableArt = table;
                }
            }
            // EXISTE ULTIMA ASEGURADORA
            if (tableArt != null)
            {
                foreach (var tableRow in tableArt.TableRows)
                {
                    // partir el dato - en 2 campos
                    if (tableRow.Text.Contains("Desde:") || tableRow.Text.Contains("Hasta:"))
                    {
                        try
                        {
                            dataRow += "\t" + (tableRow.Text.Split(':')[1]).Split('-')[0].Replace("\r\n", string.Empty) +
                               "\t" + (tableRow.Text.Split(':')[1]).Split('-')[1];
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            dataRow += "\t" + (tableRow.Text.Split(':')[1]).Split('-')[0].Replace("\r\n", string.Empty) + "\t";
                        }
                    }
                    else
                    {
                        dataRow += "\t" + tableRow.Text.Split(':')[1].Replace("\r\n", string.Empty);
                    }
                }
            }
            else
            {
                // Completar con tabs por cada campo.
                dataRow += "\t\t\t\t\t\t";
            }

            // Extinción
            if (tables[tables.Count - 1].Text.Contains("Extinción"))
            {
                var tablaExtinción = tables[tables.Count - 1];

                foreach (var tableRow in tablaExtinción.TableRows)
                {
                    // partir el dato - en 2 campos
                    if (tableRow.Text.Contains("Regularización:"))
                    {
                        dataRow += "\t" + (tableRow.Text.Split(':')[1]).Split('-')[0].Replace("\r\n", string.Empty) +
                           "\t" + (tableRow.Text.Split(':')[1]).Split('-')[1].Replace("\r\n", string.Empty);
                    }
                    else
                    {
                        dataRow += "\t" + tableRow.Text.Split(':')[1].Replace("\r\n", string.Empty);
                    }
                }
            }
            else
            {
                if (tableResult.NextSibling.NextSibling.NextSibling.NextSibling.InnerHtml.Contains("se encuentra autoasegurado"))
                {
                    dataRow += "\t\tse encuentra autoasegurado\t";
                }
                else {
                    dataRow += "\t\t\t";
                }
            }

            _ie.GoTo(RootUrl);

            System.Threading.Thread.Sleep(2000);

            return dataRow + "\t" + _operador + "\t" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\t" + _fileName;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (_ie != null)
                _ie.Close();
            Environment.Exit(0);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopNavigation = true;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if(!ValidApplication())
            {
                MessageBox.Show("La versión de esta aplicación ha vencido. Por favor consulte con el administrador");
                Environment.Exit(0);
            }

            if (String.IsNullOrEmpty(_pathToFile))
            {
                MessageBox.Show("Debe seleccionar un archivo.");
                btnClose_Click(sender, e);
                return;
            }

            if (NP == 0)
            {
                MessageBox.Show("El archivo no tiene registros para procesar");
                return;
            }

            //_pathToFile = @"D:\cuits.pase1.txt";

            var arrLines = File.ReadAllLines(_pathToFile);
            var lineNumber = 0;

            if (!arrLines.Any())
            {
                return;
            }

            _ie = new IE(RootUrl);

            foreach (var line in arrLines)
            {
                var data = arrLines[lineNumber].Split('\t');

                if (data.Count() == 1)
                {
                    var result = GetData(data[0]);

                    if (result == "-1" || StopNavigation)
                    {
                        _ie.Close();
                        MessageBox.Show("Navegación detenida");
                        return;
                    }

                    // PROCESAR CUIT
                    arrLines[lineNumber] = arrLines[lineNumber] + "\t" + result;
                    File.WriteAllLines(_pathToFile, arrLines);

                    dataGridView1.Rows.Add(result);
                    dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0];

                    NP = NP - 1;
                    P = P + 1;
                    lblP.Text = "P :" + P.ToString();
                    lblNP.Text = "NP :" + NP.ToString();
                }


                lineNumber++;
            }
        }

        private void btnNewIP_Click(object sender, EventArgs e)
        {
            Process.Start("ipconfig", "/renew");
            MessageBox.Show("Se ha renovado exitosamente la conexión.");
        }

       
        private bool ValidApplication()
        {
        /* http://api.geonames.org/timezone?lat=-34.60&lng=-58.38&username=consulgroup*/
            // CLAVE CSX343GSDG
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(" http://api.geonames.org/timezone?lat=-34.60&lng=-58.38&username=consulgroup");

            httpWebRequest.UserAgent = ".NET Framework Test Client";
            httpWebRequest.Accept = "text/html";
            httpWebRequest.Method = "GET"; //this is the default behavior
            // execute the request
            var response = (HttpWebResponse)httpWebRequest.GetResponse();
            // we will read data via the response stream
            var resStream = response.GetResponseStream();

            var date = String.Empty;
            using (var reader = new StreamReader(resStream, Encoding.UTF8))
            {
                var nav = new XPathDocument(reader).CreateNavigator();
                date = nav.SelectSingleNode("//time").InnerXml.Split(' ')[0].Replace("-", "").Substring(0, 6);

             //   date = reader.ReadToEnd().Substring(0, 7).Replace("-", "");
            }
            if (date != DateTime.Now.ToString("yyyyMM"))
            {
                return false;
            }

            var keys = ConfigurationManager.AppSettings["KEY"];

            var today = DateTime.Now.ToString("yyyyMM");
            var operador = ConfigurationManager.AppSettings["Operador"];

            var hashedKey = GetHashString(today + operador);

            // TODO: Validar con web service de hora.
            if (!keys.Contains(hashedKey))
            {
                return false;
            }
            return true;
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
    }
}

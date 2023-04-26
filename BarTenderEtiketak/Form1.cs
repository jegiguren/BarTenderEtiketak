
using Seagull.BarTender.Print;
using Seagull.BarTender.PrintServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BarTenderEtiketak
{
    public partial class Form1 : Form
    {
        string directoryPath = @"C:\bt\XML";//ERP-ak xml-ak uzten dituen karpeta
        private AutoResetEvent fileCreatedEvent = new AutoResetEvent(false);
        string xmlFilePath;//ERP-ak sortzen duen xml-aren ruta osoa (karpeta+fitxategi izena)
        string xmlizena;
        string fileName;
        XmlDocument xmlDoc, xmlWebService, xmlosoa, xml;
        FileSystemWatcher watcher = null;
        private Thread begiraleThread;
        private bool begira = false;
        List<string> fileNames = new List<string>();
        string intermec = "Intermec PM43c_406_BACKUP";
        string pdf = "Microsoft Print to Pdf";
        string konica = "KONICA MINOLTA Admin";
        string zebra = "ZDesigner GK420d";
        Queue<XmlDocument> xmlKola = new Queue<XmlDocument>();
        string etiketaFormatoa;
        LabelFormatDocument etiketa, etiketaGarantia, etiketaCode;
        Engine btEngine;
        


        public Form1()
        {
            InitializeComponent();
            btn_Gelditu.Enabled = false;
        }

        private async void Xml_print_Click(object sender, EventArgs e)
        {
            KoloreaAldatu();

            // Inpresio motorearen instantzia sortu
            btEngine = new Engine();

            // Inpresio zerbitzariarekin konektatu
            btEngine.Start();

            //XmlDocument klaseko objetuak sortu
            xmlDoc = new XmlDocument(); //ERP-ak sortuko duen xml-a
            xmlWebService = new XmlDocument(); //Web zerbitzutik jasoko dugun xml-a
            xmlosoa = new XmlDocument(); //aurreko 2 xml-ak juntatuta lortzen dugun xml-a

            //begiralea martxan jarri hari batean
            begiraleThread = new Thread(() => begiratuKarpeta(directoryPath));
            begiraleThread.Start();

            Xml_print.Enabled = false;
            Xml_print.Text = "BEGIRALEA MARTXAN DAGO...";
            btn_Gelditu.Enabled = true;
            begira = true;

        }

        private async Task begiratuKarpeta(string filePath)
        {
            try
            {
                // Crear un objeto FileSystemWatcher
                watcher = new FileSystemWatcher();
                watcher.Path = filePath;

                // Vigilar solo los archivos con extensión .xml
                watcher.Filter = "*.xml";

                // Suscribirse al evento cuando se detecte un cambio en la carpeta
                watcher.Created += OnChanged;

                // Iniciar la vigilancia
                watcher.EnableRaisingEvents = true;

                // Esperar a que se detecte un archivo
                Console.WriteLine("XML karpeta zaintzen...", filePath);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Errorea FileSystemWatcher sortzean: {ex.Message}");
            }


            while (true)
            {
                // Esperar a que se cree un archivo en la carpeta
                fileCreatedEvent.WaitOne();

                // Obtener una copia de los nombres de archivo actuales
                List<string> currentFileNames;
                lock (fileNames)
                {
                    currentFileNames = new List<string>(fileNames);
                    fileNames.Clear();
                }

                foreach (string fileName in currentFileNames)
                {
                    // Construir la ruta completa del archivo
                    xmlFilePath = Path.Combine(filePath, fileName);

                    try
                    {
                        // Cargar el archivo XML
                        XmlDocument xml = new XmlDocument();
                        xml.Load(xmlFilePath);

                        //xml-tik kodigo artikulua atera gero Ws-ari bidaltzeko
                        string codigoArticulo = KodigoaAtera(xml);

                        // Consumir el servicio web con el código del artículo
                        WsReader wsreader = new WsReader();
                        XmlDocument xmlWebService = await wsreader.WsKontsumitu(codigoArticulo);

                        // Unir el XML de ERP y el XML del servicio web
                        XmlDocument xmlosoa = JuntatuXmlak(xmlWebService, xml);
                        Console.WriteLine(xmlosoa.OuterXml);

                        //root nodoa (aurrena) aldagai batean gorde
                        XmlNode rootNode = xmlosoa.DocumentElement;

                        //WS-ko xml-tik etiketa formatoa atera
                        etiketaFormatoa = EtiketaFormatoaAtera(xmlWebService);

                        //etiketa ireki formatoaren arabera
                        etiketa = btEngine.Documents.Open(@"C:\bt\etiketak aldagaiekin\FORM00" + etiketaFormatoa + ".btw");
                        etiketaGarantia = btEngine.Documents.Open(@"C:\bt\etiketak aldagaiekin\FORMGARANTIA.btw");
                        etiketaCode = btEngine.Documents.Open(@"C:\bt\etiketak aldagaiekin\FORMBARCODE.btw");

                        BaloreakAsignatu(rootNode, etiketa, zebra);
                        //BaloreakAsignatu(rootNode, etiketaCode, zebra);
                        //BaloreakAsignatu(rootNode, etiketaGarantia, konica);

                        //crea un objeto de la clase XmlDeleter
                        XmlDeleter deleter = new XmlDeleter();

                        //borra el archivo de la carpeta "XML" y guarda una copia en la carpeta "Xml kopiak"
                        deleter.ezabatuXml(xmlFilePath);

                        Thread.Sleep(500);
                    }

                    catch (Exception ex)
                    {
                        // Si hay algún error, mostrarlo en la consola
                        Console.WriteLine($"Errorea fitxategia prezesatzean {fileName}: {ex.Message}");
                    }
                    finally
                    {
                        // Eliminar el nombre de archivo del ListBox
                        listBox1.Invoke(new Action(() => listBox1.Items.Remove(fileName)));
                    }
                }
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            lock (fileNames) //para que solo pueda acceder un subproceso a la vez
            {
                // Agregar el nombre del archivo a la lista si no está presente
                if (!fileNames.Contains(e.Name))
                {
                    fileNames.Add(e.Name);

                    // Agregar el nombre del archivo al ListBox
                    listBox1.Invoke(new Action(() => listBox1.Items.Add(e.Name)));
                }
            }

            // Señalizar el evento de que se ha creado un archivo en la carpeta
            fileCreatedEvent.Set();
        }

        private void BaloreakAsignatu(XmlNode nodoXml, LabelFormatDocument etiketa, string inpresora)
        {
            string nodoIzena = "";
            string nodoBalorea = "";
            SubStrings aldagaiak = null;

            foreach (XmlNode nodo in nodoXml.ChildNodes)
            {
                if (nodo.Name != "Numeros_Serie")
                {

                    // Obtener el nombre del nodo
                    nodoIzena = nodo.Name;

                    // Obtener el valor del nodo
                    nodoBalorea = nodo.InnerText;

                    // Obtener las variables de la etiqueta
                    aldagaiak = etiketa.SubStrings;

                    // Recorrer las variables de la etiqueta
                    foreach (SubString aldagaia in aldagaiak)
                    {
                        // Comparar el nombre de la variable con el nombre de la variable a asignar
                        if (aldagaia.Name == nodoIzena)
                        {
                            // Asignar el valor de la variable a la variable de la etiqueta
                            aldagaia.Value = nodoBalorea;
                        }
                    }
                }

                //XML dokumentoan "Numeros_Serie" aurkitzen duenean egingo duena
                else
                {
                    foreach (XmlNode nodoSerial in nodo.ChildNodes)
                    {
                        foreach (SubString aldagaia in aldagaiak)
                        {
                            // Comparar el nombre de la variable con el nombre de la variable a asignar
                            if (aldagaia.Name == "Serie")
                            {
                                //nodoaren balore aldagaia batean gorde
                                nodoBalorea = nodoSerial.InnerText;

                                // Asignar el valor de la variable a la variable de la etiqueta
                                aldagaia.Value = nodoBalorea;

                                Inprimatu(etiketa, inpresora);
                            }
                        }
                    }
                }
            }          
        }

        private string KodigoaAtera(XmlDocument ErpXml)
        {

            // Obtener el nodo "Codigo_Articulo"
            XmlNode codigoArticuloNode = ErpXml.SelectSingleNode("//Codigo_Articulo");

            // Obtener el valor del nodo y asignarlo a una variable
            string codigoArticulo = null;
            try
            {
                //xml-tik jasotako kodigoa aldagaian gorde
                codigoArticulo = codigoArticuloNode.InnerText;
                return codigoArticulo;
            }
            catch (NullReferenceException ex)
            {
                // Manejar la excepción si el nodo no se encuentra
                Console.WriteLine("No se encontró el nodo 'Codigo_Articulo'");
                return null;
            }
        }

        private string EtiketaFormatoaAtera(XmlDocument ErpWs)
        {

            // Obtener el nodo "Codigo_Articulo"
            XmlNode codigoArticuloNode = ErpWs.SelectSingleNode("//etiketaFormato");

            // Obtener el valor del nodo y asignarlo a una variable
            string etiketaFormato = null;
            try
            {
                etiketaFormato = codigoArticuloNode.InnerText;
                int etiketaFormatoZenb = Int32.Parse(etiketaFormato);

                if (etiketaFormatoZenb < 10)
                {
                    etiketaFormato = etiketaFormatoZenb.ToString();
                    etiketaFormato = "0" + etiketaFormato;
                }

                else
                {
                    etiketaFormato = etiketaFormatoZenb.ToString();
                }
                return etiketaFormato;
            }

            catch (NullReferenceException ex)
            {
                // Manejar la excepción si el nodo no se encuentra
                Console.WriteLine("No se encontró el nodo 'Codigo_Articulo'");
                return null;
            }
        }

        private void Inprimatu(LabelFormatDocument etiketa, string inpresora)
        {
            try
            {
                //inpresio motorea sortu
                Engine btEngine = new Engine();

                // Inpresio zerbitzariarekin konektatu
                btEngine.Start();

                // Inpresora konfiguratu
                etiketa.PrintSetup.PrinterName = inpresora;

                // etiketa inprimatu
                etiketa.Print();
                Thread.Sleep(1000);

                // Inpresio zerbitzariarekin konexioa ixteko
                btEngine.Stop();

                //string serie = etiketa.SubStrings.GetNamedSubStringValue("Serie");

                LogToFile(xmlFilePath+ " fitxategia inprimatu da");
            }
            catch (Exception ex)
            {
                // Si hay algún error, mostrarlo en la consola
                Console.WriteLine($"Errorea etiketa inprimatzerakoan: {ex.Message}");
            }

        }

        public XmlDocument JuntatuXmlak(XmlDocument xmlDoc1, XmlDocument xmlDoc2)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Crear el nodo raíz
            XmlNode rootNode = xmlDoc.CreateElement("root");
            xmlDoc.AppendChild(rootNode);

            // Importar los nodos hijos del primer documento
            foreach (XmlNode node in xmlDoc1.DocumentElement.ChildNodes)
            {
                XmlNode importedNode = xmlDoc.ImportNode(node, true);
                rootNode.AppendChild(importedNode);
            }

            // Importar los nodos hijos del segundo documento
            foreach (XmlNode node in xmlDoc2.DocumentElement.ChildNodes)
            {
                XmlNode importedNode = xmlDoc.ImportNode(node, true);
                rootNode.AppendChild(importedNode);
            }

            return xmlDoc;
        }

        private void KoloreaAldatu()
        {
            if (Xml_print.BackColor != Color.LightGreen)
            {
                // Cambiar el color del botón a verde si no lo está
                Xml_print.BackColor = Color.LightGreen;
            }
            else
            {
                // Cambiar el color del botón a su color original si ya está en verde
                Xml_print.BackColor = DefaultBackColor;
            }
        }

        private void btn_Gelditu_Click(object sender, EventArgs e)
        {
            btn_Gelditu.Enabled = false;

            watcher.EnableRaisingEvents = false; // Desactivar la generación de eventos del objeto FileSystemWatcher
            watcher.Created -= OnChanged; // Desuscribirse del evento cuando se detecte un cambio en la carpeta
            watcher.Dispose(); // Liberar los recursos del objeto FileSystemWatcher
            fileCreatedEvent.Reset(); // Resetear el AutoResetEvent utilizado para esperar la creación de archivos en la carpeta

            Xml_print.Enabled = true;
            KoloreaAldatu();
            Xml_print.Text = "BEGIRALEA MARTXAN JARRI";


            if (watcher != null)
            {
                watcher.Created -= OnChanged;
                watcher.EnableRaisingEvents = false;
            }
        }

        private void LogToFile(string message)
        {
            string logFilePath = "logFile.log";
            try
            {
                // Abrir un flujo de escritura hacia el archivo de registro y añadir el mensaje
                using (StreamWriter streamWriter = File.AppendText(logFilePath))
                {
                    // Formatear el mensaje con la fecha y hora actual
                    string logMessage = string.Format("{0} {1}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), message);

                    // Escribir el mensaje en el archivo de registro y guardar los cambios
                    streamWriter.WriteLine(logMessage);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (Exception ex)
            {
                // En caso de que haya un error, imprimir un mensaje de error en la consola
                Console.WriteLine("Erroea loga idazterakoan: " + ex.Message);
            }
        }
    }
}
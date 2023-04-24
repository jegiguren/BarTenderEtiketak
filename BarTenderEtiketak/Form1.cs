
using System;
using System.Collections;
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
using Timer = System.Windows.Forms.Timer;

namespace Xmlinprimatu
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
        Queue<string> fileNames = new Queue<string>();
        string etiketaFormatoa;
        string intermec = "Intermec PM43c_406_BACKUP";
        string pdf = "Microsoft Print to Pdf";
        string konica = "KONICA MINOLTA Admin";
        Queue<XmlDocument> xmlKola = new Queue<XmlDocument> ();


        public Form1()
        {
            InitializeComponent();
            btn_Gelditu.Enabled = false;
        }
        
        private async void Xml_print_Click(object sender, EventArgs e)
        {
            KoloreaAldatu();

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

            while (true)
            {
                // Esperar a que se cree un archivo en la carpeta
                fileCreatedEvent.WaitOne();
                  
                fileName = fileNames.Dequeue(); //coger el primer elemento de la cola y borrarlo de la cola

                // Construir la ruta completa del archivo
                string xmlFilePath = Path.Combine(filePath, fileName);

                xmlDoc.Load(xmlFilePath); //aldagaian kargatu xml-a

                xmlKola.Enqueue(xmlDoc); //añadir el xml a la cola


                while (xmlKola.Count > 0)
                {
                    xml = xmlKola.Dequeue(); // Saca el primer elemento de la cola

                    //xml-tik kodigo artikulua atera gero Ws-ari bidaltzeko
                    string codigoArticulo = KodigoaAtera(xml);

                    //WsReader klaseko objetua sortu
                    WsReader wsreader = new WsReader();
                    //web zerbitzua kontsumitu parametro bezala kodigoa bidaliz eta emaitza xml batean gorde
                    xmlWebService = await wsreader.WsKontsumitu(codigoArticulo);

                    //ERP-aren xml-a eta Web zerbitzuaren xml- juntatu
                    xmlosoa = JuntatuXmlak(xmlWebService, xml);
                    Console.WriteLine(xmlosoa.OuterXml);

                    Inprimatu(xmlosoa);

                    Thread.Sleep(1000);

                    listBox1.Invoke(new Action(() =>
                    {
                        listBox1.Items.RemoveAt(0); //borra el primer elemento del listbox
                    }));

                    //crea un objeto de la clase XmlDeleter
                    XmlDeleter deleter = new XmlDeleter();

                    //borra el archivo de la carpeta "XML" y guarda una copia en la carpeta "Xml kopiak"
                    deleter.ezabatuXml(xmlFilePath);
                }
            }
        }
       
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Agregar el nombre del archivo a la cola
            fileNames.Enqueue(e.Name);

            listBox1.Invoke(new Action(() =>
            {
                listBox1.Items.AddRange(fileNames.ToArray()); //añadir al lisbox los nombres de los archivos de la cola
            }));

            // Señalizar el evento de que se ha creado un archivo en la carpeta
            fileCreatedEvent.Set();
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

        private static void Inprimatu(XmlDocument xml)
        {
            // Crear una instancia de PrintDocument
            //System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument();

            // Manejar el evento PrintPage del objeto PrintDocument
            //printDoc.PrintPage += (sender, e) =>
            //{
                // Obtener el contenido del archivo XmlDocument como una cadena
                //string xmlString = xml.OuterXml;

                // Crear un objeto Font para el texto a imprimir
                //System.Drawing.Font font = new System.Drawing.Font("Arial", 12, FontStyle.Regular);

                // Configurar la posición y el tamaño del área de impresión
                //RectangleF area = e.MarginBounds;

                // Dibujar el contenido del archivo XmlDocument en el área de impresión
                //e.Graphics.DrawString(xmlString, font, Brushes.Black, area);
            //};

            // Crear una instancia de PrinterSettings
            //System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();

            // Habilitar la impresión a archivo
            //printerSettings.PrintToFile = true;

            // Especificar el nombre de archivo y la ubicación donde se guardará el archivo PDF
            //printerSettings.PrintFileName = @"C:\bt\PDF frogak\xml.pdf\";

            // Enviar el contenido a la impresora
            //printDoc.PrinterSettings = printerSettings;
            //printDoc.Print();


            Console.WriteLine(xml.OuterXml);
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
            if (Xml_print.BackColor != System.Drawing.Color.LightGreen)
            {
                // Cambiar el color del botón a verde si no lo está
                Xml_print.BackColor = System.Drawing.Color.LightGreen;
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
    }
}
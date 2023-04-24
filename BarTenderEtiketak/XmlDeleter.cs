using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Xmlinprimatu
{
    internal class XmlDeleter
    {
        string origen = @"C:\bt\XML\";
        string destino = @"C:\bt\XML kopiak\";
        public XmlDeleter() { }

        public void ezabatuKarpeta()
        {
            string[] fitxategiak = Directory.GetFiles(origen);

            foreach (string fitxategia in fitxategiak)
            {

                if (File.Exists(fitxategia))
                {
                    // Mover el archivo a la carpeta de destino
                    File.Move(fitxategia, Path.Combine(destino, Path.GetFileName(fitxategia)));
                    Console.WriteLine("Fitxategia ongi mugitu da Xml kopiak karpetara");
                    Console.WriteLine("XML karpeta zaintzen...");
                }
                else
                {
                    Console.WriteLine("Fitxategia ez da ongi mugitu karpetara");
                }
            }
        }

        public void ezabatuXml(string filePath)
        {

            if (File.Exists(filePath))
            {
                File.Move(filePath, Path.Combine(destino, Path.GetFileName(filePath)));
                Console.WriteLine("Fitxategia ongi ezabatu da: {0}", filePath);
            }
            else
            {
                Console.WriteLine("Fitxategia ezin da ezabatu: {0} ez da existitzen.", filePath);
            }
        }
    }
}
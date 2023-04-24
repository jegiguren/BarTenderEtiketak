using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace Xmlinprimatu
{
    internal class WsReader
    {
        
        private XmlNode root;
        private XmlDocument xmlDoc;
        public XmlNode Root { get { return root; } set { root = value; } }
        public XmlDocument XmlDoc { get { return xmlDoc; } set { xmlDoc = value; } }


        //Ws-aren url-a jaso, deia egin eta Xml-a bueltatzen digu
        public async Task<XmlDocument> WsKontsumitu(string cod)
        {
            try
            {
                //web serbitzu motako objetua sortu
                ServiceReference1.EtiquetadoApiPortTypeClient client = new ServiceReference1.EtiquetadoApiPortTypeClient();

                //guardar el resultado en la variable result1
                ServiceReference1.EtiquetadoBean result1 = await client.api_getSpecsEtiquetadoAsync(cod);

                //serializar el resultado a xml
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceReference1.EtiquetadoBean));

                using (StringWriter stringWriter = new StringWriter())
                {
                    // Serializa el objeto 'result1' y lo escribe en el objeto StringWriter
                    serializer.Serialize(stringWriter, result1);

                    // Obtiene la cadena XML generada por la serialización
                    string xml = stringWriter.ToString();

                    // Crear un objeto XmlDocument a partir del string XML
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);

                    //kontsolan inprimatu xml-a
                    //Console.WriteLine(xmlDoc.OuterXml);

                    return xmlDoc;
                }

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Errorea web zerbitzua kontsumitzean: {e.Message}");
                return null;
            }
        }


    }
}

using System.IO;
using System.Xml;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for XmlTools.
    /// </summary>
    public class XmlTools
    {
        public XmlTools() { }

        public static XmlDocument LoadXmlFile(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);

            if (!fi.Exists)
                return null;

            // Load the XML file
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            return doc;
        }
    }
}

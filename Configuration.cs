using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MusicBeePlugin
{
    public class Configuration
    {
        public class SerializedConfig
        {
            public string EndpointUrl { get; set; }
            public bool Suspended { get; set; }
        }

        public static string EndpointUrl { get; set; }
        public static bool Suspended { get; set; }

        public static void SaveConfig(string path)
        {
            var config = new SerializedConfig()
            {
                EndpointUrl = EndpointUrl,
                Suspended = Suspended
            };

            var serializer = new XmlSerializer(typeof(SerializedConfig));
            var writer = new StringWriter();
            serializer.Serialize(writer, config);
            File.WriteAllText(path, writer.ToString(), Encoding.UTF8);
        }

        public static void LoadConfig(string path)
        {
            if (!File.Exists(path))
            {
                EndpointUrl = "";
                Suspended = true;
            }
            else
            {
                var xmlText = File.ReadAllText(path, Encoding.UTF8);
                var reader = new StringReader(xmlText);
                var serializer = new XmlSerializer(typeof(SerializedConfig));
                var config = (SerializedConfig) serializer.Deserialize(reader);

                EndpointUrl = config.EndpointUrl;
                Suspended = config.Suspended;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Bitifier.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConsoleApplication1
{
   class Program
   {
      static void Main(string[] args)
      {
         //var configReader = new ConfigReader<Config>(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), new Uri(@"C:\temp\config.json"));

         //configReader.Changed += (source, config) =>
         //   {
         //      Console.WriteLine("Configuration updated. Config is now {0} and contains {1} values", config.Enabled, config.Values.Count);
         //   };


         //configReader.Start(TimeSpan.FromSeconds(10));

         var content = File.ReadAllText(@"C:\temp\config.yaml");

         //var d = DeserializeFromXml<Config>(content);

         var input = new StringReader(content);

         var deserializer = new Deserializer();

         var order = deserializer.Deserialize<Config>(input);

         Console.ReadLine();


      }

      public static T DeserializeFromXml<T>(string xml)
      {
         T result;
         var ser = new XmlSerializer(typeof(T));
         using (var tr = new StringReader(xml))
         {
            result = (T)ser.Deserialize(tr);
         }
         return result;
      }
   }

   public class Config
   {
      public bool Enabled { get; set; }

      public List<Guid> Values { get; set; }
   }
}

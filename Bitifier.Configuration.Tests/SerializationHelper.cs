using System.IO;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   public static class SerializationHelper
   {
      public static string SerializeAsYaml<T>(T obj)
      {
         using (var writer = new StringWriter())
         {
            var serializer = new Serializer();
            serializer.Serialize(writer, obj);

            return writer.ToString();
         }
      }

      public static string SerializeAsXml<T>(T obj)
      {
         using (var memoryStream = new MemoryStream())
         using (var reader = new StreamReader(memoryStream))
         {
            var serializer = new DataContractSerializer(obj.GetType());
            serializer.WriteObject(memoryStream, obj);
            memoryStream.Position = 0;
            return reader.ReadToEnd();
         }
      }
   }
}

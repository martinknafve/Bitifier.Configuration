using System.IO;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   public static class SerializationHelper
   {
      public static string Serialize<T>(T obj)
      {
         using (var writer = new StringWriter())
         {
            var serializer = new Serializer();
            serializer.Serialize(writer, obj);

            return writer.ToString();
         }
      }
   }
}

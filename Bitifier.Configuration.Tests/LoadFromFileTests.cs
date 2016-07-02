using System;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class LoadFromFileTests
   {
      [Test]
      public void ShouldBePossibleToLoadConfigFromFile()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            using (var reader = new ConfigReader<DummyAppConfiguration>(TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(10), new Uri(configFile)))
            {
               reader.Changed += (sender, config) => { dummyAppConfiguration = config; };

               reader.Start(TimeSpan.FromSeconds(1));

               Assert.IsNotNull(dummyAppConfiguration);
               Assert.AreEqual(12345, dummyAppConfiguration.Value);
            }
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      private string CreateDummyConfigurationFile(int value)
      {
         string configFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

         return CreateDummyConfigurationFile(configFile, value);
      }

      private string CreateDummyConfigurationFile(string configFile, int value)
      {
         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
            {
               Value = value
            };

         File.WriteAllText(configFile, JsonConvert.SerializeObject(dummyAppConfiguration));

         return configFile;
      }
   }
}

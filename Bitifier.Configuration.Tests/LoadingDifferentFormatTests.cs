using System;
using System.IO;
using NUnit.Framework;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class LoadingDifferentFormatTests
   {
      [Test]
      public void ShouldBePossibleToLoadFromYml()
      {
         var configFile = CreateConfigFile(".yml");
         
         try
         {
            AssertLoadingPossible(configFile);
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      [Test]
      public void ShouldBePossibleToLoadFromYaml()
      {
         var configFile = CreateConfigFile(".yaml");

         try
         {
            AssertLoadingPossible(configFile);
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      [Test]
      public void ShouldBePossibleToLoadFromXml()
      {
         var configFile = CreateConfigFile(".xml");

         try
         {
            AssertLoadingPossible(configFile);
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      private static void AssertLoadingPossible(string configFile)
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMinutes(30),
               RetryInterval = TimeSpan.FromSeconds(10)
            };

         using (var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
         {
            reader.Changed += (sender, config) => { dummyAppConfiguration = config; };

            reader.Start(TimeSpan.FromSeconds(1));

            Assert.IsNotNull(dummyAppConfiguration);
            Assert.AreEqual(12345, dummyAppConfiguration.Value);
         }
      }


      public string CreateConfigFile(string extension)
      {
         string configFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), extension);

         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
         {
            Value = 12345
         };

         switch (extension)
         {
            case ".yaml":
            case ".yml":
               File.WriteAllText(configFile, SerializationHelper.SerializeAsYaml(dummyAppConfiguration));
               break;
            case ".xml":
               File.WriteAllText(configFile, SerializationHelper.SerializeAsXml(dummyAppConfiguration));
               break;
            default:
               throw new NotImplementedException();
         }

         return configFile;
      }
   }
}

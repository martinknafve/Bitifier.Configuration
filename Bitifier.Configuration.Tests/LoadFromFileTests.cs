using System;
using System.IO;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class LoadFromFileTests
   {
      [Test]
      public void ShouldBePossibleToLoadFromFile()
      {
         string configFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".yml");

         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
            {
               Value = 12345
            };

         File.WriteAllText(configFile, SerializationHelper.SerializeAsYaml(dummyAppConfiguration));

         try
         {
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
         finally
         {
            File.Delete(configFile);
         }
      }
   }
}

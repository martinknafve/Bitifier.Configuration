using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class ChangedEventTests
   {
      [Test]
      public void TestEventTriggeredWhenStartCalled()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            var settings = new ConfigReaderSettings()
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

      [Test]
      public void TestEventTriggeredOnTimer()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            var resetEvent = new ManualResetEvent(false);

            var settings = new ConfigReaderSettings()
               {
                  RefreshInterval = TimeSpan.FromSeconds(1),
                  RetryInterval = TimeSpan.FromSeconds(1)
               };

            using (var reader = new ConfigReader<DummyAppConfiguration>(settings,
                  new Uri(configFile)))
            {
               reader.Changed += (sender, config) =>
               {
                  dummyAppConfiguration = config;

                  resetEvent.Set();
               };

               reader.Start(TimeSpan.FromSeconds(5));

               resetEvent.WaitOne();

               Assert.IsNotNull(dummyAppConfiguration);
               Assert.AreEqual(12345, dummyAppConfiguration.Value);
            }
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      [Test]
      public void TestEventTriggeredOnceIfFileNotChanged()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            int triggerCounter = 0;

            var settings = new ConfigReaderSettings()
               {
                  RefreshInterval = TimeSpan.FromMilliseconds(50),
                  RetryInterval = TimeSpan.FromMilliseconds(50)
               };

            using (var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
            {
               reader.Changed += (sender, config) =>
               {
                  dummyAppConfiguration = config;

                  triggerCounter++;
               };


               reader.Start(TimeSpan.FromSeconds(5));

               Thread.Sleep(TimeSpan.FromSeconds(2));

               Assert.AreEqual(1, triggerCounter);
            }
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      [Test]
      public void TestEventTriggeredMultipleTimesIfFileChanged()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            int triggerCounter = 0;

            var resetEvent = new ManualResetEvent(false);

            var settings = new ConfigReaderSettings()
               {
                  RefreshInterval = TimeSpan.FromMilliseconds(50),
                  RetryInterval = TimeSpan.FromMilliseconds(50)
               };

            using (var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
            {
               reader.Changed += (sender, config) =>
               {
                  dummyAppConfiguration = config;

                  triggerCounter++;

                  if (triggerCounter < 10)
                     CreateDummyConfigurationFile(configFile, triggerCounter);
                  else
                     resetEvent.Set();
               };

               reader.Start(TimeSpan.FromSeconds(30));

               resetEvent.WaitOne();

               Assert.AreEqual(10, triggerCounter);
               Assert.AreEqual(9, dummyAppConfiguration.Value);
            }
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      private string CreateDummyConfigurationFile(int value)
      {
         string configFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".xml");

         return CreateDummyConfigurationFile(configFile, value);
      }

      private string CreateDummyConfigurationFile(string configFile, int value)
      {
         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
            {
               Value = value
            };

         File.WriteAllText(configFile, SerializationHelper.SerializeAsXml(dummyAppConfiguration));

         return configFile;
      }
   }
}

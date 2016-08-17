using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MockHttpServer;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class LoadFromHttpTests
   {
      [Test]
      public void ShouldBePossibleToLoadConfigFromHttp()
      {
         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
         {
            Value = 12345

         };

         string configText = SerializationHelper.SerializeAsYaml(dummyAppConfiguration);

         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data.yml", "GET", (req, rsp, prm) => configText),
            };

         using (new MockServer(12345, requestHandlers))
         {
            var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMinutes(30),
               RetryInterval = TimeSpan.FromSeconds(10)
            };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri("http://127.0.0.1:12345/data.yml")))
            {
               reader.Changed += (sender, config) => { dummyAppConfiguration = config; };

               reader.Start(TimeSpan.FromSeconds(5));

               Assert.IsNotNull(dummyAppConfiguration);
               Assert.AreEqual(12345, dummyAppConfiguration.Value);
            }

         }
      }

      [Test]
      public void ChangedEventShouldNotBeTriggeredIfConfigNotChanged()
      {
         DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
         {
            Value = 12345
         };

         string configText = SerializationHelper.SerializeAsYaml(dummyAppConfiguration);


         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data.yml", "GET", (req, rsp, prm) => configText),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMilliseconds(50),
               RetryInterval = TimeSpan.FromMilliseconds(50)
            };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri("http://127.0.0.1:12345/data.yml")))
            {
               reader.Changed += (sender, config) => { timesChanged++; };
               reader.Start(TimeSpan.FromSeconds(100));

               Thread.Sleep(TimeSpan.FromSeconds(2));

               Assert.AreEqual(1, timesChanged);
            }

         }
      }

      [Test]
      public void ChangedEventShouldBeTriggeredIfConfigIsChanged()
      {
         int requestCount = 0;

         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data.yml", "GET", (req, rsp, prm) =>
                {
                  DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
                     {
                        Value = requestCount
                     };

                  string configText = SerializationHelper.SerializeAsYaml(dummyAppConfiguration);

                  requestCount ++;
                  return configText;
                }),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMilliseconds(50),
               RetryInterval = TimeSpan.FromMilliseconds(50)
            };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri("http://127.0.0.1:12345/data.yml")))
            {
               reader.Changed += (sender, config) => { timesChanged++; };
               reader.Start(TimeSpan.FromSeconds(100));

               Thread.Sleep(TimeSpan.FromSeconds(2));

               Assert.Greater(timesChanged, 1);
            }

         }
      }

      [Test]
      public void ChangedEventShouldNotBeTriggeredIfContentChangedByEtagTheSame()
      {
         int requestCount = 0;

         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data.yml", "GET", (req, rsp, prm) =>
                {
                   var ifNoneMatchValue = req.Headers["If-None-Match"];

                   if (!string.IsNullOrWhiteSpace(ifNoneMatchValue))
                   {
                      if (ifNoneMatchValue == "MyEtag")
                      {
                         rsp.StatusCode = 304;
                         return string.Empty;
                      }
                   }

                   rsp.Headers.Add("ETag", "MyEtag");

                   DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
                     {
                        Value = requestCount
                     };

                   string configText = SerializationHelper.SerializeAsYaml(dummyAppConfiguration);

                   requestCount ++;
                   return configText;
                }),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMilliseconds(50),
               RetryInterval = TimeSpan.FromMilliseconds(50)
            };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri("http://127.0.0.1:12345/data.yml")))
            {
               reader.Changed += (sender, config) => { timesChanged++; };
               reader.Start(TimeSpan.FromSeconds(100));

               Thread.Sleep(TimeSpan.FromSeconds(2));

               Assert.AreEqual(1, timesChanged);
            }

         }
      }
   }
}

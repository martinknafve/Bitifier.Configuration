using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MockHttpServer;
using Newtonsoft.Json;
using NUnit.Framework;

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

         var configText = JsonConvert.SerializeObject(dummyAppConfiguration);

         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data", "GET", (req, rsp, prm) => configText),
            };

         using (new MockServer(12345, requestHandlers))
         {
            using (
               var reader = new ConfigReader<DummyAppConfiguration>(TimeSpan.FromMinutes(30),
                  TimeSpan.FromSeconds(10), new Uri("http://127.0.0.1:12345/data")))
            {
               reader.Changed += (sender, config) => { dummyAppConfiguration = config; };

               reader.Start(TimeSpan.FromSeconds(100));

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

         var configText = JsonConvert.SerializeObject(dummyAppConfiguration);

         var requestHandlers = new List<MockHttpHandler>()
            {
                new MockHttpHandler("/data", "GET", (req, rsp, prm) => configText),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            using (
               var reader = new ConfigReader<DummyAppConfiguration>(TimeSpan.FromMilliseconds(50),
                  TimeSpan.FromMilliseconds(50), new Uri("http://127.0.0.1:12345/data")))
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
                new MockHttpHandler("/data", "GET", (req, rsp, prm) =>
                {
                  DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
                     {
                        Value = requestCount
                     };

                  var configText = JsonConvert.SerializeObject(dummyAppConfiguration);

                  requestCount ++;
                  return configText;
                }),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            using (
               var reader = new ConfigReader<DummyAppConfiguration>(TimeSpan.FromMilliseconds(50),
                  TimeSpan.FromMilliseconds(50), new Uri("http://127.0.0.1:12345/data")))
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
                new MockHttpHandler("/data", "GET", (req, rsp, prm) =>
                {
                   var ifNoneMatchValue = req.Headers["If-None-Match"];

                   if (!string.IsNullOrWhiteSpace(ifNoneMatchValue))
                   {
                      if (ifNoneMatchValue == "MyEtag")
                      {
                         rsp.StatusCode = 409;
                         return string.Empty;
                      }
                   }

                   rsp.Headers.Add("ETag", "MyEtag");

                   DummyAppConfiguration dummyAppConfiguration = new DummyAppConfiguration
                     {
                        Value = requestCount
                     };

                   var configText = JsonConvert.SerializeObject(dummyAppConfiguration);

                   requestCount ++;
                   return configText;
                }),
            };

         int timesChanged = 0;

         using (new MockServer(12345, requestHandlers))
         {
            using (
               var reader = new ConfigReader<DummyAppConfiguration>(TimeSpan.FromMilliseconds(50),
                  TimeSpan.FromMilliseconds(50), new Uri("http://127.0.0.1:12345/data")))
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

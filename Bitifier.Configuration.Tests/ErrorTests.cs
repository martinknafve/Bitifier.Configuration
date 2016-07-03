using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class ErrorTests
   {
      [Test]
      public void WhenStartedThrowsFileNotFoundExceptionForLocalInaccessibleFile()
      {
         string configFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

         var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMinutes(30),
               RetryInterval = TimeSpan.FromSeconds(10)
            };

         using (
            var reader = new ConfigReader<DummyAppConfiguration>(settings,
               new Uri(configFile)))
         {
            AggregateException aggregateException = null;

            reader.Error += (sender, exception) =>
            {
               aggregateException = exception;
            };

            Assert.Throws<TimeoutException>(() => reader.Start(TimeSpan.FromSeconds(1)));

            Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
            Assert.IsNotNull(aggregateException.InnerExceptions[0] as FileNotFoundException);
         }
      }

      [Test]
      public void WhenStartedThrowsWebExceptionForInaccessibleWebUri()
      {
         var settings = new ConfigReaderSettings
         {
            RefreshInterval = TimeSpan.FromMinutes(30),
            RetryInterval = TimeSpan.FromSeconds(10)
         };

         using (
            var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri("https://nonexistant.example.com")))
         {
            AggregateException aggregateException = null;

            reader.Error += (sender, exception) =>
            {
               aggregateException = exception;
            };

            Assert.Throws<TimeoutException>(() => reader.Start(TimeSpan.FromSeconds(1)));

            Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
            Assert.IsNotNull(aggregateException.InnerExceptions[0] as WebException);
         }
      }

      [Test]
      public void WhenStartedOneInnerExceptionPerInaccessibleConfigFile()
      {
         string configFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

         var settings = new ConfigReaderSettings
         {
            RefreshInterval = TimeSpan.FromSeconds(10),
            RetryInterval = TimeSpan.FromMinutes(30)
         };

         using (var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile), new Uri(configFile), new Uri("https://nonexistant.example.com")))
         { 
            AggregateException aggregateException = null;

            reader.Error += (sender, exception) =>
            {
               aggregateException = exception;
            };

            Assert.Throws<TimeoutException>(() => reader.Start(TimeSpan.FromSeconds(1)));

            Assert.AreEqual(3, aggregateException.InnerExceptions.Count);
            Assert.IsNotNull(aggregateException.InnerExceptions[0] as FileNotFoundException);
            Assert.IsNotNull(aggregateException.InnerExceptions[1] as FileNotFoundException);
            Assert.IsNotNull(aggregateException.InnerExceptions[2] as WebException);
         }
      }

      [Test]
      public void WhenStartedErrorEventTriggeredForUnloadableFile()
      {
         string configFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

         var resetEvent = new ManualResetEvent(false);

         var settings = new ConfigReaderSettings
            {
               RefreshInterval = TimeSpan.FromMilliseconds(50),
               RetryInterval = TimeSpan.FromMilliseconds(50)
            };

         using (
            var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
         {
            reader.Error += (sender, aggregateException) =>
            {
               Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
               Assert.IsNotNull(aggregateException.InnerExceptions[0] as FileNotFoundException);

               resetEvent.Set();

            };

            Assert.Throws<TimeoutException>(() => reader.Start(TimeSpan.FromSeconds(1)));

            resetEvent.WaitOne();
         }


      }

   }
}

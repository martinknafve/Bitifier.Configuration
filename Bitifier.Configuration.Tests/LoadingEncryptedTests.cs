using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bitifier.Configuration.Tests.Certificates;
using Bitifier.RsaEncryption;
using Moq;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration.Tests
{
   [TestFixture]
   public class LoadingEncryptedTests
   {
      private X509Certificate2 TestCertificate = X509Certificate2Loader.Load2048AWithPrivateKey();

      [Test]
      public void LoadShouldThrowUnableToFindCertIfCertNotExist()
      {

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            var settings = new ConfigReaderSettings
               {
                  RefreshInterval = TimeSpan.FromMinutes(30),
                  RetryInterval = TimeSpan.FromSeconds(10)
               };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
            {
               AggregateException aggregateException = null;

               reader.Error += (sender, exception) =>
               {
                  aggregateException = exception;
               };

               Assert.Throws<TimeoutException>(() => reader.Start(TimeSpan.FromSeconds(1)));

               Assert.AreEqual(1, aggregateException.InnerExceptions.Count);

               StringAssert.Contains("Unable to find requested certificate",
                  aggregateException.InnerExceptions[0].Message);
            }
         }
         finally
         {
            File.Delete(configFile);
         }
      }

      [Test]
      public void LoadShouldBeAbleToDecryptData()
      {
         DummyAppConfiguration dummyAppConfiguration = null;

         string configFile = CreateDummyConfigurationFile(12345);

         try
         {
            var certificateStoreRepository = new Mock<ICertificateStoreRepository>();
            certificateStoreRepository.Setup(
               f => f.Find(It.IsAny<StoreLocation>(), It.IsAny<StoreName>(), TestCertificate.Thumbprint)).Returns(() => new [] {TestCertificate});

            var settings = new ConfigReaderSettings
               {
                  RefreshInterval = TimeSpan.FromMinutes(30),
                  RetryInterval = TimeSpan.FromSeconds(10),
                  CertificateStoreRepository = certificateStoreRepository.Object
            };

            using (
               var reader = new ConfigReader<DummyAppConfiguration>(settings, new Uri(configFile)))
            {
               reader.Changed += (sender, config) => { dummyAppConfiguration = config; };

               reader.Start(TimeSpan.FromSeconds(1));

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


         string serializedPlainTextConfig = SerializationHelper.Serialize(dummyAppConfiguration);

         var certificateRepo = new Mock<ICertificateStoreRepository>();
         certificateRepo.Setup(f => f.Find(It.IsAny<StoreLocation>(), It.IsAny<StoreName>(), It.IsAny<string>()))
            .Returns(() => new [] { TestCertificate});
         
         var crypto = new X509Certificate2ThumbprintCrypto(certificateRepo.Object);
         var encrypted = crypto.Encrypt(StoreLocation.CurrentUser, StoreName.My, TestCertificate.Thumbprint, serializedPlainTextConfig);
         
         var cipherSerializer = new CipherTextWithCertificateInfoSerializer();
         var serialziedCipherTxtWithCertInfo = cipherSerializer.Serialize(encrypted);

         File.WriteAllText(configFile, serialziedCipherTxtWithCertInfo);

         return configFile;
      }
   }
}

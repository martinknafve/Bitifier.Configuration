using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Bitifier.RsaEncryption;
using YamlDotNet.Serialization;

namespace Bitifier.Configuration
{
   public class ConfigReader<T> : IDisposable
   {
      private readonly ConfigDownloader _downloader;
      private readonly Timer _timer;
      private readonly ConfigReaderSettings _settings;
      public event EventHandler<T> Changed;
      public event EventHandler<AggregateException> Error;

      private bool _disposed;

      private readonly ManualResetEvent _initialLoadCompleted = new ManualResetEvent(false);

      public ConfigReader(ConfigReaderSettings settings, params Uri[] configUris)
      {
         if (settings == null)
            throw new ArgumentNullException("settings", "settings must be specified.");

         _settings = settings;
         _timer = new Timer(OnTick, null, Int32.MaxValue, Int32.MaxValue);
         _downloader = new ConfigDownloader(configUris);
      }

      public void Start(TimeSpan timeout)
      {
         ChangeTimer(TimeSpan.FromSeconds(0), _settings.RefreshInterval);

         if (!_initialLoadCompleted.WaitOne(timeout))
            throw new TimeoutException("Configuration was not loaded.");
      }

      private void LoadInternal()
      {
         string configuration;
         string extension;

         if (_downloader.DownloadIfChanged(out configuration, out extension))
         {
            T typedConfig;

            var serializer = new CipherTextWithCertificateInfoSerializer();
            if (serializer.IsSerializedCipherText(configuration))
            {
               var cipherTextWithCertificateInfo = serializer.Deserialize(configuration);

               var crypto = CreateCrypto();
               var plainText = crypto.Decrypt(cipherTextWithCertificateInfo);

               typedConfig = Deserialize(plainText, extension);
            }
            else
            {
               typedConfig = Deserialize(configuration, extension);
            }

            InvokeChangedEvent(typedConfig);

            _initialLoadCompleted.Set();
         }
      }

      private static T Deserialize(string configuration, string extension)
      {
         T typedConfig;

         switch (extension.ToLowerInvariant())
         {
            case ".yaml":
            case ".yml":
            {
               using (var input = new StringReader(configuration))
               {
                  var deserializer = new Deserializer(ignoreUnmatched: true);
                  typedConfig = deserializer.Deserialize<T>(input);
               }
               break;
            }
            case ".xml":
            {
               using (var stream = new MemoryStream())
               {
                  byte[] data = System.Text.Encoding.UTF8.GetBytes(configuration);
                  stream.Write(data, 0, data.Length);
                  stream.Position = 0;

                  var deserializer = new DataContractSerializer(typeof (T));
                  typedConfig = (T) deserializer.ReadObject(stream);
               }
               break;
            }
            default:
               throw new InvalidOperationException(string.Format("Unsupported extension: {0}. Only .yaml, .yml and .xml is supported", extension));
         }
               
         return typedConfig;
      }

      private X509Certificate2ThumbprintCrypto CreateCrypto()
      {
         X509Certificate2ThumbprintCrypto crypto;

         if (_settings.CertificateStoreRepository == null)
            crypto = new X509Certificate2ThumbprintCrypto();
         else
            crypto = new X509Certificate2ThumbprintCrypto(_settings.CertificateStoreRepository);

         return crypto;
      }

      private void InvokeChangedEvent(T config)
      {
         var ev = Changed;
         ev?.Invoke(this, config);
      }

      private void InvokeErrorEvent(AggregateException exception)
      {
         var ev = Error;

         ev?.Invoke(this, exception);
      }
      
      private void OnTick(object state)
      {
         ChangeTimer(int.MaxValue, int.MaxValue);

         bool succesful = false;

         try
         {
            LoadInternal();

            succesful = true;
         }
         catch (AggregateException exception)
         {
            InvokeErrorEvent(exception);
         }
         catch (Exception exception)
         {
            InvokeErrorEvent(new AggregateException(Messages.ConfigLoadFailedMessage, new [] { exception}));
         }
         finally
         {
            if (succesful)
            {
               ChangeTimer(_settings.RefreshInterval, _settings.RefreshInterval);
            }
            else
            {
               ChangeTimer(_settings.RetryInterval, _settings.RetryInterval);
            }
         }
      }

      public void Dispose()
      {
         _disposed = true;

         _timer.Dispose();
      }

      private void ChangeTimer(TimeSpan dueTime, TimeSpan period)
      {
         if (_disposed)
            return;

         try
         {
            _timer.Change(dueTime, period);
         }
         catch (ObjectDisposedException)
         {
            
         }
      }

      private void ChangeTimer(int dueTime, int period)
      {
         if (_disposed)
            return;

         try
         {
            _timer.Change(dueTime, period);
         }
         catch (ObjectDisposedException)
         {

         }
      }
   }
}

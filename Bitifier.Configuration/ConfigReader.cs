using System;
using System.Threading;
using Bitifier.RsaEncryption;
using Newtonsoft.Json;

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

         if (_downloader.DownloadIfChanged(out configuration))
         {
            T typedConfig;

            var serializer = new CipherTextWithCertificateInfoSerializer();
            if (serializer.IsSerializedCipherText(configuration))
            {
               var cipherTextWithCertificateInfo = serializer.Deserialize(configuration);

               var crypto = CreateCrypto();
               var plainTextJson = crypto.Decrypt(cipherTextWithCertificateInfo);

               typedConfig = JsonConvert.DeserializeObject<T>(plainTextJson);
            }
            else
            {
               typedConfig = JsonConvert.DeserializeObject<T>(configuration);
            }

            InvokeChangedEvent(typedConfig);

            _initialLoadCompleted.Set();
         }
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

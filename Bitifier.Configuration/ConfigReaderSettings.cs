using System;
using Bitifier.RsaEncryption;

namespace Bitifier.Configuration
{
   public class ConfigReaderSettings
   {
      public ConfigReaderSettings()
      {
         RefreshInterval = TimeSpan.FromMinutes(1);
         RetryInterval = TimeSpan.FromSeconds(10);
      }

      /// <summary>
      /// Gets or sets the time between the configuration is reloaded. Default 1 minute.
      /// </summary>
      public TimeSpan RefreshInterval { get; set; }

      /// <summary>
      /// Gets or sets the time between new attempts to load configuration is made, if there is a problem loading it. Default 10 seconds.
      /// </summary>
      public TimeSpan RetryInterval { get; set; }

      /// <summary>
      /// Gets or sets the repository for loading certificates. If not specified, certificates will be loaded from the Windows certificate store.
      /// </summary>
      public ICertificateStoreRepository CertificateStoreRepository { get; set; }
   }
}

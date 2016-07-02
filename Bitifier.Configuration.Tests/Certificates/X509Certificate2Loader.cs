using System.Security.Cryptography.X509Certificates;

namespace Bitifier.Configuration.Tests.Certificates
{
   static class X509Certificate2Loader
   {
      public static X509Certificate2 Load2048AWithPrivateKey()
      {
         return new X509Certificate2(Resources.Test2048A_pfx, "secret");
      }

      public static X509Certificate2 Load2048AWithoutPrivateKey()
      {
         return new X509Certificate2(Resources.Test2048A_cer);
      }

      public static X509Certificate2 Load2048BWithPrivateKey()
      {
         return new X509Certificate2(Resources.Test2048B_pfx, "secret");
      }

      public static X509Certificate2 Load4096AWithPrivateKey()
      {
         return new X509Certificate2(Resources.Test4096A_pfx, "secret");
      }
   }
}

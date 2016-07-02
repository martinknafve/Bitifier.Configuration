using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Bitifier.Configuration
{
   class ConfigDownloader
   {
      private readonly Uri[] _configUris;
      private string _latestSha1Hash;
      private string _latestEtag;


      public ConfigDownloader(params Uri[] configUris)
      {
         _configUris = configUris;
      }

      public bool DownloadIfChanged(out string configuration)
      {
         var exceptions = new List<Exception>();

         foreach (var uri in _configUris)
         {
            try
            {
               return DownloadIfChanged(uri, out configuration);
            }
            catch (Exception exception)
            {
               exceptions.Add(exception);
            }
         }

         throw new AggregateException(Messages.ConfigLoadFailedMessage, exceptions);
      }

      private bool DownloadIfChanged(Uri uri, out string configuration)
      {
         if (uri.IsFile)
         {
            var filePath = uri.AbsolutePath;
            configuration = File.ReadAllText(filePath);

            _latestEtag = null;
         }
         else
         {
            var request = WebRequest.Create(uri);

            if (_latestEtag != null)
               request.Headers.Add("If-None-Match", _latestEtag);

            try
            {
               using (var response = request.GetResponse())
               using (var responseStream = response.GetResponseStream())
               using (var streamReader = new StreamReader(responseStream))
               {
                  _latestEtag = response.Headers[HttpResponseHeader.ETag];
                  configuration = streamReader.ReadToEnd();
               }
            }
            catch (WebException exception)
            {
               if (exception.Status == WebExceptionStatus.ProtocolError)
               {
                  var response = (HttpWebResponse) exception.Response;

                  if (response.StatusCode == HttpStatusCode.Conflict)
                  {
                     configuration = null;
                     return false;
                  }
               }

               throw;
            }
         }

         var hash = GetHash(configuration);

         if (hash != _latestSha1Hash)
         {
            _latestSha1Hash = hash;
            return true;
         }

         return false;
      }

      private static string GetHash(string configuration)
      {
         var data = System.Text.Encoding.ASCII.GetBytes(configuration);
         using (var sha1 = System.Security.Cryptography.SHA1.Create())
         {
            var hash = sha1.ComputeHash(data);

            return Convert.ToBase64String(hash);
         }
      }
   }
}

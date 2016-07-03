using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bitifier.Configuration;

namespace ConsoleApplication1
{
   class Program
   {
      static void Main(string[] args)
      {
         var configReader = new ConfigReader<Config>(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), new Uri(@"C:\temp\config.json"));

         configReader.Changed += (source, config) =>
            {
               Console.WriteLine("Configuration updated. Config is now {0} and contains {1} values", config.Enabled, config.Values.Count);
            };


         configReader.Start(TimeSpan.FromSeconds(10));

         Console.ReadLine();
      }
   }

   class Config
   {
      public bool Enabled { get; set; }
      public List<Guid> Values { get; set; }
   }
}

# Bitifier.Configuration

Bitifier Configuration is a .NET library which simplifies handling of application configuration. Bitifier Configuration supports .NET Framework 4.5.2 and later and can be obtained [via NuGet](https://www.nuget.org/packages/Bitifier.Configuration/).

[![Build status](https://ci.appveyor.com/api/projects/status/lhk2hgvif3gx3xhw?svg=true)](https://ci.appveyor.com/project/MartinKnafve/bitifier-configuration)

# Features

Bitifier Configuration makes it easy to implement the following:

* Centralized configuration accessed over HTTP/HTTPS or file system
* Simple configuration storage using YAML or XML
* Type-safe access to configuration with support for composite data structures (custom types, lists, dictionaries, etc)
* Automatic refresh of settings from the configuration data source
* Optional asymmetric encryption using RSA and X.509 certificates.
* Multiple configuration data sources for failover

# Tutorial

1. Start Visual Studio 2015 or later
2. Create a new project, of type Console Application (.NET Framework 4.5.2 or later)
3. Add a NuGet reference to Bitifier.Configuration
4. Define a new class to hold your configuration:

   ```cs
   class Config
   {
      public bool Enabled { get; set; }
      public List<Guid> Values { get; set; }
   } 
   ```
5. Create a YAML file holding the configuration. You can also choose to create the configuration as XML. When YAML is used, the file extension needs to be either .yml or .yaml and when XML is used, it needs to be .xml:

   ```yaml
   Enabled: true
   Values:
    - a1954896-4cf5-49bb-b600-ad2fe22701d8
    - 7a4dc432-3045-4ee3-b49e-1b7cd4c655a1
  ```
6. Create an instance of ConfigReader and subscribe to the Changed-event. The constructor takes a settings object, and a list of uri's the configuration should be fetched from. These uri's can either be full paths to local disk or a network share, or a HTTP Uri. The settings object lets you define how often settings should be refreshed, and how often retries should be made if there's a problem accessing the data source.

   ```cs
   var settings = new ConfigReaderSettings
      {
         RefreshInterval = TimeSpan.FromSeconds(30),
      };
   
   var configReader = new ConfigReader<Config>(settings, new Uri(@"C:\temp\config.yaml"));
   configReader.Changed += (source, config) =>
      {
         Console.WriteLine("Configuration updated. Config is now {0} and contains {1} values", 
            config.Enabled, 
           config.Values.Count);
      };
   ```
   
   The Changed-event will be triggerd whenever the configuration has been updated. If a HTTP/HTTPS backend is used, and it supports ETag, then whenever the ETag value changes the event will be triggered. If the server does not provide a ETag, or a local file system is used, the SHA1 hash of the config file will beused.
   
7. To start reading the configuration, call the Start method on the config reader. `Start`-ing the ConfigReader will intiate an immediate read of the settings. The method will block until the settings are read, or the supplied timeout is reached. If the timeout is reached, a TimeoutException will be thrown.

   ```cs
   configReader.Start(TimeSpan.FromSeconds(30));
   ```



Below is the complete code listing:
   
```cs
using System;
using System.Collections.Generic;
using Bitifier.Configuration;

namespace ConsoleApplication
{
   class Program
   {
      static void Main(string[] args)
      {
         var settings = new ConfigReaderSettings()
            {
               RefreshInterval = TimeSpan.FromSeconds(30),
            };

         var configReader = new ConfigReader<Config>(settings, new Uri(@"C:\temp\config.yaml"));
         
         configReader.Changed += (source, config) =>
         {
            Console.WriteLine("Configuration updated. Config is now {0} and contains {1} values",
               config.Enabled,
               config.Values.Count);
         };

         configReader.Start(TimeSpan.FromSeconds(30));
         Console.ReadLine();
      }
   }

   class Config
   {
      public bool Enabled { get; set; }
      public List<Guid> Values { get; set; }
   }
}
```
   
   
# Error handling

If there is a problem accessing the configuration (for example the HTTP backend is not responding) or if the file cannot be deserialized, the Error event will be triggered supplying information on what was failed.

```cs
   configReader.Error += (sorurce, aggregateException) =>
      {
         foreach (var exception in aggregateException.InnerExceptions)
         {
            Console.WriteLine(exception.Message);
         }
      };
```

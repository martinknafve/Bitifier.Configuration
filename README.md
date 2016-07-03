# Bitifier.Configuration

Bitifier Configuration is a .NET library which simplifies implementation of centralized configuration. 

# Key features

Bitifier Configuration makes it easy to implement the following:

* Centralized configuration accessed over HTTP/HTTPS or file system
* Simple configuration storage using JSON
* Type-safe access to configuration with composite data structures (lists, dictionaries, etc)
* Automatic refresh of settings from the data source
* Assymetric encryption using RSA and X.509 certificates.
* Multiple data sources for failover

# Getting started

1. Start Visual Studio 2015 or later
2. Create a new project, of type Console Application (.NET Framework 4.5.2 or later)
3. Add a NuGet reference to Bitifier.Configuration
4. Define a new class to hold your configuration

   ```cs
   class Config
   {
      public bool Enabled { get; set; }
      public List<Guid> Values { get; set; }
   } 
   ```
5. Create a JSON file holding the configuration:

   ```js
   {
     "Enabled": true,
     "Values": [
       "a1954896-4cf5-49bb-b600-ad2fe22701d8",
       "7a4dc432-3045-4ee3-b49e-1b7cd4c655a1"
     ]
   }
  ```
6. Create an instance of ConfigReader and subscribe to the Changed-event. The constructor takes three or more arguments.

   ```cs
   var configReader = new ConfigReader<Config>(TimeSpan.FromSeconds(5), 
                                               TimeSpan.FromSeconds(10), 
                                               new Uri(@"C:\temp\config.json"));
   configReader.Changed += (source, config) =>
   {
      Console.WriteLine("Configuration updated. Config is now {0} and contains {1} values", 
        config.Enabled, 
        config.Values.Count);
   };
   ```
   
   

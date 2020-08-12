# Eshopworld.DevOps package

[![Build status](https://dev.azure.com/eshopworld/Github%20build/_apis/build/status/devops)](https://dev.azure.com/eshopworld/Github%20build/_build/latest?definitionId=150)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=esw.devops&metric=coverage)](https://sonarcloud.io/dashboard?id=esw.devops)

## Summary

DevOps extensions, used for application configuration setup.

## Load configurations
Below is an example of loading various config sources using the `UseDefaultConfigs` method.  This will load the following configs (in the order shown):
- Environment Variables
- Command Line
- `appSettings.json` file
- environment specific `appSettings.json`

```csharp
public class Program
{
   ...
   public void ConfigureAppConfiguration(IConfigurationBuilder builder)
   {
	// Load config sources by calling the UseDefaultConfigs.
        builder.UseDefaultConfigs();

	// Load other config here...
   }
   ...
}
```

## Load individual secrets from a single Key Vault
Individual secrets can be loaded into configuration, directly from Key Vault in the following way:

```csharp
public class Program
{
     ...
     public void ConfigureAppConfiguration(IConfigurationBuilder builder)
     {
          // Load various config sources.
          builder.UseDefaultConfigs();

          // Pass the name of the secrets you wish to load into the configuration builder.
          builder.AddKeyVaultSecrets("TenantId", 
		                     "SubscriptionId", 
		                     "OtherSecretName");
     }
     ...
}
```

To use this method, the code expects a setting called _"KEYVAULT_URL"_.  This will typically have been setup by DevOps as an **environment** variable on the machine running the code.  If we take a key vault instance called example1, the environment setting would look like this:
`https://example1.vault.azure.net/`

Otherwise, if the `KEYVAULT_URL` setting cannot be found, the method will fallback to a config setting called _"KeyVaultInstanceName"_. This can be set in your `AppSettings.json` file.  It would be look like `example1` and the url will be inferred automatically.

AppSettings.json
```json
{
     "KeyVaultInstanceName": "example1",
     "SomeOtherSetting1": 100,
     "SomeOtherSetting2": false
}
```


## Load individual secrets from multiple key vaults

If you require settings to be loaded from multiple Key Vaults, it can be done in the following way:

```csharp
public class Program
{
	...
    public void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
	   // Load from all config sources.
           builder.UseDefaultConfigs();

           // Overload method 1: Add from key vault loaded in KEYVAULT_URL setting.
           builder.AddKeyVaultSecrets("SomeKey1", "SomeKey2");

	   var kvUriInstance1 = new Uri("https://instance1.vault.azure.net");
	   var kvUriInstance2 = new Uri("https://instance2.vault.azure.net");

           // Overload method 2: Pass the instance and list of the secrets you wish to load into configuration.
           builder.AddKeyVaultSecrets(kvUriInstance1, new [] {
			"TenantId", 
			"SubscriptionId", 
			"OtherSecretName" });
			
	   builder.AddKeyVaultSecrets(kvUriInstance2, new [] {
			"OtherKey1", 
			"OtherKey2", 
			"OtherKey3" });
    }
    ...
}
```

## Using the loaded configuration

Take this POCO class:

```csharp
public class AppSettings {
     public string TenantId { get; set; }
     public string SubscriptionId { get; set; }
     public string OtherSecretName { get; set; }
}
```

We can bind the settings to this class using the `BindBaseSection` call as follows:

```csharp
// Taken from Startup.cs after the ConfigureAppConfiguration above has been run.

public void ConfigureServices(IServiceCollection services)
{
     // Example of binding settings directly to a class (without the "GetSection" call).
     var appSettings = _configuration.BindBaseSection<AppSettings>();
	
     // Example of directly using the settings directly after they are loaded.
     var tenantId = _configuration["TenantId"];
	
     var otherSecret = null;
	
     if (_configuration.TryGetValue<string>("OtherSecretName"), out otherSecret) 
     {
	... do something conditional if the setting exists ...
     }
}
```

It's worth noting, if you have a Key Vault setting called with dashes in the name, ex "My-Key-1", it can bind to a Poco class property called "MyKey1".  The `BindBaseSection` method will make a version of the config setting that has not got the dashes.

## Full example of code above using WebHostBuilder

Example of using with a WebHostBuilder when bootstrapping a Web Application.

```csharp
public class Program
{
     public static void Main(string[] args)
     {
        try
        {
            // Build and run the web host.
            var host = CreateWebHostBuilder(args).Build().Run();
        }
        catch (Exception e)
	{
	     // Probably want to log this using BigBrother (there's a bit of a 
	     // race condition here as BB might not be wired up yet!).
            
	     // Catch startup errors and bare minimum log to console or event log.
             Console.WriteLine($"Problem occured during startup of {Assembly.GetExecutingAssembly().GetName().Name}");
             Console.WriteLine(e);

	     // Stop the application by continuing to throw the exception.
             throw;
        }
     }

	
     // In program, we setup our configuration in the standard microsoft way...
     private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
          WebHost.CreateDefaultBuilder(args)
		 .ConfigureAppConfiguration(config => {

	    	     // Import default configurations (env vars, command line args, appSettings.json etc).
		     config.UseDefaultConfigs();

		     // Load config from key vault.
		     config.AddKeyVaultSecrets("TenantId", "SubscriptionId", "OtherSecretName");
		  })
		 .ConfigureLogging((context, logging) => {
				
		     // Add logging configuration and loggers.
		     logging.AddConfiguration(context.Configuration).AddConsole().AddDebug();
		 })
		 .UseStartup<Startup>();
	...
}
```

```csharp
public class Startup
{
	private readonly ILogger<Startup> _logger;
	private readonly IConfiguration _configuration;

	// We can then grab IConfiguration from the constructor, to use in our startup file as follows:
	public Startup(IConfiguration configuration, ILogger<Startup> logger)
	{
	     _configuration = configuration;
	     _logger = logger;
	}

	public void ConfigureServices(IServiceCollection services)
	{
	     // You could bind directly to a poco class of your choice.
	     _appSettings = _configuration.BindBaseSection<AppSettings>();
		
	     // Other setting bindings...
	     var bb = BigBrother.CreateDefault(_telemetrySettings.InstrumentationKey, _telemetrySettings.InternalKey);
	     _configuration.GetSection("Telemetry").Bind(_telemetrySettings);
	     _configuration.GetSection("HttpCors").Bind(_corsSettings);
	     _configuration.GetSection("RefreshingTokenProviderSettings").Bind(_refreshingTokenProviderOptions);
	     _configuration.GetSection("Endpoints").Bind(_endpoints);
	}
	...
}
```

## How to access this package
All of the eshopworld.* packages are published to a public NuGet feed.  To consume this on your local development machine, please add the following feed to your feed sources in Visual Studio:
https://eshopworld.myget.org/F/github-dev/api/v3/index.json
 
For help setting up packages, follow this article: https://docs.microsoft.com/en-us/vsts/package/nuget/consume?view=vsts

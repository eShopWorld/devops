# Eshopworld.DevOps package

[![Build status](https://dev.azure.com/eshopworld/Github%20build/_apis/build/status/devops)](https://dev.azure.com/eshopworld/Github%20build/_build/latest?definitionId=150)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=esw.devops&metric=coverage)](https://sonarcloud.io/dashboard?id=esw.devops)

## Summary

DevOps extensions, used for application configuration setup.

## Load configurations

```csharp
public class Startup
{
   ...
   
    public void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
		// By calling the UseDefaultConfigs, settings are loaded from environment, command line args, followed by appsettings.
        builder.UseDefaultConfigs();

		...
    }
    ...
   
}
```

## Load individual secrets from Key Vault

Individual secrets can be loaded into configuration from Key Vault in the following way:

```csharp
public class Startup
{
   ...
   
    public void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
        builder.UseDefaultConfigs();

        // Pass the name of the secrets you wish to load into configuration.
        builder.AddKeyVaultSecrets(  
			"TenantId", 
			"SubscriptionId", 
			"OtherSecretName");
    }
    ...
   
}
```

NOTE: When there's a problem pulling the "KEYVAULT_URL" config or the fallback "KeyVaultInstanceName" key (which the extension method `AddKeyVaultSecrets` uses), then an exception would be thrown to the calling code. This would (and should) happen during application bootstrap (either `wehHost.ConfigureAppConfiguration` or `genericHost.ConfigureAppConfiguration`). We want this because the app wont be able to load secrets it needs to run.

## Using the loaded configuration

Take this class:

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

## How to access this package
All of the eshopworld.* packages are published to a public NuGet feed.  To consume this on your local development machine, please add the following feed to your feed sources in Visual Studio:
https://eshopworld.myget.org/F/github-dev/api/v3/index.json
 
For help setting up packages, follow this article: https://docs.microsoft.com/en-us/vsts/package/nuget/consume?view=vsts

# dfe-analytics-dotnet

This library is a port of the [DfE::Analytics gem](https://github.com/DFE-Digital/dfe-analytics) for ASP.NET Core. Currently only web request events are supported.
Applications must use ASP.NET Core 8.

## Installation

Before you can send data to BigQuery with `dfe-analytics` you'll need to setup
your Google Cloud project. See the [setup Google Cloud setup guide](https://github.com/DFE-Digital/dfe-analytics/blob/main/docs/google_cloud_bigquery_setup.md)
for instructions on how to do that.

### 1. Add the Dfe.Analytics library to your app

The package is not yet available on nuget.org so needs to be downloaded and stored in your repository.

Download the `nupkg` from the [latest release](https://github.com/DFE-Digital/dfe-analytics-dotnet/releases) and copy into a folder in your repository (e.g. `lib`).

You may need to add a `nuget.config` file to add your package location as a package source:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="lib" value="lib" protocolVersion="3" />
  </packageSources>
</configuration>
```

Install the package into your ASP.NET Core project:
```
dotnet add package DfE.Analytics
```

In your application's entry point (e.g. `Program.cs`) first add services:

```cs
builder.Services.AddDfeAnalytics()
    .AddAspNetCoreIntegration();
```

then add the middleware:

```cs
app.UseDfeAnalytics();
```

It's recommended to place this middleware after any health check or Swagger middleware.


### 2. Get an API JSON key

Depending on how your app environments are setup, we recommend you use the
service account created for the `development` environment on your localhost to
test integration with BigQuery. This requires that your project is setup in
Google Cloud as per the instructions above.

1. Access the `development` service account you previously set up
1. Go to the keys tab, click on "Add key" > "Create new key"
1. Create a JSON private key. This file will be downloaded to your local system.

The library expects to have this JSON key available within your application's `IConfiguration` under the `DfeAnalytics:CredentialsJson` key.
For local development you should use User Secrets to store the key.
For deployed environments you can set the `DfeAnalytics__CredentialsJson` environment variable.

Alternatively you can configure a `BigQueryClient` directly:
```cs
builder.Services.AddDfeAnalytics(options =>
{
    options.BigQueryClient = ...;
});
```


### 3. Configure the middleware

As well as the `CredentialsJson` above, the library will look for the following additional BigQuery configuration:

| Configuration key          | Description                                                                                                                          |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------|
| `DfeAnalytics:DatasetId`   | *REQUIRED* The BigQuery dataset to write events to.                                                                                  |
| `DfeAnalytics:Environment` | *REQUIRED* The environment name (populates the `environment` field in the event).                                                    |
| `DfeAnalytics:Namespace`   | The application's namespace (populates the `namespace` field in the event.) By default the application's assembly name will be used. |
| `DfeAnalytics:TableId`     | The BigQuery table name to write events to. Defaults to `events`.                                                                    |

The configuration above can also be set in code:
```cs
builder.Services.AddDfeAnalytics(options =>
{
    options.DatasetId = ...;
});
```

The ASP.NET Core integration has the following configuration options:

| Configuration key                         | Description                                                                                                                     |
|-------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|
| `DfeAnalytics:AspNetCore:UserIdClaimType` | The claim type that contains the user's ID. Defaults to `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`. |

The configuration about can also be set in code:
```cs
builder.Services.AddDfeAnalytics()
    .AddAspNetCoreIntegration(options =>
    {
        options.UserIdClaimType = ...;
    });
```


## Advanced usage

### Adding tags or data to the event

```cs
using DfE.Analyics.AspNetCore;

//...
httpContext.GetWebRequestEvent()?.AddTag("tag1", "tag2");
httpContext.GetWebRequestEvent()?.AddData("key", "value1", "value2");
```


### Ignoring the event

Should you want to prevent the library from sending an event to BigQuery for a particular given request you can call `IgnoreWebRequestEvent()` on the `HttpContext`:

```cs
using Dfe.Analytics.AspNetCore;

//...
httpContext.IgnoreWebRequestEvent();
```


### Modifying web request events

If you want to modify web request events before they are sent, you can create a class and implement `IWebRequestEventEnricher`.
The `EnrichEvent()` method will be called before each event is sent to BigQuery.
You can add multiple `IWebRequestEventEnricher`s to the application.

```cs
using Dfe.Analytics.AspNetCore;

public class MyEnricher : IWebRequestEventEnricher
{
    public Task EnrichEvent(EnrichWebRequestEventContext context)
    {
        context.Event.AddData("Key", "Value");
        return Task.CompletedTask;
    }
}

//...
builder.Services.AddSingleton<IWebRequestEventEnricher, MyEnricher>();
```

# dfe-analytics-dotnet

This library is a port of the [DfE::Analytics gem](https://github.com/DFE-Digital/dfe-analytics) for .NET.
It supports capturing web request events in ASP.NET Core applications and capturing Postgres database changes via Airbyte in Entity Framework Core applications.

Applications must use .NET 8, .NET 9 or .NET 10 and the corresponding ASP.NET Core and/or Entity Framework core libraries.


## Pre-requisites

Before you can send data to BigQuery you'll need to setup your Google Cloud project.
See the [setup Google Cloud setup guide](https://github.com/DFE-Digital/dfe-analytics/blob/main/docs/google_cloud_bigquery_setup.md)
for instructions on how to do that.


## Capture database changes with Airbyte and Entity Framework Core

The `DfeAnalytics.EFCore` package provides integration with Entity Framework Core to capture database changes and send them to BigQuery via Airbyte.

Only Postgres databases are supported and tables must be defined in a single `DbContext` type.

Applications should use the [Airbyte Terraform module](https://github.com/DFE-Digital/terraform-modules/tree/main/aks/airbyte).

### Installation & configuration

#### 1. Add DfeAnalytics.EFCore to your app

Install the package into your Entity Framework Core application:
```
dotnet add package DfeAnalytics.EFCore
```

> [!IMPORTANT]
> This must be added to the project that contains your `DbContext` class.

Ensure you have a reference to `Microsoft.EntityFrameworkCore.Design`.

> [!IMPORTANT]
> Ensure the `<PackageReference>` does _not_ have `PrivateAssets="all"` as this will prevent the library from working.

#### 2. Specify the DbContext type in `Directory.Build.props`

Add a `Directory.Build.props` file (or update your existing one) to include the full name of your `DbContext` type and its containing assembly:

```xml
<Project>
  <PropertyGroup>
    <DfeAnalyticsDbContext>MyApp.Data.MyDbContext, MyApp.Data</DfeAnalyticsDbContext>
  </PropertyGroup>
</Project>
```

#### 3. Configure tables and columns to sync

You must explicitly opt-in for every table you want to sync to BigQuery. This configuration can be done in your `DbContext`'s `OnModelCreating` method:

```cs
using Dfe.Analytics.EFCore;

public class MyEntity
{
    public int Id { get; set; }
    public string MyProperty { get; set; }
}

public class MyDbContext : DbContext
{
    public DbSet<MyEntity> MyEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var myEntityConfiguration = modelBuilder.Entity<MyEntity>();
        // Table-level configuration
        myEntityConfiguration.IncludeInAnalyticsSync(includeAllColumns: true, hidden: false);
        // Column-level override
        myEntityConfiguration.Property(e => e.MyProperty).ConfigureAnalyticsSync(hidden: true);
    }
}
```

The same configuration can also be set within classes implementing `IEntityTypeConfiguration<T>`.

#### 4. Verify configuration file is created at publish time

If all is configured correctly, whenever any of the applications in your solution that reference the DbContext are published
a `dfe-analytics` directory should be created in the output directory.
This directory contains a `config.json` file with the configuration for your DbContext, including which tables and columns are configured to sync to BigQuery.

### Exposing the sync configuration over HTTP

If your DbContext is hosted in an ASP.NET Core application, you can expose its sync configuration over an HTTP endpoint.
This is useful for verifying at runtime which tables and columns are configured to sync to BigQuery.

Map the endpoint in your application's entry point (e.g. `Program.cs`):

```cs
using Dfe.Analytics.EFCore;

//...
app.MapDfeAnalyticsDbConfiguration<MyDbContext>();
```

By default the configuration is served as JSON from `/_dfe-analytics/db-config.json`.
You can supply a custom path if you prefer:

```cs
app.MapDfeAnalyticsDbConfiguration<MyDbContext>("/my-custom-path.json");
```

The `MyDbContext` type must be registered in the application's service provider (e.g. via `AddDbContext<MyDbContext>()`).


## Capturing web request events in ASP.NET Core

### Installation & configuration

#### 1. Add DfeAnalytics.AspNetCore to your app

Install the package into your ASP.NET Core application:
```
dotnet add package DfeAnalytics.AspNetCore
```

In your application's entry point (e.g. `Program.cs`), add services:

```cs
builder.Services.AddDfeAnalytics()
    .AddAspNetCoreIntegration();
```

then add the middleware:

```cs
app.UseDfeAnalytics();
```

It's recommended to place this middleware after any health check or Swagger middleware to prevent events being created for requests to those endpoints.


#### 2. Configure the middleware

The library will look for the following entries in your application's configuration:

| Configuration key          | Description                                                                                                                          |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------|
| `DfeAnalytics:ProjectId`   | *REQUIRED* The Google Cloud project ID.                                                                                              |
| `DfeAnalytics:DatasetId`   | *REQUIRED* The BigQuery dataset to write events to.                                                                                  |
| `DfeAnalytics:Environment` | *REQUIRED* The environment name (populates the `environment` field in the event).                                                    |
| `DfeAnalytics:Namespace`   | The application's namespace (populates the `namespace` field in the event.) By default the application's assembly name will be used. |

The configuration above can also be set in code:
```cs
builder.Services.AddDfeAnalytics(options =>
{
    options.ProjectId = ...;
});
```

The ASP.NET Core integration has the following configuration options:

| Configuration key                         | Description                                                                                                                     |
|-------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|
| `DfeAnalytics:AspNetCore:UserIdClaimType` | The claim type that contains the user's ID. Defaults to `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`. |
| `DfeAnalytics:AspNetCore:TableId`         | The BigQuery table name to write events to. Defaults to `events`.                                                               |

The configuration above can also be set in code:
```cs
builder.Services.AddDfeAnalytics()
    .AddAspNetCoreIntegration(options =>
    {
        options.UserIdClaimType = ...;
    });
```


### Advanced usage

#### Custom events

You can publish custom events by using `IEventSender`.

```cs
//IEventSender eventSender;
var customEvent = eventSender.CreateEvent("my-custom-event");
customEvent.AddData("SingleValueKey", "value");
customEvent.AddData("MultiValueKey", "value1", "value2");
await eventSender.SendEventAsync();
```


#### Adding tags or data to the event

```cs
using DfE.Analyics.AspNetCore;

//...
httpContext.GetWebRequestEvent()?.AddTag("tag1", "tag2");
httpContext.GetWebRequestEvent()?.AddData("key", "value1", "value2");
httpContext.GetWebRequestEvent()?.AddHiddenData("key", "hidden-value1", "hidden-value2");
```


#### Ignoring the event

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

## Deployment

Use the [DfE Analytics Terraform module](https://github.com/DFE-Digital/terraform-modules/tree/main/aks/dfe_analytics) for both web request and Airbyte integration.
The module has several outputs that should be added to your application's configuration.

```terraform
module "dfe_analytics" {
  source = "github.com/DFE-Digital/terraform-modules//aks/dfe_analytics"

  #...
}

module "application_configuration" {
  source = "./vendor/modules/aks//aks/job_configuration"

  config_variables = {
    DfeAnalytics__Environment = var.environment_name
    DfeAnalytics__TableId     = module.dfe_analytics[0].bigquery_table_name
    DfeAnalytics__DatasetId   = module.dfe_analytics[0].bigquery_dataset
    DfeAnalytics__ProjectId   = module.dfe_analytics[0].bigquery_project_id
  }
}
```

For Airbyte integration, use the [Airbyte Terraform module](https://github.com/DFE-Digital/terraform-modules/tree/main/aks/airbyte)
in addition to the DfE Analytics module.
Ensure the following .NET-specific configuration is set:

```terraform
module "airbyte" {
  source = "./vendor/modules/aks//aks/airbyte"

  #...

  is_rails_application         = false
  is_dotnet_application        = true
  dotnet_application_directory = "/App"
}
```

`dotnet_application_directory` must be specified if your Dockerfile does not have a `WORKDIR` set (or the `WORKDIR` is different to your application's location).

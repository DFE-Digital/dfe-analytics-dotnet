# Changelog

## Unreleased

Amended `FederatedAksAuthenticationOptions` to reduce the number of options required; `Audience` and `GenerateAccessTokenUrl` are now the only keys required.
For backwards compatibility these will be derived from the old configuration keys if they're provided.

If `CredentialsJson` is available in configuration then `Audience` and `GenerateAccessTokenUrl` will be sourced from there.

## 0.2.3

Fixed `ProjectNumber` option to read from `ProjectNumber` configuration key instead of `ProjectId`.

## 0.2.2

Adds `RestoreOriginalPathAndQueryString` and `RestoreOriginalStatusCode` properties to `DfeAnalyticsAspNetCoreOptions`.

## 0.2.1

A `IWebRequestEventEnricher` interface has been added so that applications can modify web request events before they are sent to BigQuery.

A `RateLimiter` property has been added to `DfeAnalyticsAspNetCoreOptions` to limit the rate of web request events sent to BigQuery.

## 0.2.0

Changed the library design to have a single `Dfe.Analytics` package to cover both web events and database events.

Configuration has been split over two Options types - `DfeAnalyticsOptions` and `DfeAnalyticsAspNetCore` options and there are two separate methods for configuration:
```cs
services.AddDfeAnalytics(options => ...)
    .AddAspNetCoreIntegration(options => ...);
```

ASP.NET Core integration options are pulled from a subkey in configuration - `DfeAnalytics:AspNetCore`.

A `PseudonymizeUserId` property has been added to `DfeAnalyticsAspNetCoreOptions` to control whether the user ID is pseudonymized.

The `DfEAnalytics.Event.Anonymize()` has been renamed to `Pseudonymize()` to align with the Ruby gem.

The `GetWebRequestEvent()` extension method over `Microsoft.AspNetCore.Http.HttpContext` has been amended to return `null` if the `WebRequestEventFeature` is missing instead of throwing.

A `UseFederatedAksBigQueryClientProvider()` method has been added for using Workload Managed Identity in AKS for authentication:
```cs
services.AddDfeAnalytics(...)
    .UseFederatedAksBigQueryClientProvider(options => ...);
```

An option has been added to filter out any requests that should not generate a web request event:
```cs
services.AddDfeAnalytics(...)
    .AddAspNetCoreIntegration(options => options.RequestFilter = ctx => /* filter condition */);
```


## 0.1.0

Initial release of `Dfe.Analytics.AspNetCore`.

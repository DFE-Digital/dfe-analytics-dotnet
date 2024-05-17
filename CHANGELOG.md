# Changelog

## Unreleased

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

A `IgnoreWebRequestEvent()` extension method over `Microsoft.AspNetCore.Http.HttpContext` has been added.


## 0.1.0

Initial release of `Dfe.Analytics.AspNetCore`.

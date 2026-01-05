using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.EFCore.Operations;

// ReSharper disable once InconsistentNaming
internal class DfeAnalyticsEFCoreOperations
{
    public async Task ApplyConfigurationAsync(ApplyConfigurationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var services = new ServiceCollection()
            .AddDfeAnalytics(o =>
            {
                o.DatasetId = options.DatasetId;
                o.ProjectId = options.ProjectId;
                o.CredentialsJson = options.GoogleCredentialsJson;
            })
            .ConfigureAirbyteApi(o =>
            {
                o.BaseAddress = options.AirbyteApiBaseAddress;
                o.ClientId = options.AirbyteClientId;
                o.ClientSecret = options.AirbyteClientSecret;
            })
            .AddDeploymentTools()
            .Services
            .BuildServiceProvider();

        using var scope = services.CreateScope();

        var configurationPath = options.ConfigurationFilePath;
        var configuration = DatabaseSyncConfiguration.ReadFromFile(configurationPath);

        var airbyteConnectionId = options.AirbyteConnectionId;
        var hiddenPolicyTagName = options.HiddenPolicyTagName;

        var analyticsDeployer = scope.ServiceProvider.GetRequiredService<AnalyticsDeployer>();
        await analyticsDeployer.DeployAsync(configuration, airbyteConnectionId, hiddenPolicyTagName);
    }

    public Task CreateConfigurationFileAsync(CreateConfigurationFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var dbContext = DbContextHelper.CreateDbContext(
            options.DbContextAssemblyPath,
            options.DbContextTypeName);

        var configurationProvider = new AnalyticsConfigurationProvider();
        var configuration = configurationProvider.GetConfiguration(dbContext);

        configuration.WriteToFile(options.ConfigurationFilePath);

        return Task.CompletedTask;
    }
}

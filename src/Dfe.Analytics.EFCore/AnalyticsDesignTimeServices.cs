using System.ComponentModel;
using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.EFCore;

/// <summary>
/// Registers the design-time services needed to scaffold a <see cref="DbContext"/> that uses DfE Analytics annotations.
/// </summary>
/// <remarks>
/// Projects consuming this package have a <c>DesignTimeServicesReference</c> assembly attribute pointing at this type
/// generated for them at build time, which is how EF Core's tooling discovers it.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class DfeAnalyticsDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc/>
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        serviceCollection.AddSingleton<IAnnotationCodeGenerator, DfeAnalyticsAnnotationCodeGenerator>();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class DfeAnalyticsAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies) : AnnotationCodeGenerator(dependencies)
{
    public override IEnumerable<IAnnotation> FilterIgnoredAnnotations(IEnumerable<IAnnotation> annotations) =>
        base.FilterIgnoredAnnotations(annotations)
            .Where(a => !AnnotationKeys.All.Contains(a.Name));
}

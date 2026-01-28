using System.ComponentModel;
using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Dfe.Analytics.EFCore;

[EditorBrowsable(EditorBrowsableState.Never)]
public class DfeAnalyticsAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies) : AnnotationCodeGenerator(dependencies)
{
    public override IEnumerable<IAnnotation> FilterIgnoredAnnotations(IEnumerable<IAnnotation> annotations) =>
        base.FilterIgnoredAnnotations(annotations)
            .Where(a => !AnnotationKeys.All.Contains(a.Name));
}

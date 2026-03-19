using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dfe.Analytics.EFCore.Cli;

internal static class DbContextHelper
{
    public static DbContext CreateDbContext(string dbContextTypeName)
    {
        ArgumentNullException.ThrowIfNull(dbContextTypeName);

        var dbContextType = Type.GetType(dbContextTypeName) ??
            throw new InvalidOperationException($"Could not load '{dbContextTypeName}'.");

        return DbContextActivator.CreateInstance(dbContextType);
    }
}

namespace Dfe.Analytics.EFCore.Tests;

public class TestEntity
{
    public int TestEntityId { get; set; }
    public string? Name { get; set; }
    public string? Ignored { get; set; }
    public DateOnly DateOfBirth { get; set; }
}

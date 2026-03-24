namespace Dfe.Analytics.EFCore.Tests;

public class TestEntity
{
    public int TestEntityId { get; set; }
    public string? Name { get; set; }
    public string? Ignored { get; set; }
    public DateOnly DateOfBirth { get; set; }
}

public abstract class BaseEntity
{
    public int Id { get; set; }
    public string? BaseProperty { get; set; }
    public required string Discriminator { get; init; }
}

public class DerivedEntity1 : BaseEntity
{
    public DerivedEntity1()
    {
        Discriminator = nameof(DerivedEntity1);
    }

    public string? DerivedProperty1 { get; set; }
}

public class DerivedEntity2 : BaseEntity
{
    public DerivedEntity2()
    {
        Discriminator = nameof(DerivedEntity2);
    }

    public string? DerivedProperty2 { get; set; }
}

namespace Aganox.Lms.Api.Tests;

public sealed class LmsApiFixture : IDisposable
{
    public LmsApiFactory Factory { get; } = new();

    public void Dispose() => Factory.Dispose();
}

[CollectionDefinition("LmsApi")]
public sealed class LmsApiCollection : ICollectionFixture<LmsApiFixture>;

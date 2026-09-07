namespace Tests.Fixtures;

[CollectionDefinition(Nome)]
public sealed class ColecaoIntegracao : ICollectionFixture<ApiFactory>
{
    public const string Nome = "Integracao";
}

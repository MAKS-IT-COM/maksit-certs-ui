using Xunit;

namespace MaksIT.CertsUI.Tests.Infrastructure;

[CollectionDefinition("postgres-cache")]
public class PostgresCacheCollection : ICollectionFixture<PostgresCacheFixture>;

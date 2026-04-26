using FluentMigrator.Runner.VersionTableInfo;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>FluentMigrator version table: snake_case <c>public.version_info</c> (table and columns) for PostgreSQL consistency.</summary>
public sealed class CertsFluentMigratorVersionTableMetaData : IVersionTableMetaData {

  public const string Table = "version_info";

  public const string VersionColumn = "version";

  public const string AppliedOnColumn = "applied_on";

  public const string DescriptionColumn = "description";

  public const string UniqueIndex = "uc_version";

  public bool OwnsSchema => true;

  public string SchemaName => "public";

  public string TableName => Table;

  public string ColumnName => VersionColumn;

  public string DescriptionColumnName => DescriptionColumn;

  public string UniqueIndexName => UniqueIndex;

  public string AppliedOnColumnName => AppliedOnColumn;

  public bool CreateWithPrimaryKey => false;
}

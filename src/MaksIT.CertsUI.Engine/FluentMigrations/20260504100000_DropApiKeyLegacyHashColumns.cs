using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Drops legacy hash-at-rest columns on <c>api_keys</c>; plaintext/API material lives in <c>api_key</c>.
/// Handles FluentMigrator PascalCase names and snake_case names (if an older <c>SnakeCaseIdentityColumns</c> run renamed them).
/// </summary>
[Migration(20260504100000)]
public class DropApiKeyLegacyHashColumns : Migration {

  public override void Up() {
    Execute.Sql("""
      DO $$
      DECLARE
        col text;
      BEGIN
        FOR col IN
          SELECT a.attname::text
          FROM pg_attribute a
          INNER JOIN pg_class c ON c.oid = a.attrelid
          INNER JOIN pg_namespace n ON n.oid = c.relnamespace
          WHERE n.nspname = 'public'
            AND c.relname = 'api_keys'
            AND a.attnum > 0
            AND NOT a.attisdropped
            AND a.attname IN ('KeySalt', 'key_salt', 'KeyHashHex', 'key_hash_hex')
        LOOP
          EXECUTE format('ALTER TABLE %I.%I DROP COLUMN %I CASCADE', 'public', 'api_keys', col);
        END LOOP;
      END $$;
      """);
  }

  public override void Down() {
    // Forward-only per expand-only migration policy.
  }
}

using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Renames identity table columns to snake_case names used by <see cref="MaksIT.CertsUI.Engine.Data.CertsUILinq2DbMapping"/>.
/// Uses <c>pg_catalog</c> so quoted FluentMigrator identifiers (e.g. <c>"Id"</c>) are detected reliably on PostgreSQL.
/// </summary>
[Migration(20260503100000)]
public class SnakeCaseIdentityColumns : Migration {

  public override void Up() {
    Execute.Sql("""
      CREATE OR REPLACE FUNCTION certs_rename_if_exists(p_table text, p_old text, p_new text)
      RETURNS void LANGUAGE plpgsql AS $$
      BEGIN
        IF EXISTS (
          SELECT 1 FROM pg_attribute a
          INNER JOIN pg_class c ON c.oid = a.attrelid
          INNER JOIN pg_namespace n ON n.oid = c.relnamespace
          WHERE n.nspname = 'public'
            AND c.relname = p_table
            AND a.attname = p_old
            AND a.attnum > 0
            AND NOT a.attisdropped
        ) AND NOT EXISTS (
          SELECT 1 FROM pg_attribute a
          INNER JOIN pg_class c ON c.oid = a.attrelid
          INNER JOIN pg_namespace n ON n.oid = c.relnamespace
          WHERE n.nspname = 'public'
            AND c.relname = p_table
            AND a.attname = p_new
            AND a.attnum > 0
            AND NOT a.attisdropped
        ) THEN
          EXECUTE format('ALTER TABLE %I.%I RENAME COLUMN %I TO %I', 'public', p_table, p_old, p_new);
        END IF;
      END;
      $$;

      SELECT certs_rename_if_exists('users', 'Id', 'id');
      SELECT certs_rename_if_exists('users', 'Name', 'username');
      SELECT certs_rename_if_exists('users', 'Salt', 'password_salt');
      SELECT certs_rename_if_exists('users', 'Hash', 'password_hash');
      SELECT certs_rename_if_exists('users', 'LastLoginUtc', 'last_login');
      SELECT certs_rename_if_exists('users', 'IsActive', 'is_active');
      SELECT certs_rename_if_exists('users', 'TwoFactorSharedKey', 'two_factor_shared_key');
      SELECT certs_rename_if_exists('users', 'name', 'username');
      SELECT certs_rename_if_exists('users', 'salt', 'password_salt');
      SELECT certs_rename_if_exists('users', 'hash', 'password_hash');
      SELECT certs_rename_if_exists('users', 'lastloginutc', 'last_login');
      SELECT certs_rename_if_exists('users', 'isactive', 'is_active');
      SELECT certs_rename_if_exists('users', 'twofactorsharedkey', 'two_factor_shared_key');

      SELECT certs_rename_if_exists('jwt_tokens', 'Id', 'id');
      SELECT certs_rename_if_exists('jwt_tokens', 'UserId', 'user_id');
      SELECT certs_rename_if_exists('jwt_tokens', 'Token', 'token');
      SELECT certs_rename_if_exists('jwt_tokens', 'RefreshToken', 'refresh_token');
      SELECT certs_rename_if_exists('jwt_tokens', 'IssuedAt', 'issued_at');
      SELECT certs_rename_if_exists('jwt_tokens', 'ExpiresAt', 'expires_at');
      SELECT certs_rename_if_exists('jwt_tokens', 'RefreshTokenExpiresAt', 'refresh_token_expires_at');
      SELECT certs_rename_if_exists('jwt_tokens', 'IsRevoked', 'is_revoked');
      SELECT certs_rename_if_exists('jwt_tokens', 'userid', 'user_id');

      SELECT certs_rename_if_exists('two_factor_recovery_codes', 'Id', 'id');
      SELECT certs_rename_if_exists('two_factor_recovery_codes', 'UserId', 'user_id');
      SELECT certs_rename_if_exists('two_factor_recovery_codes', 'Salt', 'salt');
      SELECT certs_rename_if_exists('two_factor_recovery_codes', 'Hash', 'hash');
      SELECT certs_rename_if_exists('two_factor_recovery_codes', 'IsUsed', 'is_used');

      SELECT certs_rename_if_exists('user_authorizations', 'UserId', 'user_id');
      SELECT certs_rename_if_exists('user_authorizations', 'IsGlobalAdmin', 'is_global_admin');

      SELECT certs_rename_if_exists('user_entity_scopes', 'Id', 'id');
      SELECT certs_rename_if_exists('user_entity_scopes', 'UserId', 'user_id');
      SELECT certs_rename_if_exists('user_entity_scopes', 'EntityId', 'entity_id');
      SELECT certs_rename_if_exists('user_entity_scopes', 'EntityType', 'entity_type');
      SELECT certs_rename_if_exists('user_entity_scopes', 'Scope', 'scope');

      SELECT certs_rename_if_exists('api_keys', 'Id', 'id');
      SELECT certs_rename_if_exists('api_keys', 'Description', 'description');
      SELECT certs_rename_if_exists('api_keys', 'CreatedAtUtc', 'created_at');
      SELECT certs_rename_if_exists('api_keys', 'RevokedAtUtc', 'revoked_at');
      SELECT certs_rename_if_exists('api_keys', 'ExpiresAtUtc', 'expires_at');

      SELECT certs_rename_if_exists('api_key_entity_scopes', 'Id', 'id');
      SELECT certs_rename_if_exists('api_key_entity_scopes', 'ApiKeyId', 'api_key_id');
      SELECT certs_rename_if_exists('api_key_entity_scopes', 'EntityId', 'entity_id');
      SELECT certs_rename_if_exists('api_key_entity_scopes', 'EntityType', 'entity_type');
      SELECT certs_rename_if_exists('api_key_entity_scopes', 'Scope', 'scope');

      DROP FUNCTION certs_rename_if_exists(text, text, text);
      """);
  }

  public override void Down() {
    // Forward-only per expand-only migration policy.
  }
}

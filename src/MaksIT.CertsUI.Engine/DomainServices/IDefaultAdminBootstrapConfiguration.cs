namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>First-boot default operator account (Vault: <c>VaultEngineConfiguration.Admin</c>).</summary>
public interface IDefaultAdminBootstrapConfiguration {
  string Username { get; }
  string Password { get; }
}

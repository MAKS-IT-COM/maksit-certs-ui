using MaksIT.HAMode.Abstractions;

namespace MaksIT.CertsUI.Engine.Infrastructure;

public sealed class CertsRuntimeLeaseConnectionStringProvider(
  ICertsEngineConfiguration config
) : IRuntimeLeaseConnectionStringProvider {
  public string ConnectionString => config.ConnectionString;
}

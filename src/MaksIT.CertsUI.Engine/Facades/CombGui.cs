using MaksIT.Core.Comb;

namespace MaksIT.CertsUI.Engine.Facades;

/// <summary>COMB GUID helpers for PostgreSQL-ordered identifiers.</summary>
public static class CombGui {
  public static Guid GenerateCombGuid() =>
    CombGuidGenerator.CreateCombGuid(DateTime.UtcNow, CombGuidType.PostgreSql);
}

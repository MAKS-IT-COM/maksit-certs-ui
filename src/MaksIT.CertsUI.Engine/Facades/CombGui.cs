using MaksIT.Core.Comb;

namespace MaksIT.CertsUI.Engine.Facades;
public static class CombGui {
  /// <summary>
  /// Generates a new COMB GUID by combining a randomly generated GUID with the current UTC timestamp,
  /// nel formato compatibile PostgreSQL (timestamp in bytes 0–7).
  /// </summary>
  /// <returns>A new COMB GUID containing a random GUID and the current UTC timestamp.</returns>
  public static Guid GenerateCombGuid() {
    // usa overload che genera un GUID casuale e lo combina con DateTime.UtcNow
    return CombGuidGenerator.CreateCombGuid(DateTime.UtcNow, CombGuidType.PostgreSql);
  }

  /// <summary>
  /// Generates a new COMB GUID by combining a specified GUID with the current UTC timestamp,
  /// nel formato compatibile PostgreSQL (timestamp in bytes 0–7).
  /// </summary>
  /// <param name="baseGuid">The base GUID to combine with the current UTC timestamp.</param>
  /// <returns>A new COMB GUID combining the provided GUID with the current UTC timestamp.</returns>
  public static Guid GenerateCombGuid(Guid baseGuid) {
    return CombGuidGenerator.CreateCombGuid(baseGuid, CombGuidType.PostgreSql);
  }

  /// <summary>
  /// Generates a new COMB GUID by combining a randomly generated GUID with a specified timestamp,
  /// nel formato compatibile PostgreSQL (timestamp in bytes 0–7).
  /// </summary>
  /// <param name="timestamp">The timestamp to embed in the GUID.</param>
  /// <returns>A new COMB GUID combining a random GUID with the specified timestamp.</returns>
  public static Guid GenerateCombGuid(DateTime timestamp) {
    return CombGuidGenerator.CreateCombGuid(timestamp, CombGuidType.PostgreSql);
  }

  /// <summary>
  /// Generates a new COMB GUID by combining a specified GUID and timestamp,
  /// nel formato compatibile PostgreSQL (timestamp in bytes 0–7).
  /// </summary>
  /// <param name="baseGuid">The base GUID to combine with the provided timestamp.</param>
  /// <param name="timestamp">The timestamp to embed in the GUID.</param>
  /// <returns>A new COMB GUID combining the provided GUID and timestamp.</returns>
  public static Guid GenerateCombGuid(Guid baseGuid, DateTime timestamp) {
    return CombGuidGenerator.CreateCombGuid(baseGuid, timestamp, CombGuidType.PostgreSql);
  }
}


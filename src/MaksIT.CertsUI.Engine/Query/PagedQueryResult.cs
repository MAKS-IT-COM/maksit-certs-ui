namespace MaksIT.CertsUI.Engine.Query;

/// <summary>
/// Paged list payload for engine queries. The Web API maps this to API <c>PagedResponse&lt;T&gt;</c> models.
/// </summary>
public sealed record PagedQueryResult<T>(
  IReadOnlyList<T> Data,
  int TotalRecords,
  int PageNumber,
  int PageSize
);

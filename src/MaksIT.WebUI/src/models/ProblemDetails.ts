/**
 * JSON shape for `MaksIT.Results.Mvc.ProblemDetails` (RFC 7807).
 *
 * `Extensions` is `[JsonExtensionData]` in the library: extra members serialize as **sibling**
 * properties on the same object (`traceId`, custom `id`, etc.), not under a nested `extensions` key.
 *
 * @see `MaksIT.Results.Mvc.ProblemDetails` in the **maksit-results** repository (same contract as the **MaksIT.Results** NuGet package).
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Validation failures when the API puts `errors` in extension data (ValidationProblemDetails-style). */
  errors?: Record<string, string[]>;
  /** Often emitted by ASP.NET (`traceId` in extension data). */
  traceId?: string;
  /** Any other extension member the server attaches (correlation id, etc.). */
  [key: string]: unknown;
}

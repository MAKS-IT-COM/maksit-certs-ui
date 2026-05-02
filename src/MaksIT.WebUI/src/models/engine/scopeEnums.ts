/** Mirrors <c>MaksIT.CertsUI.Engine.ScopeEntityType</c> (JSON: numeric). */
export enum ScopeEntityType {
  Identity = 0,
  ApiKey = 1,
}

/** Mirrors <c>MaksIT.CertsUI.Engine.ScopePermission</c> flags (JSON: numeric). */
export enum ScopePermission {
  None = 0,
  Read = 1 << 0,
  Write = 1 << 1,
  Delete = 1 << 2,
  Create = 1 << 3,
}

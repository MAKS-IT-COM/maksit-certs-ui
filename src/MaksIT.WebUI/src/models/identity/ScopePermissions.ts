export enum ScopePermission {
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Delete = 1 << 2,
    Create = 1 << 3,

    ReadIdentities = 1 << 4,
    WriteIdentities = 1 << 5,
    DeleteIdentities = 1 << 6,
    CreateIdentities = 1 << 7,

    ReadApiKeys = 1 << 8,
    WriteApiKeys = 1 << 9,
    DeleteApiKeys = 1 << 10,
    CreateApiKeys = 1 << 11,
}





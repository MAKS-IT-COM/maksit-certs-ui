# PATCH Delta Handling – Backend & Frontend Reference

This document is the **single reference** for how PATCH payloads (deltas) are structured and interpreted so backend (BE) and frontend (FE) stay consistent. It is **aligned** with the same contract as **MaksIT-Vault** [`assets/docs/PATCH_DELTA_REFERENCE.md`](../../maksit-vault/assets/docs/PATCH_DELTA_REFERENCE.md) (Core rules, collection-item key, and `deepDelta` behavior) when both repos sit side by side under the same parent folder; otherwise open that path in your Vault clone. Vault adds RBAC-specific collections (`entityScopes`, `versions`); **MaksIT-CertsUI** uses the same **MaksIT.Core** patch model with a **hostnames** collection on account PATCH.

**Audience:** Backend (C# / ASP.NET) and Frontend (TypeScript / React) developers.

---

## TL;DR (start here)

- **PATCH** sends only **what changed**, not the full resource. Each change is tagged with an **operation** (set, remove, add item, remove item).
- **Root fields** (e.g. `description`, `contact`): send new value + `operations["fieldName"] = SetField` or `RemoveField`.
- **Collections** (e.g. `hostnames`): **do not** replace the whole array when the API is “patchable collection” semantics. Send **per-item** changes: each added item has `operations.collectionItemOperation = AddToCollection`, each removed item has `RemoveFromCollection`, and changed items send identity and changed fields.
- **Frontend (Certs WebUI):** For **Edit Account**, use  
  `deepDelta(formState, backupState, { arrays: { hostnames: { identityKey: 'hostname', idFieldKey: 'hostname' } } })`  
  so hostname rows are itemized (including “add first hostname”) and stay in sync with the backend.
- **Backend:** Use `TryGetOperation(Constants.CollectionItemOperation, out var op)` on each collection item; never treat root `Operations["hostnames"] = SetField` as “replace all” if the API follows per-item patch semantics.

---

## 1. Core contract (MaksIT.Core)

The following come from **MaksIT.Core** and must be respected by all consumers.

### 1.1 PatchOperation enum

| Value | Integer | Meaning |
|-------|---------|--------|
| `SetField` | 0 | Set or replace a scalar or root-level value |
| `RemoveField` | 1 | Set a field to null |
| `AddToCollection` | 2 | Add an item to a collection (used on **collection items**, not root) |
| `RemoveFromCollection` | 3 | Remove an item from a collection (used on **collection items**, not root) |

- **Source:** `MaksIT.Core.Webapi.Models.PatchOperation`
- **FE mirror:** `PatchOperation` enum in WebUI (`src/MaksIT.WebUI/src/models/PatchOperation.ts`) must keep the same numeric values for JSON serialization.

### 1.2 PatchRequestModelBase

- **Operations:** `Dictionary<string, PatchOperation>?` (C#) / `{ [key: string]: PatchOperation }` (TS).
- **Lookup:** Case-insensitive by **property name** (e.g. `"hostnames"`, `"description"`).
- **Usage:**
  - **Root level:** `Operations["propertyName"]` describes the operation for that property (e.g. `SetField` for a changed field, `RemoveField` for null).
  - **Collection items:** Each element of a collection property is itself a patch model; it uses a **reserved key** (see below) to indicate add/remove/update for that item.

### 1.3 Collection item operation key

For **elements inside a collection property** (e.g. each item in `hostnames`), the operation is stored under a fixed key so the backend can distinguish “add/remove this item” from “update fields of this item”.

- **Key name:** `collectionItemOperation`
- **BE:** `Constants.CollectionItemOperation` (same string across MaksIT services when aligned with Core).
- **FE:** `COLLECTION_ITEM_OPERATION` in `src/MaksIT.WebUI/src/models/PatchOperation.ts`; same string in payloads and in `deepDelta`. Keep in sync with backend.

**Allowed values for collection items:** `AddToCollection` (2), `RemoveFromCollection` (3). For in-place updates (same item, changed fields), the item typically has an `id` and no `collectionItemOperation`, or field-level changes follow your API’s semantics.

---

## 2. Backend (BE) rules

### 2.1 Root-level properties

- Read `request.TryGetOperation(propertyName, out var op)`.
- If `op == SetField`: apply the new value from the request for that property.
- If `op == RemoveField`: set the property to null (or clear it) where applicable.
- If the property is not in `Operations` but is present with a value, treat as optional direct assignment (or ignore), depending on your API convention; for strict PATCH, prefer requiring operations for changed fields.

### 2.2 Collection properties (e.g. hostnames)

- **Rule:** Collection properties are patched **only via per-item operations** when the API follows patchable-collection semantics. The backend does **not** treat root `Operations["hostnames"] = SetField` as “replace the entire collection” unless explicitly documented for that endpoint.
- For each item in the collection payload:
  1. Call `item.TryGetOperation(Constants.CollectionItemOperation, out var collectionOp)` (or the agreed constant).
  2. If `collectionOp == AddToCollection`: add the item to the collection (merge by id if present, or append).
  3. If `collectionOp == RemoveFromCollection`: remove the item (by `item.Id` or by matching key fields such as hostname string).
  4. If no `collectionItemOperation` but the item is identifiable: treat as **in-place update**.
  5. If no `collectionItemOperation` and the item cannot be identified: **do not add** ambiguous items; the FE must send `AddToCollection` for new rows when required by your rules.

### 2.3 Consistency checklist (BE)

- [ ] Use the same `CollectionItemOperation` key as the FE (see Constants / Core).
- [ ] Do not rely on root-level `SetField` for patchable collections to mean “replace all”; use per-item add/remove/update only (unless documented otherwise).
- [ ] For add: require `TryGetOperation(CollectionItemOperation) == AddToCollection` (or equivalent) for new items where applicable.
- [ ] For remove: use `RemoveFromCollection` and/or identity fields agreed with the FE.

---

## 3. Frontend (FE) rules

### 3.1 Building the delta (deepDelta)

- **Scalar / root fields:** Emit the new value and set `operations[propertyName] = SetField` or `RemoveField` as appropriate.
- **Primitive arrays:** Emit the full array and `operations[propertyName] = SetField` (full replace).
- **Object arrays that are “patchable collections”:** Must always produce **itemized deltas** when configured with an **array policy** (identity key / id field):
  - Each added item must have `operations.collectionItemOperation = AddToCollection`.
  - Each removed item must have `operations.collectionItemOperation = RemoveFromCollection`.
  - Updated items carry changed fields and identity; see `deepDelta` implementation in `src/MaksIT.WebUI/src/functions/deep/deepDelta.ts`.

### 3.2 Patchable collections – identity requirement

For the backend to interpret add/remove/update correctly, each collection **item** must be identifiable:

- **Existing items:** Use `id` from the server when present.
- **New items:** May have no server `id`; the FE must pass an **array policy** with `identityKey` / `idFieldKey` so the delta stays itemized (e.g. hostname string as stable key for hostnames).

### 3.3 Shared array policies (this repository)

Unlike MaksIT-Vault, **MaksIT-CertsUI** does **not** ship `patchCollectionPolicies.ts`. The **Edit Account** form passes an **inline** policy for the `hostnames` collection:

| Collection | Policy (inline) | Used in |
|------------|-----------------|---------|
| `hostnames` | `{ identityKey: 'hostname', idFieldKey: 'hostname' }` | `EditAccount.tsx` |

Example:

```ts
deepDelta(fromFormState, fromBackupState, {
  arrays: {
    hostnames: {
      identityKey: 'hostname',
      idFieldKey: 'hostname',
    },
  },
})
```

For **Vault**-style shared policies (`ENTITY_SCOPES_ARRAY_POLICY`, `VERSIONS_ARRAY_POLICY`), see the Vault repo and forms that edit `entityScopes` / `versions`.

### 3.4 Consistency checklist (FE)

- [ ] Use the same `PatchOperation` numeric values as Core (SetField 0, RemoveField 1, AddToCollection 2, RemoveFromCollection 3).
- [ ] Use `COLLECTION_ITEM_OPERATION` from `PatchOperation.ts` (same string as backend `Constants.CollectionItemOperation`).
- [ ] For account `hostnames`, pass the `hostnames` array policy in `deepDelta` so the delta is **itemized**, not a blind full-replace of the array.
- [ ] New items must have `operations.collectionItemOperation = AddToCollection` when the backend expects it.

---

## 4. Payload examples

### 4.1 Root-level SetField (scalar)

```json
{
  "description": "Updated",
  "operations": {
    "description": 0
  }
}
```

`0` = SetField.

### 4.2 Root-level RemoveField (clear optional field)

```json
{
  "operations": { "someOptionalField": 1 }
}
```

`1` = RemoveField.

### 4.3 Root-level SetField (primitive array – full replace)

```json
{
  "tags": ["a", "b", "c"],
  "operations": {
    "tags": 0
  }
}
```

### 4.4 Collection property – itemized (add items)

Example shape for new hostname rows (numeric ops match `PatchOperation` enum):

```json
{
  "hostnames": [
    {
      "hostname": "api.example.com",
      "isDisabled": false,
      "operations": { "collectionItemOperation": 2 }
    }
  ]
}
```

`2` = AddToCollection.

### 4.5 Collection property – remove item

```json
{
  "hostnames": [
    {
      "hostname": "old.example.com",
      "operations": { "collectionItemOperation": 3 }
    }
  ]
}
```

`3` = RemoveFromCollection (identity may be `hostname` or server `id` depending on API).

### 4.6 Collection property – in-place update

Item exists; fields change; no `collectionItemOperation` on the item (or only nested field operations per your model).

---

## 5. Quick reference

| Aspect | Backend | Frontend |
|--------|---------|----------|
| Operation enum | `PatchOperation` (Core) | `PatchOperation` (same values 0–3) |
| Root operations | `TryGetOperation(propertyName, out op)` | `operations[propertyName] = op` |
| Collection item key | `Constants.CollectionItemOperation` (`"collectionItemOperation"`) | `COLLECTION_ITEM_OPERATION` in payload and deepDelta |
| New collection item | Require `AddToCollection` on item when applicable | Send `operations.collectionItemOperation: 2` for new items |
| Certs account hostnames | Per-item patch semantics | `deepDelta` + `hostnames` array policy in `EditAccount.tsx` |

---

## 6. Related docs

- **MaksIT.Core:** `PatchOperation`, `PatchRequestModelBase` (README / XML).
- **Aligned reference (RBAC / Vault collections):** [MaksIT-Vault `PATCH_DELTA_REFERENCE.md`](../../maksit-vault/assets/docs/PATCH_DELTA_REFERENCE.md) — same Core rules; extra sections for `entityScopes` and `versions`.

---

## 7. Current implementation vs reference (MaksIT-CertsUI)

### 7.1 Frontend (FE)

**Checked:** `deepDelta` usage in `src/MaksIT.WebUI/src/forms/EditAccount.tsx` for account PATCH; inline `hostnames` array policy; `COLLECTION_ITEM_OPERATION` in `PatchOperation.ts` and usage in `deepDelta.ts`.

- **Safe:** Hostname rows use `identityKey` / `idFieldKey` so itemized deltas include `AddToCollection` / `RemoveFromCollection` where appropriate.
- **Consistent:** Same `PatchOperation` values and collection-item key string as Core.

**FE summary:** Follows the shared reference; scope is **account + hostnames** (no Vault RBAC collections in this product).

### 7.2 Backend (BE)

**Maintainers:** Confirm the account PATCH handler in MaksIT.CertsUI WebAPI applies the same per-item rules (`TryGetOperation(CollectionItemOperation, ...)`) for `hostnames` as described in sections 2.2 and 3.

### 7.3 Gaps and maintenance

| Topic | Status | Note |
|-------|--------|------|
| Shared policies file | N/A in Certs | Inline policy in `EditAccount.tsx`; Vault uses `patchCollectionPolicies.ts` for RBAC collections. |
| New forms with patchable collections | **Ongoing** | When adding a form that patches a collection, pass the correct `arrays: { key: policy }` to `deepDelta`. |

---

*Last updated: 2026-04-12*

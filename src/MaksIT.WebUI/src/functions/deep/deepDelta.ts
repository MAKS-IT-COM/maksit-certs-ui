import { PatchOperation } from '../../models/PatchOperation.js'
import { deepCopy } from './deepCopy.js'
import { deepEqual } from './deepEqual.js'

type IdLike = string | number | null | undefined

export type Identifiable<I extends string | number = string | number> = {
  id?: I | null
}

type OperationBag<K extends string = string> = {
  operations?: Partial<Record<K | 'collectionItemOperation', PatchOperation>>
}

type EnsureId<T extends Identifiable> = { id?: T['id'] }

type PlainObject = Record<string, unknown>

type DeltaArrayItem<T extends Identifiable> = Partial<T> & EnsureId<T> & OperationBag

/**
 * Policy that controls how object arrays behave.
 * 
 * - Arrays with identifiable items (id or identityKey) get per-item Add/Remove/Update logic.
 * - Arrays without identity fall back to "full replace" semantics.
 */
export type ArrayPolicy = {
  /** Name of the "root" field that implies re-parenting (e.g. 'organizationId') */
  rootKey?: string

  /** Child array field names to process on re-parenting (e.g. ['applicationRoles']) */
  childArrayKeys?: string[]

  /** If true, children are cleared on root change (default TRUE) */
  dropChildrenOnRootChange?: boolean

  /** Name of the role field (default 'role') */
  roleFieldKey?: string

  /** If true, when role becomes null the entire item is removed (default TRUE) */
  deleteItemWhenRoleRemoved?: boolean

  /**
   * Stable identity for items that do not have an `id`.
   * Can be:
   * - a property name (e.g. "hostname")
   * - a function that extracts a unique value
   *
   * Without identityKey AND without item.id, the array falls back to full replace.
   */
  identityKey?: string | ((item: Record<string, unknown>) => string | number)
}

export type DeepDeltaOptions<T> = {
  /**
   * Optional per-array rules.
   * Example:
   * {
   *   hostnames: { identityKey: "hostname" }
   * }
   */
  arrays?: Partial<Record<Extract<keyof T, string>, ArrayPolicy>>
}

/**
 * Delta<T> represents:
 * - T fields that changed (primitives, objects, arrays)
 * - "operations" dictionary describing what type of change (SetField, RemoveField, AddToCollection, etc.)
 * - For primitive arrays: delta contains the full new array + SetField.
 * - For identifiable object arrays: delta contains per-item changes.
 */
export type Delta<T> =
  Partial<{
    [K in keyof T]:
      T[K] extends (infer U)[]
        ? (U extends object
            ? DeltaArrayItem<(U & Identifiable)>[] // object arrays → itemized
            : U[])                                  // primitive arrays → full array
        : T[K] extends object
          ? Delta<T[K] & OperationBag<Extract<keyof T, string>>>
          : T[K]
  }> & OperationBag<Extract<keyof T, string>>

/** Safe index to avoid TS2536 when addressing dynamic keys */
const getArrayPolicy = <T>(options: DeepDeltaOptions<T> | undefined, key: string): ArrayPolicy | undefined => {
  const arrays = options?.arrays as Partial<Record<string, ArrayPolicy>> | undefined
  return arrays?.[key]
}

const isPlainObject = (value: unknown): value is PlainObject =>
  typeof value === 'object' && value !== null && !Array.isArray(value)

/**
 * Computes a deep "delta" object between formState and backupState.
 * 
 * Rules:
 * - Primitive fields → SetField / RemoveField
 * - Primitive arrays → full replace (SetField)
 * - Object arrays:
 *     * if items have id or identityKey → itemized collection diff
 *     * otherwise → full replace (SetField)
 */
export const deepDelta = <T extends Record<string, unknown>>(
  formState: T,
  backupState: T,
  options?: DeepDeltaOptions<T>
): Delta<T> => {
  const delta = {} as Delta<T>

  // Sets an operation flag into the provided bag for a given key
  const setOp = (bag: OperationBag, key: string, op: PatchOperation) => {
    const ops = (bag.operations ??= {} as Record<string, PatchOperation>)
    ops[key] = op
  }

  /**
   * Recursive object diffing.
   * 
   * Handles:
   * - primitives
   * - nested objects
   * - arrays (delegates to array logic)
   */
  const calculateDelta = (
    form: PlainObject,
    backup: PlainObject,
    parentDelta: PlainObject & OperationBag
  ) => {
    const keys = Array.from(new Set([...Object.keys(form), ...Object.keys(backup)]))

    for (const rawKey of keys) {
      const key = rawKey as keyof T & string
      const formValue = form[key]
      const backupValue = backup[key]

      // --- ARRAY ---
      if (Array.isArray(formValue) && Array.isArray(backupValue)) {
        const bothPrimitive =
          (formValue as unknown[]).every(v => typeof v !== 'object' || v === null) &&
          (backupValue as unknown[]).every(v => typeof v !== 'object' || v === null)

        /**
         * Detect primitive arrays (string[], number[], primitive unions).
         * Primitive arrays have no identity → always full replace.
         */
        if (bothPrimitive) {
          if (!deepEqual(formValue, backupValue)) {
            ;(parentDelta as Delta<T>)[key] = deepCopy(formValue) as unknown as Delta<T>[typeof key]
            setOp(parentDelta, key, PatchOperation.SetField)
          }
          continue
        }

        // Object collections
        const policy = getArrayPolicy(options, key)

        /**
         * If items have neither `id` nor `identityKey`, they cannot be diffed.
         * => treat array as a scalar and replace entirely.
         */
        const lacksIdentity =
          !(policy?.identityKey) &&
          (formValue as Identifiable[]).every(x => (x?.id ?? null) == null) &&
          (backupValue as Identifiable[]).every(x => (x?.id ?? null) == null)

        if (lacksIdentity) {
          if (!deepEqual(formValue, backupValue)) {
            ;(parentDelta as Delta<T>)[key] = deepCopy(formValue) as unknown as Delta<T>[typeof key]
            setOp(parentDelta, key, PatchOperation.SetField)
          }
          continue
        }

        /**
         * Identifiable arrays => itemized delta with Add/Remove/Update
         */
        const arrayDelta = calculateArrayDelta(
          formValue as Identifiable[],
          backupValue as Identifiable[],
          policy
        )
        
        if (arrayDelta.length > 0) {
          ;(parentDelta as Delta<T>)[key] = arrayDelta as unknown as Delta<T>[typeof key]
        }

        continue
      }


      // --- OBJECT ---
      if (isPlainObject(formValue) && isPlainObject(backupValue)) {
        if (!deepEqual(formValue, backupValue)) {
          const nestedDelta: PlainObject & OperationBag = {}
          calculateDelta(
            formValue as PlainObject,
            (backupValue as PlainObject) ?? {},
            nestedDelta
          )
          if (Object.keys(nestedDelta).length > 0) {
            ;(parentDelta as Delta<T>)[key] = nestedDelta as unknown as Delta<T>[typeof key]
          }
        }
        continue
      }

      // --- PRIMITIVE / TYPE CHANGED ---
      if (!deepEqual(formValue, backupValue)) {
        ;(parentDelta as Delta<T>)[key] = formValue as Delta<T>[typeof key]
        setOp(parentDelta, key, formValue === null ? PatchOperation.RemoveField : PatchOperation.SetField)
      }
    }
  }

  /**
   * Computes itemized delta for identifiable object arrays.
   * 
   * Handles:
   * - Add: item without id or identity
   * - Remove: item missing in formArray
   * - Update: fields changed inside item
   * - Re-parenting: rootKey changed
   * - Role: if policy.deleteItemWhenRoleRemoved is true
   */
  const calculateArrayDelta = <U extends Identifiable>(
    formArray: U[],
    backupArray: U[],
    policy?: ArrayPolicy
  ): DeltaArrayItem<U>[] => {
    const arrayDelta: DeltaArrayItem<U>[] = []

    /**
     * Identity resolution order:
     * 1. If item has `.id` → use it.
     * 2. Else if identityKey is provided → use that to extract a unique key.
     * 3. Else: return null → item will be treated as “new”.
     */
    const resolveId = (item?: U): IdLike => {
      if (!item) return null
      const directId = (item as Identifiable).id
      if (directId !== null && directId !== undefined) return directId
      if (!policy?.identityKey) return null

      if (typeof policy.identityKey === 'function') {
        try { return policy.identityKey(item as unknown as Record<string, unknown>) }
        catch { return null }
      }

      const k = policy.identityKey as string
      const v = (item as unknown as Record<string, unknown>)[k]
      return (typeof v === 'string' || typeof v === 'number') ? v : null
    }

    const childrenKeys = policy?.childArrayKeys ?? []
    const dropChildren = policy?.dropChildrenOnRootChange ?? true
    const roleKey = (policy?.roleFieldKey ?? 'role') as keyof U & string
    const rootKey = policy?.rootKey

    const sameRoot = (f: U, b: U): boolean => {
      if (!rootKey) return true
      return (f as PlainObject)[rootKey] === (b as PlainObject)[rootKey]
    }

    // id → item maps for O(1) lookups
    const formMap = new Map<string | number, U>()
    const backupMap = new Map<string | number, U>()
    for (const item of formArray) {
      const id = resolveId(item)
      if (id !== null && id !== undefined) formMap.set(id as string | number, item)
    }
    for (const item of backupArray) {
      const id = resolveId(item)
      if (id !== null && id !== undefined) backupMap.set(id as string | number, item)
    }

    // 1) Items present in the form array
    for (const formItem of formArray) {
      const fid = resolveId(formItem)

      // 1.a) New item (no identity)
      if (fid === null || fid === undefined) {
        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        // normalize children as AddToCollection
        for (const ck of childrenKeys) {
          const v = (addItem as PlainObject)[ck]
          if (Array.isArray(v)) {
            const normalized = (v as Identifiable[]).map(child => {
              const c = {} as DeltaArrayItem<Identifiable>
              Object.assign(c, child as Partial<Identifiable>)
              c.operations = { collectionItemOperation: PatchOperation.AddToCollection }
              return c
            })
          ;(addItem as PlainObject)[ck] = normalized
          }
        }

        arrayDelta.push(addItem)
        continue
      }

      // 1.b) Has identity but not in backup ⇒ AddToCollection
      const backupItem = backupMap.get(fid as string | number)
      if (!backupItem) {
        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.id = fid as U['id'] // store identity for server convenience
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        for (const ck of childrenKeys) {
          const v = (addItem as PlainObject)[ck]
          if (Array.isArray(v)) {
            const normalized = (v as Identifiable[]).map(child => {
              const c = {} as DeltaArrayItem<Identifiable>
              Object.assign(c, child as Partial<Identifiable>)
              c.operations = { collectionItemOperation: PatchOperation.AddToCollection }
              return c
            })
          ;(addItem as PlainObject)[ck] = normalized
          }
        }

        arrayDelta.push(addItem)
        continue
      }

      // 1.c) Re-parenting: root changed
      if (!sameRoot(formItem, backupItem)) {
        const removeItem = {} as DeltaArrayItem<U>
        removeItem.id = fid as U['id']
        removeItem.operations = { collectionItemOperation: PatchOperation.RemoveFromCollection }
        arrayDelta.push(removeItem)

        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        if (dropChildren) {
          for (const ck of childrenKeys) {
            if (ck in (addItem as PlainObject)) {
              ;(addItem as PlainObject)[ck] = []
            }
          }
        } else {
          for (const ck of childrenKeys) {
            const v = (addItem as PlainObject)[ck]
            if (Array.isArray(v)) {
              const normalized = (v as Identifiable[]).map(child => {
                const c = {} as DeltaArrayItem<Identifiable>
                Object.assign(c, child as Partial<Identifiable>)
                c.operations = { collectionItemOperation: PatchOperation.AddToCollection }
                return c
              })
            ;(addItem as PlainObject)[ck] = normalized
            }
          }
        }

        arrayDelta.push(addItem)
        continue
      }

      // 1.d) Role → null ⇒ remove item (if enabled)
      const deleteOnRoleNull = policy?.deleteItemWhenRoleRemoved ?? true
      if (deleteOnRoleNull) {
        const formRole = (formItem as PlainObject)[roleKey]
        const backupRole = (backupItem as PlainObject)[roleKey]
        const roleBecameNull = backupRole !== null && formRole === null
        if (roleBecameNull) {
          const removeItem = {} as DeltaArrayItem<U>
          removeItem.id = fid as U['id']
          removeItem.operations = { collectionItemOperation: PatchOperation.RemoveFromCollection }
          arrayDelta.push(removeItem)
          continue
        }
      }

      // 1.e) Field-level diff
      const itemDeltaBase = {} as (PlainObject & OperationBag & { id?: U['id'] })
      itemDeltaBase.id = fid as U['id']

      calculateDelta(
      formItem as PlainObject,
      backupItem as PlainObject,
      itemDeltaBase
      )

      const hasMeaningfulChanges = Object.keys(itemDeltaBase).some(k => k !== 'id')
      if (hasMeaningfulChanges) {
        arrayDelta.push(itemDeltaBase as DeltaArrayItem<U>)
      }
    }

    // 2) Items removed
    for (const backupItem of backupArray) {
      const bid = resolveId(backupItem)
      if (bid === null || bid === undefined) continue
      if (!formMap.has(bid as string | number)) {
        const removeItem = {} as DeltaArrayItem<U>
        removeItem.id = bid as U['id']
        removeItem.operations = { collectionItemOperation: PatchOperation.RemoveFromCollection }
        arrayDelta.push(removeItem)
      }
    }

    return arrayDelta
  }

  calculateDelta(
    deepCopy(formState) as PlainObject,
    deepCopy(backupState) as PlainObject,
    delta as PlainObject & OperationBag
  )

  return delta
}

/**
 * Checks whether any operations exist inside the delta.
 * 
 * A delta has operations if:
 * - parent-level operations exist, or
 * - nested object deltas contain operations, or
 * - any array item contains operations.
 */
export const deltaHasOperations = <T extends Record<string, unknown>>(delta: Delta<T>): boolean => {
  if (!isPlainObject(delta)) return false
  if ('operations' in delta && isPlainObject(delta.operations)) return true

  for (const key in delta) {
    const v = (delta as PlainObject)[key]

    if (isPlainObject(v) && deltaHasOperations(v as Delta<{}>)) return true

    if (Array.isArray(v)) {
      for (const item of v) {
        if (isPlainObject(item) && deltaHasOperations(item as Delta<{}>)) return true
      }
    }
  }
  return false
}

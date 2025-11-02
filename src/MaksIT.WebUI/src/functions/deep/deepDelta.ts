import { PatchOperation } from '../../models/PatchOperation'
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

/** Policy non-generica: chiavi sempre stringhe */
export type ArrayPolicy = {
  /** Nome del campo “radice” che implica re-parenting (es. 'organizationId') */
  rootKey?: string
  /** Nomi degli array figli da trattare in caso di re-parenting (es. ['applicationRoles']) */
  childArrayKeys?: string[]
  /** Se true, in re-parenting i figli vengono azzerati (default TRUE) */
  dropChildrenOnRootChange?: boolean
  /** Nome del campo ruolo (default 'role') */
  roleFieldKey?: string
  /** Se true, quando role diventa null si rimuove l’intero item (default TRUE) */
  deleteItemWhenRoleRemoved?: boolean
}

export type DeepDeltaOptions<T> = {
  /** Policy per i campi array del payload (mappati per nome chiave) */
  arrays?: Partial<Record<Extract<keyof T, string>, ArrayPolicy>>
}

export type Delta<T> =
  Partial<{
    [K in keyof T]:
      T[K] extends (infer U)[]
        ? DeltaArrayItem<(U & Identifiable)>[]
        : T[K] extends object
          ? Delta<T[K] & OperationBag<Extract<keyof T, string>>>
          : T[K]
  }> & OperationBag<Extract<keyof T, string>>

/** Safe index per evitare TS2536 quando si indicizza su chiavi dinamiche */
const getArrayPolicy = <T>(options: DeepDeltaOptions<T> | undefined, key: string): ArrayPolicy | undefined =>{
  const arrays = options?.arrays as Partial<Record<string, ArrayPolicy>> | undefined
  return arrays?.[key]
}

const isPlainObject = (value: unknown): value is PlainObject =>
  typeof value === 'object' && value !== null && !Array.isArray(value)

export const deepDelta = <T extends Record<string, unknown>>(
  formState: T,
  backupState: T,
  options?: DeepDeltaOptions<T>
): Delta<T> => {
  const delta = {} as Delta<T>

  const setOp = (bag: OperationBag, key: string, op: PatchOperation) => {
    const ops = (bag.operations ??= {} as Record<string, PatchOperation>)
    ops[key] = op
  }

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
        const policy = getArrayPolicy(options, key)
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

  const calculateArrayDelta = <U extends Identifiable>(
    formArray: U[],
    backupArray: U[],
    policy?: ArrayPolicy
  ): DeltaArrayItem<U>[] => {
    const arrayDelta: DeltaArrayItem<U>[] = []

    const getId = (item?: U): IdLike => (item ? item.id ?? null : null)
    const childrenKeys = policy?.childArrayKeys ?? []
    const dropChildren = policy?.dropChildrenOnRootChange ?? true
    const roleKey = (policy?.roleFieldKey ?? 'role') as keyof U & string
    const rootKey = policy?.rootKey

    const sameRoot = (f: U, b: U): boolean => {
      if (!rootKey) return true
      return (f as PlainObject)[rootKey] === (b as PlainObject)[rootKey]
    }

    // Mappe id → item per lookup veloce
    const formMap = new Map<string | number, U>()
    const backupMap = new Map<string | number, U>()
    for (const item of formArray) {
      const id = getId(item)
      if (id !== null && id !== undefined) formMap.set(id as string | number, item)
    }
    for (const item of backupArray) {
      const id = getId(item)
      if (id !== null && id !== undefined) backupMap.set(id as string | number, item)
    }

    // 1) Gestione elementi presenti nel form
    for (const formItem of formArray) {
      const fid = getId(formItem)

      // 1.a) Nuovo item (senza id)
      if (fid === null || fid === undefined) {
        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        // ⬇️ NON droppiamo i figli su "add": li normalizziamo come AddToCollection
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

      // 1.b) Ha id ma non esiste nel backup ⇒ AddToCollection
      const backupItem = backupMap.get(fid as string | number)
      if (!backupItem) {
        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.id = fid as U['id']
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        // ⬇️ Anche qui: manteniamo i figli, marcandoli come AddToCollection
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

      // 1.c) Re-parenting: root cambiata
      if (!sameRoot(formItem, backupItem)) {
        // REMOVE vecchio
        const removeItem = {} as DeltaArrayItem<U>
        removeItem.id = fid as U['id']
        removeItem.operations = { collectionItemOperation: PatchOperation.RemoveFromCollection }
        arrayDelta.push(removeItem)

        // ADD nuovo
        const addItem = {} as DeltaArrayItem<U>
        Object.assign(addItem, formItem as Partial<U>)
        addItem.operations = { collectionItemOperation: PatchOperation.AddToCollection }

        if (dropChildren) {
          // ⬇️ SOLO qui, in caso di re-parenting e se richiesto, azzera i figli
          for (const ck of childrenKeys) {
            if (ck in (addItem as PlainObject)) {
              ;(addItem as PlainObject)[ck] = []
            }
          }
        } else {
          // Mantieni i figli marcandoli come AddToCollection
          for (const ck of childrenKeys) {
            const v = (addItem as PlainObject)[ck]
            if (Array.isArray(v)) {
              const normalized = (v as Identifiable[]).map(child => {
                const c = {} as DeltaArrayItem<Identifiable>
                Object.assign(c, child as Partial<Identifiable>)
                c.operations = { collectionItemOperation: PatchOperation.AddToCollection }
                return c
              }); (addItem as PlainObject)[ck] = normalized
            }
          }
        }

        arrayDelta.push(addItem)
        continue
      }


      // 1.d) Ruolo → null ⇒ rimozione item (se abilitato)
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

      // 1.e) Diff puntuale su campi
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

    // 2) Elementi rimossi
    for (const backupItem of backupArray) {
      const bid = getId(backupItem)
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

import { FC, useEffect, useMemo, useRef } from 'react'
import z, { array, object, RefinementCtx, string, ZodType } from 'zod'
import { useAppSelector } from '../../redux/hooks'
import { useFormState } from '../../hooks/useFormState'
import { ButtonComponent } from '../../components/editors'
import { Shield, Trash2 } from 'lucide-react'
import { deepCopy, deepEqual, enumToArr, hasAnyFlag, hasFlag, toggleFlag } from '../../functions'

import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'
import { CERTS_UI_PLATFORM_SCOPE_ENTITY_ID } from '../../constants/certsUiPlatformScope'

export interface EntityScopeFormProps {
  id?: string
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}

export const EntityScopeFormPropsSchema: ZodType<EntityScopeFormProps> = object({
  id: string().optional(),
  entityId: string(),
  entityType: z.nativeEnum(ScopeEntityType),
  scope: z.number(),
}).superRefine((val: EntityScopeFormProps, ctx: RefinementCtx) => {
  if (
    (val.entityType === ScopeEntityType.Identity || val.entityType === ScopeEntityType.ApiKey) &&
    !hasAnyFlag(
      val.scope,
      ScopePermission.Read |
        ScopePermission.Write |
        ScopePermission.Delete |
        ScopePermission.Create
    )
  ) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'invalid scope permission value',
      path: ['entityScopes'],
    })
  }
})

interface EditScopesFormProps {
  entityScopes: EntityScopeFormProps[]
  scopeIdentity?: ScopePermission
  scopeApiKey?: ScopePermission
}

/** Draft fields validated implicitly when rows are added; parent forms enforce “at least one scope” like Vault. */
const EditScopesFormPropsSchema = (): ZodType<EditScopesFormProps> =>
  object({
    entityScopes: array(EntityScopeFormPropsSchema),
    scopeIdentity: z.number().optional(),
    scopeApiKey: z.number().optional(),
  })

interface EditUserRolesProps {
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  id?: string
  entityScopes?: EntityScopeFormProps[]
  onChange?: (entityScopes: EntityScopeFormProps[]) => void
  emptyStateMessage?: string
  allowIdentityAndApiKeyScopes?: boolean
}

const SCOPE_TYPE_LABELS: Record<ScopeEntityType, string> = {
  [ScopeEntityType.Identity]: 'User management',
  [ScopeEntityType.ApiKey]: 'API key management',
}

const crudFlags = [
  ScopePermission.Read,
  ScopePermission.Write,
  ScopePermission.Delete,
  ScopePermission.Create,
]

function draftBitsFromScopes(scopes: EntityScopeFormProps[]) {
  const idRow = scopes.find(
    (s) =>
      s.entityType === ScopeEntityType.Identity &&
      s.entityId === CERTS_UI_PLATFORM_SCOPE_ENTITY_ID
  )
  const akRow = scopes.find(
    (s) =>
      s.entityType === ScopeEntityType.ApiKey &&
      s.entityId === CERTS_UI_PLATFORM_SCOPE_ENTITY_ID
  )
  return {
    scopeIdentity: idRow?.scope ?? ScopePermission.None,
    scopeApiKey: akRow?.scope ?? ScopePermission.None,
  }
}

const EditUserScopes: FC<EditUserRolesProps> = (props) => {
  const {
    colspan = 12,
    id,
    entityScopes = [],
    onChange,
    emptyStateMessage,
    allowIdentityAndApiKeyScopes = false,
  } = props

  const { identity } = useAppSelector((state) => state.identity)

  const validationSchema = useMemo(() => EditScopesFormPropsSchema(), [])

  const prevValue = useRef<EntityScopeFormProps[] | undefined>(undefined)
  const formStateRef = useRef<EditScopesFormProps | null>(null)

  const {
    formState,
    errors,
    handleInputChange,
    setInitialState,
  } = useFormState<EditScopesFormProps>({
    initialState: {
      entityScopes: [],
      scopeIdentity: ScopePermission.None,
      scopeApiKey: ScopePermission.None,
    },
    validationSchema,
  })

  formStateRef.current = formState

  useEffect(() => {
    if (!entityScopes) return
    if (deepEqual(entityScopes, prevValue.current)) return
    prevValue.current = deepCopy(entityScopes)
    const draft = draftBitsFromScopes(entityScopes)
    setInitialState({
      ...formStateRef.current,
      entityScopes,
      ...draft,
    })
  }, [entityScopes, setInitialState])

  const upsertScope = (
    scopes: EntityScopeFormProps[],
    next: EntityScopeFormProps
  ): EntityScopeFormProps[] => {
    const index = scopes.findIndex(
      (s) => s.entityType === next.entityType && s.entityId === next.entityId
    )
    if (index === -1) return [...scopes, next]
    const updated = [...scopes]
    const existing = updated[index]
    updated[index] = { ...next, id: existing.id ?? next.id }
    return updated
  }

  const handleAddScope = () => {
    const crud =
      ScopePermission.Read |
      ScopePermission.Write |
      ScopePermission.Delete |
      ScopePermission.Create
    const hasIdentity =
      allowIdentityAndApiKeyScopes &&
      hasAnyFlag(formState.scopeIdentity ?? ScopePermission.None, crud)
    const hasApiKey =
      allowIdentityAndApiKeyScopes &&
      hasAnyFlag(formState.scopeApiKey ?? ScopePermission.None, crud)
    if (!hasIdentity && !hasApiKey) return

    let updatedScopes = [...formState.entityScopes]

    if (hasIdentity) {
      updatedScopes = upsertScope(updatedScopes, {
        id: undefined,
        entityId: CERTS_UI_PLATFORM_SCOPE_ENTITY_ID,
        entityType: ScopeEntityType.Identity,
        scope: formState.scopeIdentity!,
      })
    }
    if (hasApiKey) {
      updatedScopes = upsertScope(updatedScopes, {
        id: undefined,
        entityId: CERTS_UI_PLATFORM_SCOPE_ENTITY_ID,
        entityType: ScopeEntityType.ApiKey,
        scope: formState.scopeApiKey!,
      })
    }

    setInitialState({
      ...formState,
      entityScopes: updatedScopes,
      scopeIdentity: ScopePermission.None,
      scopeApiKey: ScopePermission.None,
    })
    onChange?.(updatedScopes)
  }

  const handleRemoveScope = (entityType: ScopeEntityType, entityId?: string) => {
    if (!entityId) return
    const updatedScopes = formState.entityScopes.filter(
      (scope) => !(scope.entityType === entityType && scope.entityId === entityId)
    )
    setInitialState({ ...formState, entityScopes: updatedScopes })
    onChange?.(updatedScopes)
  }

  const handleScopePermissionChange = (index: number, newScope: ScopePermission) => {
    const updated = formState.entityScopes.map((s, i) =>
      i === index ? { ...s, scope: newScope } : s
    )
    setInitialState({ ...formState, entityScopes: updated })
    onChange?.(updated)
  }

  const getContextLabel = (scope: EntityScopeFormProps): string => {
    if (scope.entityId === CERTS_UI_PLATFORM_SCOPE_ENTITY_ID) return 'Certs UI'
    return scope.entityId
  }

  const crud =
    ScopePermission.Read |
    ScopePermission.Write |
    ScopePermission.Delete |
    ScopePermission.Create
  const hasDraftScope = Boolean(
    allowIdentityAndApiKeyScopes &&
      (hasAnyFlag(formState.scopeIdentity ?? ScopePermission.None, crud) ||
        hasAnyFlag(formState.scopeApiKey ?? ScopePermission.None, crud))
  )

  const canEdit = identity?.userId !== id

  const colSpanClass =
    colspan === 12
      ? 'col-span-12'
      : ({
          1: 'col-span-1',
          2: 'col-span-2',
          3: 'col-span-3',
          4: 'col-span-4',
          5: 'col-span-5',
          6: 'col-span-6',
          7: 'col-span-7',
          8: 'col-span-8',
          9: 'col-span-9',
          10: 'col-span-10',
          11: 'col-span-11',
        }[colspan] ?? 'col-span-12')

  return (
    <div className={colSpanClass}>
      <div className="grid grid-cols-12 gap-4 w-full">
        <div className="col-span-12 border border-gray-200 rounded-lg bg-white p-5 shadow-sm">
          <h3 className="text-sm font-semibold text-gray-800 flex items-center gap-2 mb-4">
            <Shield className="w-4 h-4 text-blue-500" />
            Add scope
          </h3>

          <div className="flex flex-col gap-5">
            <fieldset
              className="rounded-md border border-gray-200 bg-gray-50/50 px-4 py-3 col-span-12"
              disabled={!allowIdentityAndApiKeyScopes}
            >
              <legend className="text-xs font-medium text-gray-600 px-1">
                Platform permissions
              </legend>
              {allowIdentityAndApiKeyScopes && (
                <>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6 space-y-3 mt-1 md:space-y-0">
                    <div>
                      <span className="text-xs text-gray-500 block mb-1">
                        User management
                      </span>
                      <div className="flex flex-wrap gap-x-4 gap-y-1">
                        {enumToArr(ScopePermission)
                          .filter((x) =>
                            crudFlags.includes(x.value as ScopePermission)
                          )
                          .map((x) => (
                            <label
                              key={x.value}
                              className="inline-flex items-center gap-2 cursor-pointer"
                            >
                              <input
                                type="checkbox"
                                checked={hasFlag(
                                  formState.scopeIdentity ?? ScopePermission.None,
                                  x.value as ScopePermission
                                )}
                                onChange={() =>
                                  handleInputChange(
                                    'scopeIdentity',
                                    toggleFlag(
                                      formState.scopeIdentity ?? ScopePermission.None,
                                      x.value as ScopePermission
                                    )
                                  )
                                }
                                className="rounded border-gray-300 text-blue-600"
                              />
                              <span className="text-sm text-gray-700">
                                {x.displayValue}
                              </span>
                            </label>
                          ))}
                      </div>
                    </div>
                    <div>
                      <span className="text-xs text-gray-500 block mb-1">
                        API key management
                      </span>
                      <div className="flex flex-wrap gap-x-4 gap-y-1">
                        {enumToArr(ScopePermission)
                          .filter((x) =>
                            crudFlags.includes(x.value as ScopePermission)
                          )
                          .map((x) => (
                            <label
                              key={`ak-${x.value}`}
                              className="inline-flex items-center gap-2 cursor-pointer"
                            >
                              <input
                                type="checkbox"
                                checked={hasFlag(
                                  formState.scopeApiKey ?? ScopePermission.None,
                                  x.value as ScopePermission
                                )}
                                onChange={() =>
                                  handleInputChange(
                                    'scopeApiKey',
                                    toggleFlag(
                                      formState.scopeApiKey ?? ScopePermission.None,
                                      x.value as ScopePermission
                                    )
                                  )
                                }
                                className="rounded border-gray-300 text-blue-600"
                              />
                              <span className="text-sm text-gray-700">
                                {x.displayValue}
                              </span>
                            </label>
                          ))}
                      </div>
                    </div>
                  </div>
                  <p className="text-red-500 text-xs italic mt-2">
                    {errors['scopeIdentity']}
                  </p>
                </>
              )}
            </fieldset>

            <div className="flex items-center gap-3 pt-1">
              <ButtonComponent
                label="Add scope"
                buttonHierarchy="primary"
                onClick={handleAddScope}
                disabled={!canEdit || !hasDraftScope}
              />
              {identity?.userId === id && (
                <span className="text-gray-500 text-sm">
                  You cannot modify your own scopes.
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="col-span-12">
          <h3 className="text-sm font-semibold text-gray-800 mb-2 flex items-center gap-2">
            <Shield className="w-4 h-4 text-gray-500" />
            Scopes
          </h3>
          {formState.entityScopes.length === 0 ? (
            <div className="border border-dashed border-gray-300 rounded-lg bg-gray-50 p-6 text-center">
              <p className="text-sm text-gray-600">No scopes yet.</p>
              <p className="text-xs text-gray-500 mt-1">
                Set user management and/or API key management permissions above, then
                click Add scope.
              </p>
              {emptyStateMessage && (
                <p className="text-xs text-amber-700 mt-2 font-medium">
                  {emptyStateMessage}
                </p>
              )}
            </div>
          ) : (
            <div className="border border-gray-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-100 border-b border-gray-200">
                    <th className="px-3 py-2 text-left text-xs font-medium text-gray-600">
                      Scope type
                    </th>
                    <th className="px-3 py-2 text-left text-xs font-medium text-gray-600">
                      Context
                    </th>
                    <th className="px-2 py-2 text-center text-xs font-medium text-gray-600">
                      Read
                    </th>
                    <th className="px-2 py-2 text-center text-xs font-medium text-gray-600">
                      Write
                    </th>
                    <th className="px-2 py-2 text-center text-xs font-medium text-gray-600">
                      Create
                    </th>
                    <th className="px-2 py-2 text-center text-xs font-medium text-gray-600">
                      Delete
                    </th>
                    <th className="w-10 px-2 py-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {formState.entityScopes.map((scope, index) => (
                    <tr
                      key={`${scope.entityType}-${scope.entityId}-${index}`}
                      className="border-b border-gray-100 hover:bg-gray-50/50"
                    >
                      <td className="px-3 py-2 font-medium text-gray-800">
                        {SCOPE_TYPE_LABELS[scope.entityType]}
                      </td>
                      <td className="px-3 py-2 text-gray-700">
                        {getContextLabel(scope)}
                      </td>
                      <td className="px-2 py-2 text-center">
                        <input
                          type="checkbox"
                          checked={hasFlag(scope.scope, ScopePermission.Read)}
                          onChange={() =>
                            handleScopePermissionChange(
                              index,
                              toggleFlag(scope.scope, ScopePermission.Read)
                            )
                          }
                          disabled={!canEdit}
                          className="rounded border-gray-300"
                        />
                      </td>
                      <td className="px-2 py-2 text-center">
                        <input
                          type="checkbox"
                          checked={hasFlag(scope.scope, ScopePermission.Write)}
                          onChange={() =>
                            handleScopePermissionChange(
                              index,
                              toggleFlag(scope.scope, ScopePermission.Write)
                            )
                          }
                          disabled={!canEdit}
                          className="rounded border-gray-300"
                        />
                      </td>
                      <td className="px-2 py-2 text-center">
                        <input
                          type="checkbox"
                          checked={hasFlag(scope.scope, ScopePermission.Create)}
                          onChange={() =>
                            handleScopePermissionChange(
                              index,
                              toggleFlag(scope.scope, ScopePermission.Create)
                            )
                          }
                          disabled={!canEdit}
                          className="rounded border-gray-300"
                        />
                      </td>
                      <td className="px-2 py-2 text-center">
                        <input
                          type="checkbox"
                          checked={hasFlag(scope.scope, ScopePermission.Delete)}
                          onChange={() =>
                            handleScopePermissionChange(
                              index,
                              toggleFlag(scope.scope, ScopePermission.Delete)
                            )
                          }
                          disabled={!canEdit}
                          className="rounded border-gray-300"
                        />
                      </td>
                      <td className="px-2 py-2">
                        <button
                          type="button"
                          onClick={() =>
                            handleRemoveScope(scope.entityType, scope.entityId)
                          }
                          disabled={!canEdit}
                          className="p-1.5 rounded hover:bg-red-50 text-gray-500 hover:text-red-600 disabled:opacity-40"
                          aria-label="Remove scope"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {errors['entityScopes'] && (
            <p className="text-red-500 text-xs italic mt-2">{errors['entityScopes']}</p>
          )}
        </div>
      </div>
    </div>
  )
}

export { EditUserScopes }

import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../components/FormLayout'
import { useAppSelector } from '../../redux/hooks'
import { PagedResponse } from '../../models/certsUI/common/PagedResponse'
import { UserResponse } from '../../models/identity/user/UserResponse'
import { deleteData, postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { createColumns, DataTable, DataTableFilter, DataTableLabel } from '../../components/DataTable'
import { SearchUserRequest } from '../../models/identity/user/search/SearchUserRequest'
import { SearchUserResponse } from '../../models/identity/user/search/SearchUserResponse'
import { CreateUser } from './CreateUser'
import { Offcanvas } from '../../components/Offcanvas'
import { EditUser } from './EditUser'
import { hasFlag } from '../../functions'
import { createColumn } from '../../components/DataTable'
import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'

const SearchUser: FC = () => {

  const { identity } = useAppSelector(state => state.identity)

  const [pagedRequest, setPagedRequest] = useState<SearchUserRequest>({})
  const [rawd, setRawd] = useState<PagedResponse<SearchUserResponse> | undefined>(undefined)

  const [userId, setUserId] = useState<string | undefined>(undefined)
  const [creating, setCreating] = useState<boolean>(false)

  const columns = createColumns([
    createColumn<SearchUserResponse, 'id'>({
      id: 'id',
      accessorKey: 'id',
      header: 'Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'id'}
          onFilterChange={onFilterChange}
          disabled={true}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value} />
      }
    }),
    createColumn<SearchUserResponse, 'username'>({
      id: 'username',
      accessorKey: 'username',
      header: 'Username',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'username'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value} />
      },
    }),
    createColumn<SearchUserResponse, 'email'>({
      id: 'email',
      accessorKey: 'email',
      header: 'Email',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'email'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ?? ''} />
      },
    }),
    createColumn<SearchUserResponse, 'mobileNumber'>({
      id: 'mobileNumber',
      accessorKey: 'mobileNumber',
      header: 'Mobile Number',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'mobileNumber'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ?? ''} />
      },
    }),
    createColumn<SearchUserResponse, 'isActive'>({
      id: 'isActive',
      accessorKey: 'isActive',
      header: 'Is Active',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'isActive'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ? 'Yes' : 'No'} />
      },
    }),
    createColumn<SearchUserResponse, 'twoFactorEnabled'>({
      id: 'twoFactorEnabled',
      accessorKey: 'twoFactorEnabled',
      header: '2FA',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'twoFactorEnabled'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ? 'Yes' : 'No'} />
      },
    }),
    createColumn<SearchUserResponse, 'recoveryCodesLeft'>({
      id: 'recoveryCodesLeft',
      accessorKey: 'recoveryCodesLeft',
      header: '2FA Recovery Left',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'recoveryCodesLeft'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={`${value ?? ''}`} />
      },
    }),
    createColumn<SearchUserResponse, 'createdAt'>({
      id: 'createdAt',
      accessorKey: 'createdAt',
      header: 'Created At',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'createdAt'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value} dataType={'date'} />
      },
    }),
    createColumn<SearchUserResponse, 'lastLogin'>({
      id: 'lastLogin',
      accessorKey: 'lastLogin',
      header: 'Last Login',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'lastLogin'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ?? ''} dataType={'date'} />
      },
    }),
    createColumn<SearchUserResponse, 'isGlobalAdmin'>({
      id: 'isGlobalAdmin',
      accessorKey: 'isGlobalAdmin',
      header: 'Global Admin',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'isGlobalAdmin'}
          onFilterChange={onFilterChange}
          disabled={true}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ? 'Yes' : 'No'} />
      },
    }),
  ])

  const loadData = useCallback(() => {
    postData<SearchUserRequest, PagedResponse<SearchUserResponse>>(GetApiRoute(ApiRoutes.identitySearch).route, pagedRequest)
      .then((response) => {
        setRawd(response.payload ?? undefined)
      })
      .finally(() => {})
  }, [pagedRequest])

  useEffect(() => loadData(), [loadData])

  const handleAddRow = () => {
    setCreating(true)
  }

  const handleEditRow = (ids: { [key: string]: string }) => {
    setUserId(ids.id)
  }

  const handleDeleteRow = (ids: { [key: string]: string }) => {
    deleteData(GetApiRoute(ApiRoutes.identityDelete).route
      .replace('{userId}', ids.id)
    ).then((response) => {
      if (response.ok)
        loadData()
    })
  }

  const handleEditCancel = () => {
    setUserId(undefined)
  }

  const handleFilterChange = (filters: { [filterId: string]: string }) => {
    setPagedRequest({
      ...pagedRequest,
      ...filters
    })
  }

  const handlePageChange = (pageNumber: number) => {
    setPagedRequest({
      ...pagedRequest,
      pageNumber
    })
  }

  const handleOnSubmitted = (_item?: UserResponse) => {
    setUserId(undefined)

    setCreating(false)

    loadData()
  }

  if (!identity) return <></>

  const handleAllowAddRow = () => {
    if (identity.isGlobalAdmin)
      return true
    const hasCreateIdentity = identity.acls?.some(acl =>
      acl.entityType === ScopeEntityType.Identity && hasFlag(acl.scope, ScopePermission.Create))
    return !!hasCreateIdentity
  }

  const handleAllowEditRow = (ids: Record<string, string>) => {
    if (identity.isGlobalAdmin)
      return true

    const organizationId = ids.organizationId
    const canEditIdentity = identity.acls
      ?.find(acl => acl.entityId == organizationId
        && acl.entityType === ScopeEntityType.Identity
        && hasFlag(acl.scope, ScopePermission.Write))

    return !!canEditIdentity
  }

  const handleAllowDeleteRow = (ids: Record<string, string>) => {
    if (identity.isGlobalAdmin)
      return true

    const organizationId = ids.organizationId
    const canDeleteIdentity = identity.acls
      ?.find(acl => acl.entityId == organizationId
        && acl.entityType === ScopeEntityType.Identity
        && hasFlag(acl.scope, ScopePermission.Delete))

    return !!canDeleteIdentity
  }

  return <>
    <FormContainer>
      <FormHeader>Users</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full'}>
          <DataTable
            colspan={12}
            rawd={rawd}
            columns={columns}
            storageKey={'SearchUser'}
            idFields={['id']}

            allowAddRow={handleAllowAddRow}
            onAddRow={handleAddRow}
            allowEditRow={handleAllowEditRow}
            onEditRow={handleEditRow}
            allowDeleteRow={handleAllowDeleteRow}
            onDeleteRow={handleDeleteRow}

            onFilterChange={handleFilterChange}
            onPreviousPage={handlePageChange}
            onNextPage={handlePageChange}
          />
        </div>
      </FormContent>
    </FormContainer>

    <Offcanvas isOpen={userId !== undefined}>
      {userId && <EditUser
        userId={userId}
        cancelEnabled={true}
        onSubmitted={handleOnSubmitted}
        onCancel={handleEditCancel}
      />}
    </Offcanvas>
    {creating && (
      <CreateUser
        isOpen={true}
        onSubmitted={handleOnSubmitted}
        cancelEnabled={true}
        onCancel={() => setCreating(false)}
      />
    )}
  </>
}

export {
  SearchUser
}

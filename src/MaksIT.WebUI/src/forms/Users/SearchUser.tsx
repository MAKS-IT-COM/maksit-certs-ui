import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../components/FormLayout'
import { PagedResponse } from '../../models/letsEncryptServer/common/PagedResponse'
import { SearchUserRequest } from '../../models/identity/user/search/SearchUserRequest'
import { SearchUserResponse } from '../../models/identity/user/search/SearchUserResponse'
import { UserResponse } from '../../models/identity/user/UserResponse'
import { deleteData, postDataWithoutLoader } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { createColumn, createColumns, DataTable, DataTableFilter, DataTableLabel } from '../../components/DataTable'
import { Offcanvas } from '../../components/Offcanvas'
import { EditUser } from './EditUser'
import { CreateUser } from './CreateUser'
import { extractPropFilter } from '../../functions/dataTableFilters'

const defaultPage: SearchUserRequest = { pageNumber: 1, pageSize: 20 }

const SearchUser: FC = () => {

  const [pagedRequest, setPagedRequest] = useState<SearchUserRequest>(defaultPage)
  const [rawd, setRawd] = useState<PagedResponse<SearchUserResponse> | undefined>(undefined)

  const [userId, setUserId] = useState<string | undefined>(undefined)
  const [creating, setCreating] = useState(false)

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
      }
    }),
    createColumn<SearchUserResponse, 'isActive'>({
      id: 'isActive',
      accessorKey: 'isActive',
      header: 'Active',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'isActive'}
          onFilterChange={onFilterChange}
          disabled={true}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value === true ? 'Yes' : value === false ? 'No' : '—'} />
      }
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
          disabled={true}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value === true ? 'On' : value === false ? 'Off' : '—'} />
      }
    }),
    createColumn<SearchUserResponse, 'lastLogin'>({
      id: 'lastLogin',
      accessorKey: 'lastLogin',
      header: 'Last login',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'lastLogin'}
          onFilterChange={onFilterChange}
          disabled={true}
        />
      ),
      cell: (props) => {
        const { value } = props
        return <DataTableLabel type={'normal'} value={value ?? ''} dataType={'date'} />
      }
    }),
  ])

  const loadData = useCallback(() => {
    postDataWithoutLoader<SearchUserRequest, PagedResponse<SearchUserResponse>>(
      GetApiRoute(ApiRoutes.identitySearch).route,
      pagedRequest
    ).then((response) => {
      setRawd(response.payload ?? undefined)
    }).finally(() => {})
  }, [pagedRequest])

  useEffect(() => loadData(), [loadData])

  const handleAddRow = () => {
    setCreating(true)
  }

  const handleEditRow = (ids: Record<string, string>) => {
    setUserId(ids.id)
  }

  const handleDeleteRow = (ids: Record<string, string>) => {
    deleteData(GetApiRoute(ApiRoutes.identityDelete).route.replace('{userId}', ids.id))
      .then((response) => {
        if (!response.ok) return
        loadData()
      })
  }

  const handleEditCancel = () => {
    setUserId(undefined)
  }

  const handleFilterChange = (filters: Record<string, string>) => {
    const combined = filters.filters ?? ''
    const usernameFilter = extractPropFilter(combined, 'Username')
    setPagedRequest(prev => ({
      ...prev,
      pageNumber: 1,
      usernameFilter: usernameFilter?.trim() || undefined,
    }))
  }

  const handlePageChange = (pageNumber: number) => {
    setPagedRequest(prev => ({
      ...prev,
      pageNumber
    }))
  }

  const handleOnSubmitted = (_item?: UserResponse) => {
    setUserId(undefined)
    setCreating(false)
    setPagedRequest(prev => ({
      ...prev,
      pageNumber: 1,
    }))
    loadData()
  }

  return <>
    <FormContainer>
      <FormHeader>Users</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full min-h-[28rem]'}>
          <DataTable
            colspan={12}
            rawd={rawd}
            columns={columns}
            storageKey={'SearchUser'}
            idFields={['id']}

            allowAddRow={() => true}
            onAddRow={handleAddRow}

            allowEditRow={() => true}
            onEditRow={handleEditRow}
            allowDeleteRow={() => true}
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
        key={userId}
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

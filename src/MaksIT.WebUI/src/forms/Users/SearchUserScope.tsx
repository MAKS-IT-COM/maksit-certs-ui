import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../components/FormLayout'
import { useAppSelector } from '../../redux/hooks'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { PagedResponse } from '../../models/certsUI/common/PagedResponse'
import { createColumn, createColumns, DataTable, DataTableFilter, DataTableLabel } from '../../components/DataTable'
import { SearchUserEntityScopeRequest } from '../../models/identity/user/search/SearchUserEntityScopeRequest'
import { SearchUserEntityScopeResponse } from '../../models/identity/user/search/SearchUserEntityScopeResponse'
import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'
import { CheckBoxComponent } from '../../components/editors/CheckBoxComponent'
import { enumToString } from '../../functions'

const SearchUserScope: FC = () => {
  const { identity } = useAppSelector(state => state.identity)
  const [pagedRequest, setPagedRequest] = useState<SearchUserEntityScopeRequest>({})
  const [rawd, setRawd] = useState<PagedResponse<SearchUserEntityScopeResponse> | undefined>(undefined)

  const columns = createColumns([
    createColumn<SearchUserEntityScopeResponse, 'id'>({
      id: 'id',
      accessorKey: 'id',
      header: 'Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'id'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'userId'>({
      id: 'userId',
      accessorKey: 'userId',
      header: 'User Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'userId'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'username'>({
      id: 'username',
      accessorKey: 'username',
      header: 'Username',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'username'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value ?? ''} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'entityId'>({
      id: 'entityId',
      accessorKey: 'entityId',
      header: 'Entity Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityId'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'entityName'>({
      id: 'entityName',
      accessorKey: 'entityName',
      header: 'Entity Name',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityName'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value ?? ''} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'entityType'>({
      id: 'entityType',
      accessorKey: 'entityType',
      header: 'Entity Type',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityType'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={enumToString(ScopeEntityType, props.value) ?? ''} />
    }),
    createColumn<SearchUserEntityScopeResponse, 'scope'>({
      id: 'read',
      accessorKey: 'scope',
      header: 'Read',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'scope'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => (
        <CheckBoxComponent label="" value={(props.data.scope ?? 0) & ScopePermission.Read ? true : false} disabled={true} />
      )
    }),
    createColumn<SearchUserEntityScopeResponse, 'scope'>({
      id: 'write',
      accessorKey: 'scope',
      header: 'Write',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'scope'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => (
        <CheckBoxComponent label="" value={(props.data.scope ?? 0) & ScopePermission.Write ? true : false} disabled={true} />
      )
    }),
    createColumn<SearchUserEntityScopeResponse, 'scope'>({
      id: 'delete',
      accessorKey: 'scope',
      header: 'Delete',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'scope'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => (
        <CheckBoxComponent label="" value={(props.data.scope ?? 0) & ScopePermission.Delete ? true : false} disabled={true} />
      )
    }),
    createColumn<SearchUserEntityScopeResponse, 'scope'>({
      id: 'create',
      accessorKey: 'scope',
      header: 'Create',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'scope'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => (
        <CheckBoxComponent label="" value={(props.data.scope ?? 0) & ScopePermission.Create ? true : false} disabled={true} />
      )
    }),
  ])

  const loadData = useCallback(() => {
    postData<SearchUserEntityScopeRequest, PagedResponse<SearchUserEntityScopeResponse>>(
      GetApiRoute(ApiRoutes.identitySearchUserScopes).route,
      pagedRequest
    ).then((response) => {
      setRawd(response.payload ?? undefined)
    }).finally(() => {})
  }, [pagedRequest])

  useEffect(() => loadData(), [loadData])

  const handleFilterChange = (filters: { [filterId: string]: string }) => {
    setPagedRequest({ ...pagedRequest, ...filters })
  }

  const handlePageChange = (pageNumber: number) => {
    setPagedRequest({ ...pagedRequest, pageNumber })
  }

  if (!identity) return <></>

  return (
    <FormContainer>
      <FormHeader>User scopes</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full'}>
          <DataTable
            colspan={12}
            rawd={rawd}
            columns={columns}
            storageKey={'SearchUserScope'}
            idFields={['id']}
            onFilterChange={handleFilterChange}
            onPreviousPage={handlePageChange}
            onNextPage={handlePageChange}
          />
        </div>
      </FormContent>
    </FormContainer>
  )
}

export { SearchUserScope }

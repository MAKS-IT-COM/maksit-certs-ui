import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../components/FormLayout'
import { useAppSelector } from '../../redux/hooks'
import { postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { PagedResponse } from '../../models/certsUI/common/PagedResponse'
import { createColumn, createColumns, DataTable, DataTableFilter, DataTableLabel } from '../../components/DataTable'
import { SearchApiKeyEntityScopeRequest } from '../../models/certsUI/apiKeys/search/SearchApiKeyEntityScopeRequest'
import { SearchApiKeyEntityScopeResponse } from '../../models/certsUI/apiKeys/search/SearchApiKeyEntityScopeResponse'
import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'
import { CheckBoxComponent } from '../../components/editors/CheckBoxComponent'
import { enumToString } from '../../functions'

const SearchApiKeyScope: FC = () => {
  const { identity } = useAppSelector(state => state.identity)
  const [pagedRequest, setPagedRequest] = useState<SearchApiKeyEntityScopeRequest>({})
  const [rawd, setRawd] = useState<PagedResponse<SearchApiKeyEntityScopeResponse> | undefined>(undefined)

  const columns = createColumns([
    createColumn<SearchApiKeyEntityScopeResponse, 'id'>({
      id: 'id',
      accessorKey: 'id',
      header: 'Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'id'} onFilterChange={onFilterChange} disabled={true} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'apiKeyId'>({
      id: 'apiKeyId',
      accessorKey: 'apiKeyId',
      header: 'API Key Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'apiKeyId'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'description'>({
      id: 'description',
      accessorKey: 'description',
      header: 'Description',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'description'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value ?? ''} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'entityId'>({
      id: 'entityId',
      accessorKey: 'entityId',
      header: 'Entity Id',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityId'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'entityName'>({
      id: 'entityName',
      accessorKey: 'entityName',
      header: 'Entity Name',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityName'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={props.value ?? ''} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'entityType'>({
      id: 'entityType',
      accessorKey: 'entityType',
      header: 'Entity Type',
      filter: (props, onFilterChange) => (
        <DataTableFilter type={'normal'} columnId={props.columnId} accessorKey={'entityType'} onFilterChange={onFilterChange} />
      ),
      cell: (props) => <DataTableLabel type={'normal'} value={enumToString(ScopeEntityType, props.value)} />
    }),
    createColumn<SearchApiKeyEntityScopeResponse, 'scope'>({
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
    createColumn<SearchApiKeyEntityScopeResponse, 'scope'>({
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
    createColumn<SearchApiKeyEntityScopeResponse, 'scope'>({
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
    createColumn<SearchApiKeyEntityScopeResponse, 'scope'>({
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
    postData<SearchApiKeyEntityScopeRequest, PagedResponse<SearchApiKeyEntityScopeResponse>>(
      GetApiRoute(ApiRoutes.apikeySearchEntityScopes).route,
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
      <FormHeader>API key scopes</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full'}>
          <DataTable
            colspan={12}
            rawd={rawd}
            columns={columns}
            storageKey={'SearchApiKeyScope'}
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

export { SearchApiKeyScope }

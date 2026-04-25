import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormHeader } from '../../components/FormLayout'
import { Offcanvas } from '../../components/Offcanvas'
import { CreateApiKey } from './CreateApiKey'
import { useAppSelector } from '../../redux/hooks'
import { deleteData, postData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { PagedResponse } from '../../models/letsEncryptServer/common/PagedResponse'
import { SearchAPIKeyRequest } from '../../models/letsEncryptServer/apiKeys/search/SearchAPIKeyRequest'
import { SearchAPIKeyResponse } from '../../models/letsEncryptServer/apiKeys/search/SearchAPIKeyResponse'
import { ApiKeyResponse } from '../../models/letsEncryptServer/apiKeys/ApiKeyResponse'
import { createColumn, createColumns, DataTable, DataTableFilter, DataTableLabel } from '../../components/DataTable'
import { EditApiKey } from './EditApiKey'
import { extractPropFilter } from '../../functions/dataTableFilters'


const SearchApiKey: FC = () => {

  const { identity } = useAppSelector(state => state.identity)

  const [pagedRequest, setPagedRequest] = useState<SearchAPIKeyRequest>({})
  const [rawd, setRawd] = useState<PagedResponse<SearchAPIKeyResponse> | undefined>(undefined)

  const [apiKeyId, setApiKeyId] = useState<string | undefined>(undefined)
  const [creating, setCreating] = useState<boolean>(false)

  const columns = createColumns([
    createColumn<SearchAPIKeyResponse, 'id'>({
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
      },
    }),
    createColumn<SearchAPIKeyResponse, 'description'>({
      id: 'description',
      accessorKey: 'description',
      header: 'Description',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'description'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => (
        <DataTableLabel type={'normal'} value={props.value ?? ''} />
      ),
    }),
    createColumn<SearchAPIKeyResponse, 'createdAt'>({
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
      cell: (props) => (
        <DataTableLabel type={'normal'} value={props.value} dataType={'date'} />
      ),
    }),
    createColumn<SearchAPIKeyResponse, 'expiresAt'>({
      id: 'expiresAt',
      accessorKey: 'expiresAt',
      header: 'Expires At',
      filter: (props, onFilterChange) => (
        <DataTableFilter
          type={'normal'}
          columnId={props.columnId}
          accessorKey={'expiresAt'}
          onFilterChange={onFilterChange}
        />
      ),
      cell: (props) => (
        <DataTableLabel type={'normal'} value={props.value ?? ''} />
      ),
    }),
  ])

  const loadData = useCallback(() => {
    postData<SearchAPIKeyRequest, PagedResponse<SearchAPIKeyResponse>>(GetApiRoute(ApiRoutes.apikeySearch).route, pagedRequest).then((response) => {
      setRawd(response ?? undefined)
    }).finally(() => {})
  }, [pagedRequest])

  useEffect(() => loadData(), [loadData])


  const handleAddRow = () => {
    setCreating(true)
  }

  const handleEditRow = (ids: {[key: string]: string}) => {
    setApiKeyId(ids.id)
  }

  const handleDeleteRow = (ids: {[key: string]: string}) => {
    deleteData(GetApiRoute(ApiRoutes.apikeyDelete).route
      .replace('{apiKeyId}', ids.id)
    ).then(() => loadData())
  }

  const handleEditCancel = () => {
    setApiKeyId(undefined)
  }

  const handleFilterChange = (filters: { [filterId: string]: string }) => {
    const combined = filters.normal ?? ''
    const descriptionFilter = extractPropFilter(combined, 'Description')
    setPagedRequest({
      ...pagedRequest,
      descriptionFilter: descriptionFilter?.trim() || undefined,
    })
  }

  const handlePageChange = (pageNumber: number) => {
    setPagedRequest({
      ...pagedRequest,
      pageNumber
    })
  }

  const handleOnSubmitted = (_item?: ApiKeyResponse) => {
    setApiKeyId(undefined)

    setCreating(false)

    loadData()
  }

  if (!identity) return <></>

  const handleAllowAddRow = () => true

  const handleAllowEditRow = (_ids: Record<string, string>) => true

  const handleAllowDeleteRow = (_ids: Record<string, string>) => true

  return <>
    <FormContainer>
      <FormHeader>API Keys</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full'}>
          <DataTable
            colspan={12}
            rawd={rawd}
            columns={columns}
            storageKey={'SearchApiKey'}
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

    <Offcanvas isOpen={apiKeyId !== undefined}>
      {apiKeyId && <EditApiKey
        key={apiKeyId}
        apiKeyId={apiKeyId}
        cancelEnabled={true}
        onSubmitted={handleOnSubmitted}
        onCancel={handleEditCancel}
      />}
    </Offcanvas>

    {creating && (
      <CreateApiKey
        isOpen={true}
        onSubmitted={handleOnSubmitted}
        cancelEnabled={true}
        onCancel={() => setCreating(false)}
      />
    )}
  </>
}

export {
  SearchApiKey,
}

import type { PagedRequest, PagedResponse, SearchResponseBase } from '@maks-it.com/webui-contracts'
import type {
  DataTableRemoteFilterDataSource,
  DataTableRemoteLabelDataSource,
  RemoteSelectSearchDataSource,
} from '@maks-it.com/webui-components'
import { getDataWithoutLoader, postData, postDataWithoutLoader } from '../axiosConfig'
import { store } from '../redux/store'
import { disableLoader, enableLoader } from '../redux/slices/loaderSlice'

export const createRemoteSelectDataSource = <TRequest extends PagedRequest>(
  route: string,
): RemoteSelectSearchDataSource<TRequest> => {
  return (request, options) => {
    if (!options?.showLoader) {
      store.dispatch(disableLoader())
    }

    return postData<TRequest, PagedResponse<SearchResponseBase>>(route, request)
      .then((response) => (response.ok ? response.payload?.items : undefined))
      .finally(() => {
        if (!options?.showLoader) {
          store.dispatch(enableLoader())
        }
      })
  }
}

export const createDataTableRemoteFilterDataSource = <T extends { id: string }>(
  route: string,
): DataTableRemoteFilterDataSource<T> => {
  return (filters) =>
    postDataWithoutLoader<{ pageSize: number; filters: string }, PagedResponse<T>>(route, {
      pageSize: 100,
      filters,
    }).then((response) => (response.ok ? response.payload?.items : undefined))
}

export const createDataTableRemoteLabelDataSource = <T extends Record<string, unknown>>(
  route: string,
): DataTableRemoteLabelDataSource<T> => {
  return () =>
    getDataWithoutLoader<T>(route).then((response) => (response.ok ? response.payload : undefined))
}

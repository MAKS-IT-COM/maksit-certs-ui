import { createWebUiHttpClient, readIdentity, type ApiResponse } from '@maks-it.com/webui-core'
import { addToast } from '@maks-it.com/webui-components'
import { ApiRoutes, GetApiRoute } from './apiRoutes'
import { store } from './redux/store'
import { clearIdentity } from './redux/slices/identitySlice'
import { refreshWebUiAccessToken } from './webUiAuthRefresh'
import { hideLoader, showLoader } from './redux/slices/loaderSlice'

const {
  axiosInstance,
  getData,
  postData,
  patchData,
  putData,
  deleteData,
  postBinary,
  getBinary,
  postFormData,
  postFile,
  getDataWithoutLoader,
  postDataWithoutLoader,
} = createWebUiHttpClient({
  auth: {
    readIdentity,
    refreshToken: refreshWebUiAccessToken,
    clearIdentity: () => {
      store.dispatch(clearIdentity())
    },
    showLoader: () => {
      store.dispatch(showLoader())
    },
    hideLoader: () => {
      store.dispatch(hideLoader())
    },
    authExcludedUrls: [
      GetApiRoute(ApiRoutes.identityLogin).route,
      GetApiRoute(ApiRoutes.identityRefresh).route,
    ],
    onErrorToast: (message) => addToast(message, 'error'),
  },
})

export {
  type ApiResponse,
  axiosInstance,
  getData,
  postData,
  patchData,
  putData,
  deleteData,
  postBinary,
  getBinary,
  postFormData,
  postFile,
  getDataWithoutLoader,
  postDataWithoutLoader,
}

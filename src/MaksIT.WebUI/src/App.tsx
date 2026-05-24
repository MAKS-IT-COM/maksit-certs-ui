
import { Routes } from 'react-router-dom'
import { GetRoutes } from './AppMap'
import { ToastContainer as Toast } from '@maks-it.com/webui-components'
import { Loader } from './components'

const App = () => {

  return <>
    <Routes>
      {GetRoutes()}
    </Routes>
    <Loader />
    <Toast />
  </>
}

export {
  App
}

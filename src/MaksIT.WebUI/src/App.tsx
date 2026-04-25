
import { Routes } from 'react-router-dom'
import { GetRoutes } from './AppMap'
import { Loader } from './components/Loader'
import { Toast } from './components/Toast'

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

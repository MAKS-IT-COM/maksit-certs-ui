
import { Routes } from 'react-router-dom'
import { GetRoutes } from './AppMap'
import { Loader } from './components/Loader'

const App = () => {

  return <>
    <Routes>
      {GetRoutes()}
    </Routes>
    <Loader />
  </>
}

export {
  App
}

import { Link, Route } from 'react-router-dom'
import { HomePage } from './pages/HomePage'
import { ComponentType, FC, ReactNode } from 'react'
import { Layout } from './components/Layout'
import { LoginScreen } from './components/LoginScreen'
import { Authorization } from './components/Authorization'
import { UserOffcanvas } from './components/UserOffcanvas'
import { UserButton } from './components/UserButton'
import { Toast } from './components/Toast'
import { UtilitiesPage } from './pages/UtilitiesPage'
import { RegisterPage } from './pages/RegisterPage'


interface LayoutWrapperProps {
    children: ReactNode
  }
  
const LayoutWrapper: FC<LayoutWrapperProps> = (props) => {
  const { children } = props

  return <Layout
    sideMenu={
      {
        headerChildren:  <p>{import.meta.env.VITE_APP_TITLE}</p> ,
        children: <ul>
          {GetMenuItems(LinkArea.SideMenu)}
        </ul>,
        footerChildren: <></>
      }
    }
    header={
      {
        children: <>
          <ul className={'flex space-x-4'}>
            {/* <li>Item 1</li> */}
          </ul>
          <ul className={'flex space-x-4'}>
            <li>
              <UserButton />
            </li>
          </ul>
        </>
      }
    }
    footer={
      {
        children: <p>&copy; {new Date().getFullYear()} {import.meta.env.VITE_COMPANY}</p>
      }
    }
  >{children}</Layout>
}

interface AppMapType {
    title: string,
    routes: string[],
    page: ComponentType,
    useAuth?: boolean,
    useLayout?: boolean
    linkArea?: LinkArea []
}

enum LinkArea {
    SideMenu,
    TopMenuLeft,
    TopMenuRigt
}

const AppMap: AppMapType[] = [
  {
    title: 'Home',
    routes: ['/', '/home'],
    page: HomePage,
    linkArea: [LinkArea.SideMenu]
  },
  {
    title: 'Register',
    routes: ['/register'],
    page: RegisterPage,
    linkArea: [LinkArea.SideMenu]
  },
  {
    title: 'Utilities',
    routes: ['/utilities'],
    page: UtilitiesPage,
    linkArea: [LinkArea.SideMenu]
  }

  // {
  //   title: 'Login',
  //   routes: ['/login'],
  //   page: LoginScreen,
  //   useAuth: false,
  //   useLayout: false
  // },
  // {
  //   title: 'About',
  //   routes: ['/about'],
  //   page: Home
  // },
  // {
  //   title: 'User',
  //   routes: ['/user/:userId'],
  //   page: Users
  // },
  // {
  //   title: 'Organizations',
  //   routes: ['/organizations', '/organization/:organizationId'],
  //   page: Organizations,
  //   linkArea: [LinkArea.SideMenu]
  // },
  // {
  //   title: 'Applications',
  //   routes: ['/applications'],
  //   page: Applications,
  //   linkArea: [LinkArea.SideMenu]
  // },
  // {
  //   title: 'Secrets',
  //   routes: ['/secrets'],
  //   page: Secrets,
  //   linkArea: [LinkArea.SideMenu]
  // },
  // {
  //   title: 'Users',
  //   routes: ['/users'],
  //   page: Users,
  //   linkArea: [LinkArea.SideMenu]
  // },
  // {
  //   title: 'API Keys',
  //   routes: ['/apikeys'],
  //   page: ApiKeys,
  //   linkArea: [LinkArea.SideMenu]
  // }
]

// AGENT_TEST = 'api/agent/test',




enum ApiRoutes {
  ACCOUNTS = 'GET|/accounts',

  ACCOUNT_POST = 'POST|/account',
  ACCOUNT_GET = 'GET|/account/{accountId}',
  ACCOUNT_PATCH = 'PATCH|/account/{accountId}',
  ACCOUNT_DELETE = 'DELETE|/account/{accountId}',

  // ACCOUNT_ID_CONTACTS = 'GET|/account/{accountId}/contacts',
  // ACCOUNT_ID_CONTACT_ID = 'GET|/account/{accountId}/contact/{index}',

  // ACCOUNT_ID_HOSTNAMES = 'GET|/account/{accountId}/hostnames',
  // ACCOUNT_ID_HOSTNAME_ID = 'GET|/account/{accountId}/hostname/{index}',

  // Secrets
  generateSecret = 'GET|/secret/generatesecret',

  // Identity
  identityLogin = 'POST|/identity/login',
  identityRefresh = 'POST|/identity/refresh',
  identityLogout = 'POST|/identity/logout',
}

const GetMenuItems = (linkArea: LinkArea) => {
  return AppMap.filter(item => item.linkArea?.includes(linkArea)).map((item, index) => {
    return <li key={index}><Link to={item.routes[0]}>{item.title}</Link></li>
  })
}

const GetRoutes = () => {
  return AppMap.flatMap((item) => 
    item.routes.map((route) => {
      const {
        useAuth = true,
        useLayout = true,
        page: Page
      } = item

      const PageComponent = (
        <>
          {useLayout ? (
            <LayoutWrapper>
              <Page />
            </LayoutWrapper>
          ) : (
            <Page />
          )}
          <Toast />
        </>
      )

      return (
        <Route
          key={route}
          path={route}
          // element={useAuth
          //   ? <Authorization>
          //     {PageComponent}
          //     <UserOffcanvas />
          //   </Authorization>
          //   : PageComponent}

          element={PageComponent}
        />
      )
    })
  )
}

interface ApiRoute {
  method: string,
  route: string
}

const GetApiRoute = (apiRoute: ApiRoutes): ApiRoute => {
  const apiUrl = import.meta.env.VITE_API_URL

  const [method, route] = apiRoute.split('|')

  return {
    method,
    route: `${apiUrl}${route}`
  }
}

export {
  GetMenuItems,
  GetRoutes,
  ApiRoutes,
  GetApiRoute
}
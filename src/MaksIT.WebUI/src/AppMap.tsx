import { Link, Route } from 'react-router-dom'
import { HomePage } from './pages/HomePage'
import { ComponentType, FC, ReactNode } from 'react'
import { Layout } from '@maks-it.com/webui-components'
import { Authorization, LoginScreen, UserButton, UserOffcanvas } from './components'
import { UtilitiesPage } from './pages/UtilitiesPage'
import { RegisterPage } from './pages/RegisterPage'
import { LetsEncryptTermsOfServicePage } from './pages/LetsEncryptTermsOfServicePage'
import { Users } from './pages/Users'
import { UserScopes } from './pages/UserScopes'
import { ApiKeys } from './pages/ApiKeys'
import { ApiKeyScopes } from './pages/ApiKeyScopes'


declare global {
  interface Window {
    RUNTIME_CONFIG?: {
      API_URL?: string;
    };
  }
}

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
        children: <p>v{import.meta.env.VITE_APP_VERSION} - &copy; {new Date().getFullYear()} <a
            href={import.meta.env.VITE_COMPANY_URL}
            target={'_blank'}
            rel={'noopener noreferrer'}>
            {import.meta.env.VITE_COMPANY}
          </a>
        </p>
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
  },
  {
    title: 'Terms of Service',
    routes: ['/terms-of-service'],
    page: LetsEncryptTermsOfServicePage,
  },

  {
    title: 'Login',
    routes: ['/login'],
    page: LoginScreen,
    useAuth: false,
    useLayout: false
  },

  {
    title: 'Users',
    routes: ['/users'],
    page: Users,
    linkArea: [LinkArea.SideMenu],
  },
  {
    title: 'User scopes',
    routes: ['/users/scopes'],
    page: UserScopes,
    linkArea: [LinkArea.SideMenu],
  },
  {
    title: 'User',
    routes: ['/user/:userId'],
    page: Users,
  },
  {
    title: 'API keys',
    routes: ['/apikeys'],
    page: ApiKeys,
    linkArea: [LinkArea.SideMenu],
  },
  {
    title: 'API key scopes',
    routes: ['/apikeys/scopes'],
    page: ApiKeyScopes,
    linkArea: [LinkArea.SideMenu],
  },
  {
    title: 'API key',
    routes: ['/apikeys/:apiKeyId'],
    page: ApiKeys,
  },
]

export { ApiRoutes, GetApiRoute } from './apiRoutes'
export type { ApiRoute } from './apiRoutes'

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
        </>
      )

      return (
        <Route
          key={route}
          path={route}
          element={useAuth
            ? <Authorization>
              {PageComponent}
              <UserOffcanvas />
            </Authorization>
            : PageComponent}
        />
      )
    })
  )
}

export {
  GetMenuItems,
  GetRoutes,
}
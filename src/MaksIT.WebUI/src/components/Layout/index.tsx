import { FC } from 'react'
import { MainContainer } from './MainContainer'
import { SideMenu, SideMenuProps } from './SideMenu'
import { Container } from './Container'
import { Header, HeaderProps } from './Header'
import { Footer, FooterProps } from './Footer'
import { Content } from './Content'

interface LayoutProps {
    sideMenu: SideMenuProps
    header: HeaderProps
    children: React.ReactNode
    footer: FooterProps
}

const Layout: FC<LayoutProps> = (props) => {
  const { sideMenu, header, children, footer } = props
    
  return <MainContainer>
    <SideMenu
      headerChildren={sideMenu.headerChildren}
      footerChildren={sideMenu.footerChildren}
    >
      {sideMenu.children}
    </SideMenu>
    <Container>
      <Header>
        {header.children}
      </Header>
      <Content>
        {children}
      </Content>
      <Footer>{footer.children}</Footer>
    </Container>
  </MainContainer>
}

export {
  Layout
}
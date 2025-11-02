import { FC, ReactNode } from 'react'
import { Container } from '../Container'
import { Header } from './Header'
import { Content } from './Content'
import { Footer } from './Footer'


export interface SideMenuProps {
    headerChildren?: ReactNode
    children: ReactNode
    footerChildren?: ReactNode
}

const SideMenu: FC<SideMenuProps> = (props) => {
  const { headerChildren, children, footerChildren } = props
    
  return <Container>
    <Header>
      {headerChildren}
    </Header>
    <Content>
      {children}
    </Content>
    <Footer>
      {footerChildren}
    </Footer>
  </Container>
}

export {
  SideMenu
}
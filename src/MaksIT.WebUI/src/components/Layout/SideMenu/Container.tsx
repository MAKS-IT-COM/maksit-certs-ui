import { FC, ReactNode } from 'react'

export interface ContainerProps {
    headerChildren?: ReactNode
    children: ReactNode
    footerChildren?: ReactNode
}

const Container: FC<ContainerProps> = (props) => {
  const { children } = props
    
  return <aside className={'grid grid-rows-[auto_1fr_auto] h-screen'}>
    {children}
  </aside>
}

export {
  Container
}
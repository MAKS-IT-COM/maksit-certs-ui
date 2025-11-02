import { FC, ReactNode } from 'react'

interface ContainerProps {
    children: ReactNode
}

const MainContainer: FC<ContainerProps> = (props) => {
  const { children } = props

  return <div className={'grid grid-cols-[250px_1fr] h-screen w-screen'}>
    {children}
  </div>
}

export {
  MainContainer
}
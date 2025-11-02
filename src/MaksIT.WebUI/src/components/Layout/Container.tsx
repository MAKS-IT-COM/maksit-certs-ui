import { FC, ReactNode } from 'react'

interface ContentProps {
    children: ReactNode
}

const Container: FC<ContentProps> = (props) => {
  const { children } = props

  return <div className={'grid grid-rows-[auto_1fr_auto] h-screen'}>
    {children}
  </div>
}

export {
  Container
}
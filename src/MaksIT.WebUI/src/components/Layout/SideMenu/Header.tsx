import { FC, ReactNode } from 'react'

export interface HeaderProps {
    children: ReactNode
}

const Header: FC<HeaderProps> = (props) => {
  const { children } = props

  return <header>
    <div className={'bg-blue-500 text-white p-4 h-14'}>
      {children}
    </div>
  </header>
}

export {
  Header
}
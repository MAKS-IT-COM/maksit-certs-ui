import { FC, ReactNode } from 'react'

export interface FooterProps {
    children: ReactNode
}

const Footer: FC<FooterProps> = (props) => {
  const { children } = props
  return <footer className={'bg-blue-500 text-white p-4 h-14 flex items-center justify-center'}>
    {children}
  </footer>
}

export {
  Footer
}
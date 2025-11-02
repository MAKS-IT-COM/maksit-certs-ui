import { FC, ReactNode } from 'react'

interface ContentProps {
    children: ReactNode
}

const Content: FC<ContentProps> = (props) => {
  const { children } = props

  return <main className={'bg-gray-200 h-full p-4 overflow-y-auto border-r border-blue-500'}>
    {children}
  </main>
}

export {
  Content
}
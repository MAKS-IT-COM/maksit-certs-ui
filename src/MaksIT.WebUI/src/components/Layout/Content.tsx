import { FC, ReactNode } from 'react'

interface ContentProps {
    children: ReactNode
}

const Content: FC<ContentProps> = (props) => {
  const { children } = props

  return <main className={'bg-gray-100 h-full w-full overflow-y-auto'}>
    {children}
  </main>
}

export {
  Content
}
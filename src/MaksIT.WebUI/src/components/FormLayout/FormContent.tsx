import { FC, ReactNode } from 'react'

interface FormContentProps {
    children?: ReactNode
}

const FormContent: FC<FormContentProps> = (props) => {
  const {
    children
  } = props

  return <div className={'bg-gray-100 w-full h-full p-4 overflow-y-auto'}>
    {children}
  </div>
}

export {
  FormContent
}
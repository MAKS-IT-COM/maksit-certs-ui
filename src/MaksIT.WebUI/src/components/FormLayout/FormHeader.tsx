import { FC, ReactNode } from 'react'

interface FormHeaderProps {
    children?: ReactNode
}

const FormHeader: FC<FormHeaderProps> = (props) => {
  const {
    children
  } = props

  return <h1 className={'bg-gray-200 p-4 h-14 text-2xl font-bold'}>
    {children}
  </h1>
}

export {
  FormHeader
}
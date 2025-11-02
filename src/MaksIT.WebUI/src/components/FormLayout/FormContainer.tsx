import { FC, ReactNode } from 'react'

interface FormContainerProps {
    children?: ReactNode
}

const FormContainer: FC<FormContainerProps> = (props) => {
  const {
    children
  } = props

  return <div className={'grid grid-rows-[auto_1fr_auto] h-full gap-0'}>
    {children}
  </div>
}

export {
  FormContainer
}
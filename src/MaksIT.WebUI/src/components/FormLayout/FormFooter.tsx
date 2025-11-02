import { FC, ReactNode } from 'react'


interface FormFooterProps {
    children?: ReactNode,
    leftChildren?: ReactNode,
    rightChildren?: ReactNode
}

const FormFooter: FC<FormFooterProps> = (props) => {

  const {
    children,
    leftChildren,
    rightChildren
  } = props

  return <div className={'bg-gray-200 p-4 h-14 flex justify-between items-center'}>
    {children ?? <>
      <div className={'flex space-x-4'}>{leftChildren}</div>
      <div className={'flex space-x-4'}>{rightChildren}</div>
    </>}


  </div>

}

export {
  FormFooter
}
import { FC, ReactNode } from 'react'

interface FieldContainerProps {
    colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
    label?: string;
    errorText?: string;
    children: ReactNode
}

const FieldContainer: FC<FieldContainerProps> = (props) => {
  const {
    colspan,
    label,
    errorText,
    children
  } = props

  return <div className={`${colspan ? `col-span-${colspan}` : 'w-full'}`}>
    <label className={`block text-gray-700 text-sm font-bold mb-2  ${!label ? 'invisible' : ''}`}>{label || '\u00A0'}</label>
    {children}
    <p className={`text-red-500 text-xs italic mt-2 ${!errorText ? 'invisible' : ''}`}>{errorText || '\u00A0'}</p>
  </div>
}


export {
  FieldContainer
}
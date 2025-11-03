import { useEffect, useRef } from 'react'
import { FieldContainer } from './FieldContainer'

interface CheckBoxComponentProps {
    colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
    label: string;
    value: boolean;
    onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
    errorText?: string;
    disabled?: boolean;
}

const CheckBoxComponent: React.FC<CheckBoxComponentProps> = (props) => {

  const {
    colspan = 6,
    label,
    value,
    onChange,
    errorText,
    disabled = false
  } = props

  const prevValue = useRef<boolean>(value)

  useEffect(() => {
    prevValue.current = value
  }, [value])

  const handleOnChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (prevValue.current === e.target.checked)
      return

    prevValue.current = e.target.checked

    onChange?.(e)
  }

  return (
    <FieldContainer colspan={colspan} label={label} errorText={errorText}>
      <input
        type={'checkbox'}
        checked={value}
        onChange={handleOnChange}
        className={`mr-2 leading-tight ${errorText ? 'border-red-500' : ''}`}
        disabled={disabled}
      />
    </FieldContainer>
  )
}

export {
  CheckBoxComponent
}
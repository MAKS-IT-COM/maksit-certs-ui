import { Copy, Dices, Eye, EyeOff } from 'lucide-react'
import { ChangeEvent, FC, useRef, useState } from 'react'
import { TrngResponse } from '../../models/TrngResponse'
import { getData } from '../../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../../AppMap'


interface PasswordGeneratorProps {
  label: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  errorText?: string
  value?: string
  onChange?: (e: ChangeEvent<HTMLInputElement>) => void
  placeholder?: string
  readOnly?: boolean
  enableCopy?: boolean
  enableGenerate?: boolean
}

const SecretComponent: FC<PasswordGeneratorProps> = (props) => {

  const {
    label,
    colspan = 12,
    errorText,
    value = '',
    onChange,
    placeholder,
    readOnly = false,
    enableCopy = false,
    enableGenerate = false
  } = props

  const prevValue = useRef<string>(value)

  // Stato locale per alternare la visibilità della password
  const [showPassword, setShowPassword] = useState(false)

  const handleOnChange = (e: ChangeEvent<HTMLInputElement>) => {
    if (prevValue.current === e.target.value)
      return

    prevValue.current = e.target.value

    onChange?.(e)
  }

  const handleGenerateSecret = () => {
    getData<TrngResponse>(`${GetApiRoute(ApiRoutes.generateSecret).route}`)
      .then(response => {
        if (!response) return
  
        const fakeEvent = {
          target: { value: response.secret }
        } as ChangeEvent<HTMLInputElement>

        handleOnChange(fakeEvent)
      })
  }

  const handleCopy = async () => {
    if (!value) return
    
    await navigator.clipboard.writeText(value)
  }

  // Controlla se c'è contenuto per mostrare il pulsante di toggle
  const hasContent = String(value).length > 0

  const actionButtons = () => {
    
    const className = 'p-1 text-gray-600 hover:text-gray-800 bg-white'

    return [
      hasContent && (
        <button
          key={'eye'}
          type={'button'}
          onClick={() => setShowPassword(prev => !prev)}
          className={className}
          tabIndex={-1}
          aria-label={showPassword ? 'Hide password' : 'Show password'}
        >
          {showPassword ? <EyeOff /> : <Eye />}
        </button>
      ),

      enableGenerate && !readOnly && (
        <button
          key={'generate'}
          type={'button'}
          onClick={handleGenerateSecret}
          className={className}
          tabIndex={-1}
          aria-label={'Generate secret'}
        >
          <Dices />
        </button>
      ),
      enableCopy && hasContent && (
        <button
          key={'copy'}
          type={'button'}
          onClick={handleCopy}
          className={className}
          tabIndex={-1}
          aria-label={'Copy secret'}
        >
          <Copy />
        </button>
      ),
    ].filter(Boolean)
  }
  

  return (
    <div className={`mb-4 ${colspan ? `col-span-${colspan}` : 'w-full'}`}>
      <label className={'block text-gray-700 text-sm font-bold mb-2'}>{label}</label>

      <div className={'relative'}>
        <input
          type={showPassword ? 'text' : 'password'}
          value={value}
          onChange={handleOnChange}
          placeholder={placeholder}
          className={`
            shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline
            ${errorText ? 'border-red-500' : ''}
            ${readOnly ? 'bg-gray-100 text-gray-500' : ''}
          `}
          readOnly={readOnly}
        />

        {/* Action buttons container */}
        <div
          className={'absolute top-0 bottom-0 right-2 flex items-center gap-1 pointer-events-auto'}
        >
          {actionButtons()}
        </div>
      </div>

      {errorText && <p className={'text-red-500 text-xs italic mt-2'}>{errorText}</p>}
    </div>
  )
}

export { SecretComponent }
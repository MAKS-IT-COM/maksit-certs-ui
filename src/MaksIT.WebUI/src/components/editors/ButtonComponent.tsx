import { ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface CommonButtonProps {
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  route?: string;
  buttonHierarchy?: 'primary' | 'secondary' | 'success' | 'error' | 'warning';
  onClick?: () => void;
  disabled?: boolean;
}

type ButtonComponentProps =
  | ({ label: string; children?: never } & CommonButtonProps)
  | ({ children: ReactNode; label?: never } & CommonButtonProps);

const ButtonComponent: React.FC<ButtonComponentProps> = (props) => {
  const {
    colspan,
    route,
    buttonHierarchy,
    onClick,
    disabled = false
  } = props

  const isChildren = 'children' in props && props.children !== undefined
  const content = 'label' in props ? props.label : props.children

  const handleClick = (e?: React.MouseEvent) => {
    if (disabled) {
      e?.preventDefault()
      return
    }
    onClick?.()
  }

  let buttonClass = ''
  switch (buttonHierarchy) {
  case 'primary':
    buttonClass = 'bg-blue-500 text-white'
    break
  case 'secondary':
    buttonClass = 'bg-gray-500 text-white'
    break
  case 'success':
    buttonClass = 'bg-green-500 text-white'
    break
  case 'warning':
    buttonClass = 'bg-yellow-500 text-white'
    break
  case 'error':
    buttonClass = 'bg-red-500 text-white'
    break
  default:
    buttonClass = 'bg-blue-500 text-white'
    break
  }

  const disabledClass = disabled ? 'opacity-50 cursor-default' : 'cursor-pointer'

  const centeringClass = isChildren ? 'flex justify-center items-center' : 'text-center'

  return route
    ? (
      <Link
        to={route}
        className={`${buttonClass} px-4 py-2 rounded ${colspan ? `col-span-${colspan}` : 'w-full'} ${centeringClass} ${disabledClass}`}
        onClick={handleClick}
        tabIndex={disabled ? -1 : undefined}
        aria-disabled={disabled}
        style={disabled ? { pointerEvents: 'none' } : undefined}
      >
        {content}
      </Link>
    ) : (
      <button
        className={`${buttonClass} px-4 py-2 rounded ${colspan ? `col-span-${colspan}` : 'w-full'} ${centeringClass} ${disabledClass}`}
        onClick={handleClick}
        disabled={disabled}
      >
        {content}
      </button>
    )
}

export { ButtonComponent }
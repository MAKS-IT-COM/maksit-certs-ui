import { ChangeEvent, FC, useState, useEffect, useRef } from 'react'
import { parseISO, formatISO, format, getDaysInMonth, addMonths, subMonths } from 'date-fns'
import { ButtonComponent } from './ButtonComponent'
import { TextBoxComponent } from './TextBoxComponent'
import { CircleX } from 'lucide-react'
import { FieldContainer } from './FieldContainer'
import { getInputClasses } from './editorStyles'

const DISPLAY_FORMAT = 'yyyy-MM-dd HH:mm'

interface DateTimePickerComponentProps {
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  label: string
  value?: string
  onChange?: (isoString?: string) => void
  errorText?: string
  placeholder?: string
  readOnly?: boolean
  disabled?: boolean
}

const DateTimePickerComponent: FC<DateTimePickerComponentProps> = ({
  colspan = 6,
  label,
  value,
  onChange,
  errorText,
  placeholder,
  readOnly = false,
  disabled = false,
}) => {
  const prevValueRef = useRef<string | undefined>(value)
  const parsedValue = value ? parseISO(value) : undefined

  const [showDropdown, setShowDropdown] = useState(false)
  const [currentViewDate, setCurrentViewDate] = useState<Date>(parsedValue || new Date())
  const [tempDate, setTempDate] = useState<Date>(parsedValue || new Date())

  const dropdownRef = useRef<HTMLDivElement>(null)

  const formatForDisplay = (date: Date) => format(date, DISPLAY_FORMAT)

  const daysCount = getDaysInMonth(currentViewDate)
  const daysArray = Array.from({ length: daysCount }, (_, i) => i + 1)

  const handlePrevMonth = () => setCurrentViewDate((prev) => subMonths(prev, 1))
  const handleNextMonth = () => setCurrentViewDate((prev) => addMonths(prev, 1))

  const handleDayClick = (day: number) => {
    const newDate = new Date(tempDate)
    newDate.setFullYear(currentViewDate.getFullYear(), currentViewDate.getMonth(), day)
    setTempDate(newDate)
  }

  const handleTimeChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const [hours, minutes] = e.target.value.split(':').map(Number)
    const newDate = new Date(tempDate)
    newDate.setHours(hours, minutes, 0, 0)
    setTempDate(newDate)
  }

  const handleClear = () => {
    if (prevValueRef.current !== undefined) {
      onChange?.(undefined)
      prevValueRef.current = undefined
    }
    setShowDropdown(false)
  }

  const handleConfirm = () => {
    const isoString = formatISO(tempDate, { representation: 'complete' })
    if (isoString !== prevValueRef.current) {
      onChange?.(isoString)
      prevValueRef.current = isoString
    }
    setShowDropdown(false)
  }

  const handleOpen = () => {
    if (readOnly || disabled) {
      return
    }

    const newDate = parsedValue || new Date()
    setCurrentViewDate(newDate)
    setTempDate(newDate)
    prevValueRef.current = value
    setShowDropdown(true)
  }

  const actionButtons = () => {
    const className = 'p-1 text-gray-600 hover:text-gray-800 bg-white'
    return [
      !!value && !readOnly && (
        <button
          key={'clear'}
          type={'button'}
          onClick={handleClear}
          className={className}
          tabIndex={-1}
          aria-label={'Clear'}
        >
          <CircleX />
        </button>
      ),
    ].filter(Boolean)
  }

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false)
      }
    }

    if (showDropdown) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [showDropdown])

  return (
    <FieldContainer colspan={colspan} label={label} errorText={errorText}>
      <div className={'relative'} ref={dropdownRef}>
        <input
          type={'text'}
          value={value ? formatForDisplay(parsedValue!) : ''}
          onFocus={handleOpen}
          readOnly
          placeholder={placeholder}
          className={getInputClasses({ errorText, disabled, readOnly })}
          disabled={disabled}
        />

        {/* Fixed Action Buttons */}
        <div className={'absolute top-0 bottom-0 right-2 flex items-center gap-1 pointer-events-auto'}>
          {actionButtons()}
        </div>

        {showDropdown && !readOnly && !disabled && (
          <div className={'absolute left-0 top-full mt-1 w-72 min-w-0 bg-white border border-gray-300 rounded shadow-lg z-10'}>
            <div className={'flex justify-between items-center px-2 py-1.5'}>
              <button onClick={handlePrevMonth} type={'button'} className={'rounded py-1 px-2 text-gray-700 hover:bg-gray-100'}>
                &lt;
              </button>
              <span className={'text-sm'}>{format(currentViewDate, 'MMMM yyyy')}</span>
              <button onClick={handleNextMonth} type={'button'} className={'rounded py-1 px-2 text-gray-700 hover:bg-gray-100'}>
                &gt;
              </button>
            </div>
            <div className={'grid grid-cols-7 gap-0.5 px-2 pb-1.5'}>
              {daysArray.map((day) => (
                <div
                  key={day}
                  onClick={() => handleDayClick(day)}
                  className={`p-1.5 cursor-pointer text-center text-sm ${
                    tempDate.getDate() === day &&
                    tempDate.getMonth() === currentViewDate.getMonth() &&
                    tempDate.getFullYear() === currentViewDate.getFullYear()
                      ? 'bg-blue-500 text-white rounded'
                      : 'hover:bg-gray-200 rounded'
                  }`}
                >
                  {day}
                </div>
              ))}
            </div>
            <div className={'px-2 py-1.5'}>
              <TextBoxComponent
                label={'Time'}
                type={'time'}
                value={format(tempDate, 'HH:mm')}
                onChange={handleTimeChange}
                placeholder={'HH:MM'}
                readOnly={readOnly}
              />
            </div>
            <div className={'px-2 py-1.5 gap-2 flex justify-between'}>
              <ButtonComponent label={'Clear'} buttonHierarchy={'secondary'} onClick={handleClear} />
              <ButtonComponent label={'Confirm'} buttonHierarchy={'primary'} onClick={handleConfirm} />
            </div>
          </div>
        )}
      </div>
    </FieldContainer>
  )
}

export { DateTimePickerComponent }

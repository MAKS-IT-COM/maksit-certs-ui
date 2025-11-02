import React, { useState } from 'react'

interface DualListboxComponentProps {
  label?: string; 
  availableItemsLabel?: string;
  selectedItemsLabel?: string;
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;

  idFieldName?: string;
  availableItems: string[];
  selectedItems: string[];
  onChange: (selectedItems: string[]) => void;
  errorText?: string;
}

const DualListboxComponent: React.FC<DualListboxComponentProps> = (props) => {

  const {
    label,
    availableItemsLabel = 'Available Items',
    selectedItemsLabel = 'Selected Items',
    colspan = 6,
    availableItems,
    selectedItems,
    onChange,
    errorText
  } = props

  const [available, setAvailable] = useState<string[]>(availableItems)
  const [selected, setSelected] = useState<string[]>(selectedItems)

  const moveToSelected = () => {
    const movedItems = available.filter(item => selected.includes(item))
    setAvailable(available.filter(item => !movedItems.includes(item)))
    setSelected([...selected, ...movedItems])
    onChange([...selected, ...movedItems])
  }

  const moveToAvailable = () => {
    const movedItems = selected.filter(item => !available.includes(item))
    setSelected(selected.filter(item => !movedItems.includes(item)))
    setAvailable([...available, ...movedItems])
    onChange(selected.filter(item => !movedItems.includes(item)))
  }

  return (
    <div className={`col-span-${colspan}`}>
      <label className={'block text-gray-700 text-sm font-bold mb-2'}>
        {label}
      </label>
      <div className={'flex justify-center items-center gap-4 w-full h-full'}>
        <div className={'flex flex-col'}>
          <h3>{availableItemsLabel}</h3>
          <ul className={'border p-2 w-40 h-64 overflow-auto'}>
            {available.map(item => (
              <li
                key={item}
                className={'cursor-pointer hover:bg-gray-200'}
                onClick={() => setAvailable(available.filter(i => i !== item))}
              >
                {item}
              </li>
            ))}
          </ul>
        </div>
        <div className={'flex flex-col gap-2'}>
          <button
            onClick={moveToSelected}
            className={'border px-4 py-2 bg-blue-500 text-white hover:bg-blue-600'}
          >
            &gt;
          </button>
          <button
            onClick={moveToAvailable}
            className={'border px-4 py-2 bg-red-500 text-white hover:bg-red-600'}
          >
            &lt;
          </button>
        </div>
        <div className={'flex flex-col'}>
          <h3>{selectedItemsLabel}</h3>
          <ul className={'border p-2 w-40 h-64 overflow-auto'}>
            {selected.map(item => (
              <li
                key={item}
                className={'cursor-pointer hover:bg-gray-200'}
                onClick={() => setSelected(selected.filter(i => i !== item))}
              >
                {item}
              </li>
            ))}
          </ul>
        </div>
      </div>
      {errorText && (
        <p className={'text-red-500 text-xs italic mt-2'}>
          {errorText}
        </p>
      )}
    </div>
  )
}

export {
  DualListboxComponent
}
/**
 * Shared Tailwind classes for editor components (TextBox, SelectBox, DateTimePicker, Secret).
 * Keeps input styling uniform and avoids drift.
 */

export const inputBaseClasses =
  'border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500/30'

export interface InputClassOptions {
  errorText?: string
  disabled?: boolean
  readOnly?: boolean
  /** Extra classes (e.g. pr-10 for input with trailing button) */
  extra?: string
}

export function getInputClasses(options: InputClassOptions): string {
  const { errorText, disabled = false, readOnly = false, extra = '' } = options
  const border = errorText ? 'border-red-500' : 'border-gray-300'
  const state =
    disabled
      ? 'bg-gray-100 text-gray-500 cursor-default'
      : 'bg-white' + (readOnly ? ' text-gray-500 cursor-default' : '')
  return [inputBaseClasses, border, state, extra].filter(Boolean).join(' ')
}

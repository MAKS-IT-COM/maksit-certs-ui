'use client'

import { FormEvent, useCallback, useState } from 'react'
import {
  useValidation,
  isValidEmail,
  isValidHostname,
  isBypass
} from '@/hooks/useValidation'
import {
  CustomButton,
  CustomCheckbox,
  CustomEnumSelect,
  CustomInput,
  CustomRadioGroup
} from '@/controls'
import { CacheAccount } from '@/entities/CacheAccount'
import { FaPlus, FaTrash } from 'react-icons/fa'
import { ChallengeTypes } from '@/entities/ChallengeTypes'

interface AccountEditProps {
  account: CacheAccount
  onCancel?: () => void
  onSave?: (account: CacheAccount) => void
  onDelete?: (accountId: string) => void
}

const AccountEdit: React.FC<AccountEditProps> = (props) => {
  const { account, onCancel, onSave, onDelete } = props

  const [editingAccount, setEditingAccount] = useState<CacheAccount>(account)
  const [newContact, setNewContact] = useState('')
  const [newHostname, setNewHostname] = useState('')

  const setDescription = useCallback(
    (newDescription: string) => {
      if (editingAccount) {
        setEditingAccount({ ...editingAccount, description: newDescription })
      }
    },
    [editingAccount]
  )

  const {
    value: description,
    error: descriptionError,
    handleChange: handleDescriptionChange,
    reset: resetDescription
  } = useValidation<string>({
    defaultValue: '',
    externalValue: account.description,
    setExternalValue: setDescription,
    validateFn: isBypass,
    errorMessage: ''
  })

  const {
    value: contact,
    error: contactError,
    handleChange: handleContactChange,
    reset: resetContact
  } = useValidation<string>({
    defaultValue: '',
    externalValue: newContact,
    setExternalValue: setNewContact,
    validateFn: isValidEmail,
    errorMessage: 'Invalid email format.'
  })

  const {
    value: hostname,
    error: hostnameError,
    handleChange: handleHostnameChange,
    reset: resetHostname
  } = useValidation<string>({
    defaultValue: '',
    externalValue: newHostname,
    setExternalValue: setNewHostname,
    validateFn: isValidHostname,
    errorMessage: 'Invalid hostname format.'
  })

  const handleIsDisabledChange = (value: boolean) => {
    // setAccount({ ...account, isDisabled: value })
  }

  const handleChallengeTypeChange = (option: any) => {
    //setAccount({ ...account, challengeType: option.value })
  }

  const handleHostnameDisabledChange = (hostname: string, value: boolean) => {
    //   setAccount({
    //     ...account,
    //     hostnames: account.hostnames.map((h) =>
    //       h.hostname === hostname ? { ...h, isDisabled: value } : h
    //     )
    //   })
    // }
    // const handleStagingChange = (value: string) => {
    //   setAccount({ ...account, isStaging: value === 'staging' })
  }

  const deleteContact = (contact: string) => {
    if (account?.contacts.length ?? 0 < 1) return

    //   setAccount({
    //     ...account,
    //     contacts: account.contacts.filter((c) => c !== contact)
    //   })
    // }

    // const addContact = () => {
    //   if (newContact === '' || contactError) {
    //     return
    //   }

    //   if (account.contacts.includes(newContact)) return

    //   setAccount({ ...account, contacts: [...account.contacts, newContact] })
    //   resetContact()
  }

  const deleteHostname = (hostname: string) => {
    //if (account?.hostnames.length ?? 0 < 1) return

    //   setAccount({
    //     ...account,
    //     hostnames: account.hostnames.filter((h) => h.hostname !== hostname)
    //   })
    // }

    // const addHostname = () => {
    //   if (newHostname === '' || hostnameError) {
    //     return
    //   }

    //   if (account.hostnames.some((h) => h.hostname === newHostname)) return

    //   setAccount({
    //     ...account,
    //     hostnames: [
    //       ...account.hostnames,
    //       {
    //         hostname: newHostname,
    //         expires: new Date(),
    //         isUpcomingExpire: false,
    //         isDisabled: false
    //       }
    //     ]
    //   })
    resetHostname()
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    // const contactChanges = {
    //   added: account.contacts.filter(
    //     (contact) => !initialAccountState.contacts.includes(contact)
    //   ),
    //   removed: initialAccountState.contacts.filter(
    //     (contact) => !account.contacts.includes(contact)
    //   )
    // }

    // const hostnameChanges = {
    //   added: account.hostnames.filter(
    //     (hostname) =>
    //       !initialAccountState.hostnames.some(
    //         (h) => h.hostname === hostname.hostname
    //       )
    //   ),
    //   removed: initialAccountState.hostnames.filter(
    //     (hostname) =>
    //       !account.hostnames.some((h) => h.hostname === hostname.hostname)
    //   )
    // }

    // // Handle contact changes
    // if (contactChanges.added.length > 0) {
    //   // TODO: POST new contacts
    //   console.log('Added contacts:', contactChanges.added)
    // }
    // if (contactChanges.removed.length > 0) {
    //   // TODO: DELETE removed contacts
    //   console.log('Removed contacts:', contactChanges.removed)
    // }

    // // Handle hostname changes
    // if (hostnameChanges.added.length > 0) {
    //   // TODO: POST new hostnames
    //   console.log('Added hostnames:', hostnameChanges.added)
    // }
    // if (hostnameChanges.removed.length > 0) {
    //   // TODO: DELETE removed hostnames
    //   console.log('Removed hostnames:', hostnameChanges.removed)
    // }

    // onSave(account)
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="mb-4">
        <CustomInput
          value={description}
          onChange={handleDescriptionChange}
          placeholder="Add new description"
          type="text"
          error={descriptionError}
          title="Description"
          inputClassName="border p-2 rounded w-full"
          errorClassName="text-red-500 text-sm mt-1"
          className="mr-2 flex-grow"
        />
      </div>

      <div className="mb-4">
        <CustomCheckbox
          checked={account.isDisabled}
          label="Disabled"
          onChange={(value) => handleIsDisabledChange(value)}
          className="mr-2 flex-grow"
        />
      </div>

      <div className="mb-4">
        <CustomEnumSelect
          title="Challenge Type"
          enumType={ChallengeTypes}
          selectedValue={account.challengeType}
          onChange={(option) => handleChallengeTypeChange(option)}
          className="mr-2 flex-grow"
        />
      </div>

      <div className="mb-4">
        <h3 className="text-xl font-medium mb-2">Contacts:</h3>
        <ul className="list-disc list-inside pl-4 mb-2">
          {account.contacts.map((contact) => (
            <li key={contact} className="text-gray-700 mb-2 inline-flex">
              {contact}
              <CustomButton
                type="button"
                onClick={() => deleteContact(contact)}
                className="bg-red-500 text-white p-2 rounded ml-2"
              >
                <FaTrash />
              </CustomButton>
            </li>
          ))}
        </ul>
        <div className="flex items-center mb-4">
          <CustomInput
            value={newContact}
            onChange={handleContactChange}
            placeholder="Add new contact"
            type="email"
            error={contactError}
            title="New Contact"
            inputClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 flex-grow"
          />
          <CustomButton
            type="button"
            //onClick={addContact}
            className="bg-green-500 text-white p-2 rounded ml-2"
          >
            <FaPlus />
          </CustomButton>
        </div>
      </div>
      <div>
        <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
        <ul className="list-disc list-inside pl-4 mb-2">
          {account.hostnames?.map((hostname) => (
            <li key={hostname.hostname} className="text-gray-700 mb-2">
              <div className="inline-flex">
                {hostname.hostname} - {hostname.expires.toDateString()} -{' '}
                <span
                  className={`ml-2 px-2 py-1 rounded ${
                    hostname.isUpcomingExpire
                      ? 'bg-yellow-200 text-yellow-800'
                      : 'bg-green-200 text-green-800'
                  }`}
                >
                  {hostname.isUpcomingExpire ? 'Upcoming' : 'Not Upcoming'}
                </span>{' '}
                -{' '}
                <CustomCheckbox
                  className="ml-2"
                  checked={hostname.isDisabled}
                  label="Disabled"
                  onChange={(value) =>
                    handleHostnameDisabledChange(hostname.hostname, value)
                  }
                />
              </div>

              <CustomButton
                type="button"
                onClick={() => deleteHostname(hostname.hostname)}
                className="bg-red-500 text-white p-2 rounded ml-2"
              >
                <FaTrash />
              </CustomButton>
            </li>
          ))}
        </ul>
        <div className="flex items-center">
          <CustomInput
            value={newHostname}
            onChange={handleHostnameChange}
            placeholder="Add new hostname"
            type="text"
            error={hostnameError}
            title="New Hostname"
            inputClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 flex-grow"
          />
          <CustomButton
            type="button"
            //onClick={addHostname}
            className="bg-green-500 text-white p-2 rounded ml-2"
          >
            <FaPlus />
          </CustomButton>
        </div>
      </div>
      <div className="mb-4">
        <CustomRadioGroup
          options={[
            { value: 'staging', label: 'Staging' },
            { value: 'production', label: 'Production' }
          ]}
          initialValue={account.isStaging ? 'staging' : 'production'}
          title="LetsEncrypt Environment"
          className="mr-2 flex-grow"
          radioClassName=""
          errorClassName="text-red-500 text-sm mt-1"
          disabled={true}
        />
      </div>
      <div className="flex justify-between mt-4">
        <CustomButton
          //onClick={() => onDelete(account.accountId)}
          className="bg-red-500 text-white p-2 rounded ml-2"
        >
          <FaTrash />
        </CustomButton>
        <CustomButton
          onClick={onCancel}
          className="bg-yellow-500 text-white p-2 rounded ml-2"
        >
          Cancel
        </CustomButton>
        <CustomButton
          type="submit"
          className="bg-green-500 text-white p-2 rounded ml-2"
        >
          Save
        </CustomButton>
      </div>
    </form>
  )
}

export { AccountEdit }

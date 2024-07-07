'use client'

import { Dispatch, FormEvent, SetStateAction, useEffect, useState } from 'react'
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
import { CacheAccount, toPatchAccountRequest } from '@/entities/CacheAccount'
import { FaPlus, FaTrash } from 'react-icons/fa'
import { ChallengeTypes } from '@/entities/ChallengeTypes'
import { deepCopy } from '@/functions'
import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { httpService } from '@/services/HttpService'
import { PatchAccountRequest } from '@/models/letsEncryptServer/account/requests/PatchAccountRequest'
import { PatchOperation } from '@/models/PatchOperation'
import { useAppDispatch } from '@/redux/store'
import { showToast } from '@/redux/slices/toastSlice'
import {
  GetAccountResponse,
  toCacheAccount
} from '@/models/letsEncryptServer/account/responses/GetAccountResponse'

interface AccountEditProps {
  account: CacheAccount
  onCancel?: () => void
  onDelete: (accountId: string) => void
  onSubmit: (account: CacheAccount) => void
}

const AccountEdit: React.FC<AccountEditProps> = (props) => {
  const { account, onCancel, onDelete, onSubmit } = props

  const dispatch = useAppDispatch()

  const [newAccount, setNewAccount] = useState<PatchAccountRequest>(
    toPatchAccountRequest(account)
  )

  const [newContact, setNewContact] = useState('')
  const [newHostname, setNewHostname] = useState('')

  const {
    value: description,
    error: descriptionError,
    handleChange: handleDescriptionChange
  } = useValidation<string>({
    defaultValue: '',
    externalValue: newAccount.description?.value ?? '',
    setExternalValue: (newDescription) => {
      setNewAccount((prev) => {
        const newAccount = deepCopy(prev)
        newAccount.description = {
          op:
            newDescription !== account.description
              ? PatchOperation.Replace
              : PatchOperation.None,
          value: newDescription
        }
        return newAccount
      })
    },
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
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.isDisabled = {
        op:
          value !== account.isDisabled
            ? PatchOperation.Replace
            : PatchOperation.None,
        value
      }
      return newAccount
    })
  }

  const handleAddContact = () => {
    if (newContact === '' || contactError) return

    // Check if the contact already exists in the account
    const contactExists = newAccount.contacts?.some(
      (contact) => contact.value === newContact
    )

    if (contactExists) {
      // Optionally, handle the duplicate contact case, e.g., show an error message
      dispatch(
        showToast({ message: 'Contact already exists.', type: 'warning' })
      )
      resetContact()
      return
    }

    // If the contact does not exist, add it
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.contacts?.push({ op: PatchOperation.Add, value: newContact })
      return newAccount
    })

    resetContact()
  }

  const handleDeleteContact = (contact: string) => {
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.contacts = newAccount.contacts
        ?.map((c) => {
          if (c.value === contact && c.op !== PatchOperation.Add)
            c.op = PatchOperation.Remove
          return c
        })
        .filter((c) => !(c.value === contact && c.op === PatchOperation.Add))

      console.log(newAccount.contacts)

      return newAccount
    })
  }

  const handleAddHostname = () => {
    if (newHostname === '' || hostnameError) return

    // Check if the hostname already exists in the account
    const hostnameExists = newAccount.hostnames?.some(
      (hostname) => hostname.hostname?.value === newHostname
    )

    if (hostnameExists) {
      // Optionally, handle the duplicate hostname case, e.g., show an error message
      dispatch(
        showToast({ message: 'Hostname already exists.', type: 'warning' })
      )
      resetHostname()
      return
    }

    // If the hostname does not exist, add it
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.hostnames?.push({
        hostname: { op: PatchOperation.Add, value: newHostname },
        isDisabled: { op: PatchOperation.Add, value: false }
      })
      return newAccount
    })

    resetHostname()
  }

  const handleHostnameDisabledChange = (hostname: string, value: boolean) => {
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      const targetHostname = newAccount.hostnames?.find(
        (h) => h.hostname?.value === hostname
      )
      if (targetHostname) {
        targetHostname.isDisabled = {
          op:
            value !== targetHostname.isDisabled?.value
              ? PatchOperation.Replace
              : PatchOperation.None,
          value
        }
      }
      return newAccount
    })
  }

  const handleDeleteHostname = (hostname: string) => {
    setNewAccount((prev) => {
      const newAccount = deepCopy(prev)
      newAccount.hostnames = newAccount.hostnames
        ?.map((h) => {
          if (
            h.hostname?.value === hostname &&
            h.hostname?.op !== PatchOperation.Add
          )
            h.hostname.op = PatchOperation.Remove
          return h
        })
        .filter(
          (h) =>
            !(
              h.hostname?.value === hostname &&
              h.hostname?.op === PatchOperation.Add
            )
        )
      return newAccount
    })
  }

  const handleCancel = () => {
    onCancel?.()
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    if (!newAccount) return

    httpService
      .patch<
        PatchAccountRequest,
        GetAccountResponse
      >(GetApiRoute(ApiRoutes.ACCOUNT_ID, account.accountId), newAccount)
      .then((response) => {
        if (response.isSuccess && response.data) {
          onSubmit?.(toCacheAccount(response.data))
        } else {
          // Optionally, handle the error case, e.g., show an error message
          dispatch(
            showToast({ message: 'Failed to update account.', type: 'error' })
          )
        }
      })
  }

  const handleDelete = (accountId: string) => {
    httpService
      .delete(GetApiRoute(ApiRoutes.ACCOUNT_ID, accountId))
      .then((response) => {
        if (response.isSuccess) {
          onDelete?.(accountId)
        } else {
          // Optionally, handle the error case, e.g., show an error message
          dispatch(
            showToast({ message: 'Failed to detele account.', type: 'error' })
          )
        }
      })
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
          className="mr-2 w-full"
        />
      </div>

      <div className="mb-4">
        <CustomCheckbox
          checked={newAccount.isDisabled?.value ?? false}
          label="Disabled"
          onChange={handleIsDisabledChange}
          className="mr-2"
        />
      </div>

      <div className="mb-4">
        <h3 className="text-xl font-medium mb-2">Contacts:</h3>
        <ul className="list-disc list-inside pl-4 mb-2">
          {newAccount.contacts
            ?.filter((contact) => contact.op !== PatchOperation.Remove)
            .map((contact) => (
              <li key={contact.value} className="text-gray-700 mb-2">
                <div className="inline-flex">
                  {contact.value}
                  <CustomButton
                    type="button"
                    onClick={() => handleDeleteContact(contact.value ?? '')}
                    className="bg-red-500 text-white p-2 rounded ml-2"
                  >
                    <FaTrash />
                  </CustomButton>
                </div>
              </li>
            ))}
        </ul>
        <div className="flex items-center mb-4">
          <CustomInput
            value={contact}
            onChange={handleContactChange}
            placeholder="Add new contact"
            type="email"
            error={contactError}
            title="New Contact"
            inputClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 w-full"
          />
          <CustomButton
            type="button"
            onClick={handleAddContact}
            className="bg-green-500 text-white p-2 rounded ml-2"
          >
            <FaPlus />
          </CustomButton>
        </div>
      </div>

      <div className="mb-4">
        <CustomEnumSelect
          title="Challenge Type"
          enumType={ChallengeTypes}
          selectedValue={account.challengeType}
          className="mr-2 w-full"
          disabled={true}
        />
      </div>

      <div>
        <h3 className="text-xl font-medium mb-2">Hostnames:</h3>
        <ul className="list-disc list-inside pl-4 mb-2">
          {newAccount.hostnames
            ?.filter(
              (hostname) => hostname.hostname?.op !== PatchOperation.Remove
            )
            .map((hostname) => (
              <li key={hostname.hostname?.value} className="text-gray-700 mb-2">
                <div className="inline-flex items-center">
                  {hostname.hostname?.value} -{' '}
                  <CustomCheckbox
                    className="ml-2"
                    checked={hostname.isDisabled?.value ?? false}
                    label="Disabled"
                    onChange={(value) =>
                      handleHostnameDisabledChange(
                        hostname.hostname?.value ?? '',
                        value
                      )
                    }
                  />
                </div>

                <CustomButton
                  type="button"
                  onClick={() =>
                    handleDeleteHostname(hostname.hostname?.value ?? '')
                  }
                  className="bg-red-500 text-white p-2 rounded ml-2"
                >
                  <FaTrash />
                </CustomButton>
              </li>
            ))}
        </ul>
        <div className="flex items-center">
          <CustomInput
            value={hostname}
            onChange={handleHostnameChange}
            placeholder="Add new hostname"
            type="text"
            error={hostnameError}
            title="New Hostname"
            inputClassName="border p-2 rounded w-full"
            errorClassName="text-red-500 text-sm mt-1"
            className="mr-2 w-full"
          />
          <CustomButton
            type="button"
            onClick={handleAddHostname}
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
          className="mr-2 w-full"
          radioClassName=""
          errorClassName="text-red-500 text-sm mt-1"
          disabled={true}
        />
      </div>
      <div className="flex justify-between mt-4">
        <CustomButton
          type="button"
          onClick={() => handleDelete(account.accountId)}
          className="bg-red-500 text-white p-2 rounded"
        >
          <FaTrash />
        </CustomButton>
        <CustomButton
          type="button"
          onClick={handleCancel}
          className="bg-yellow-500 text-white p-2 rounded"
        >
          Cancel
        </CustomButton>
        <CustomButton
          type="submit"
          className="bg-green-500 text-white p-2 rounded"
        >
          Save
        </CustomButton>
      </div>
    </form>
  )
}

export { AccountEdit }

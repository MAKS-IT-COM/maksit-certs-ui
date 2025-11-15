import { FC, useCallback, useEffect, useState } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { ButtonComponent, CheckBoxComponent, TextBoxComponent } from '../components/editors'
import { GetAccountResponse } from '../models/letsEncryptServer/account/responses/GetAccountResponse'
import { useFormState } from '../hooks/useFormState'
import { array, boolean, object, Schema, string } from 'zod'
import { PlusIcon, TrashIcon } from 'lucide-react'
import { getData, patchData } from '../axiosConfig'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { FieldContainer } from '../components/editors/FieldContainer'
import { deepCopy, deepDelta, deltaHasOperations } from '../functions'
import { PatchAccountRequest, PatchAccountRequestSchema } from '../models/letsEncryptServer/account/requests/PatchAccountRequest'
import { addToast } from '../components/Toast/addToast'


interface EditAccountHostnameFormProps {
  isDisabled: boolean
  hostname: string
}

const EditAccountHostnameFormProto = (): EditAccountHostnameFormProps => ({
  isDisabled: false,
  hostname: ''
})

const EditAccountHostnameFormSchema: Schema<EditAccountHostnameFormProps> = object({
  isDisabled: boolean(),
  hostname: string()
})

interface EditAccountFormProps {
  isDisabled: boolean
  description: string

  contact: string
  contacts: string[]
  
  hostname: string,
  hostnames: EditAccountHostnameFormProps[]
}

const RegisterFormProto = (): EditAccountFormProps => ({
  isDisabled: false,
  description: '',

  contact: '',
  contacts: [],

  hostname: '',
  hostnames: [],
})

const RegisterFormSchema: Schema<EditAccountFormProps> = object({
  isDisabled: boolean(),
  description: string(),
  
  contact: string(),
  contacts: array(string()),

  hostname: string(),
  hostnames: array(EditAccountHostnameFormSchema)
})

interface EditAccountProps {
    accountId: string
    onSubmitted?: (entity: GetAccountResponse) => void
    cancelEnabled?: boolean
    onCancel?: () => void
}

const EditAccount: FC<EditAccountProps> = (props) => {
  const {
    accountId,
    onSubmitted,
    cancelEnabled,
    onCancel
  } = props


  const {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState
  } = useFormState<EditAccountFormProps>({
    initialState: RegisterFormProto(),
    validationSchema: RegisterFormSchema
  })

  const [backupState, setBackupState] = useState<EditAccountFormProps>(RegisterFormProto())

  const handleInitialization = useCallback((response: GetAccountResponse) => {
    const newState = {
      ...RegisterFormProto(),
      isDisabled: response.isDisabled,
      description: response.description,
      contacts: [...response.contacts],
      hostnames: (response.hostnames ?? []).map(h => ({
        ...EditAccountHostnameFormProto(),
        isDisabled: h.isDisabled,
        hostname: h.hostname
      }))
    }
    
    setInitialState(newState)
    setBackupState(deepCopy(newState))
  }, [setInitialState])

  useEffect(() => {
    getData<GetAccountResponse>(GetApiRoute(ApiRoutes.ACCOUNT_GET).route
      .replace('{accountId}', accountId)
    ).then((response) => {
      if (!response) return

      handleInitialization(response)
    })
  }, [accountId, handleInitialization])


  const mapFormStateToPatchRequest = (formState: EditAccountFormProps) : PatchAccountRequest => {
    const formStateCopy = deepCopy(formState)

    const patchRequest: PatchAccountRequest = {
      isDisabled: formStateCopy.isDisabled,
      description: formStateCopy.description,
      contacts: [...formStateCopy.contacts],
      hostnames: formStateCopy.hostnames.map(h => ({
        hostname: h.hostname,
        isDisabled: h.isDisabled
      }))
    }

    return patchRequest
  }

  const handleSubmit = async () => {
    if (!formIsValid) return

    const fromFormState = mapFormStateToPatchRequest(formState)
    const fromBackupState = mapFormStateToPatchRequest(backupState)

    const delta = deepDelta(fromFormState, fromBackupState, {
      arrays: {
        hostnames: {
          identityKey: 'hostname',
          idFieldKey: 'hostname'
        }
      }
    })

    if (!deltaHasOperations(delta)) {
      addToast('No changes detected', 'info')
      return
    }

    const request = PatchAccountRequestSchema.safeParse(delta)

    if (!request.success) {
      request.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })

      return
    }

    const response = await patchData<PatchAccountRequest, GetAccountResponse>(GetApiRoute(ApiRoutes.ACCOUNT_PATCH).route
      .replace('{accountId}', accountId), delta, 120000
    )

    if (!response) return

    handleInitialization(response)
    onSubmitted?.(response)

  }

  const handleCancel = () => {
    onCancel?.()
  }

  return <FormContainer>
    <FormHeader>Edit Account {accountId}</FormHeader>
    <FormContent>
      <div className={'grid grid-cols-12 gap-4 w-full'}>
        <TextBoxComponent
          colspan={12}
          label={'Account Description'}
          value={formState.description}
          onChange={(e) => handleInputChange('description', e.target.value)}
          placeholder={'Account Description'}
          errorText={errors.description}
        />
        <CheckBoxComponent
          colspan={12}
          label={'Disabled'}
          value={formState.isDisabled}
          onChange={(e) => handleInputChange('isDisabled', e.target.checked)}
          errorText={errors.isDisabled}
        />

        <h3 className={'col-span-12'}>Contacts:</h3>
        <ul className={'col-span-12'}>
          {formState.contacts
            .map((contact) => (
              <li key={contact} className={'grid grid-cols-12 gap-4 w-full pb-2'}> 
                <div className={'col-span-10'}>
                  {contact}
                </div>
                <ButtonComponent
                  colspan={2}
                  onClick={() => {
                    const updatedContacts = formState.contacts.filter(c => c !== contact)
                    handleInputChange('contacts', updatedContacts)
                  }}
             
                >
                  <TrashIcon />
                </ButtonComponent>
              </li>
            ))}
        </ul>
        <TextBoxComponent
          colspan={10}
          label={'New Contact'}
          value={formState.contact}
          onChange={(e) => {
            handleInputChange('contact', e.target.value)
          }}
          placeholder={'Add contact'}
          type={'text'}
          errorText={errors.contact}
        />
        <FieldContainer colspan={2}>
          <ButtonComponent
            onClick={() => {
              if (formState.contacts.includes(formState.contact))
                return
            
              handleInputChange('contacts', [...formState.contacts, formState.contact])
              handleInputChange('contact', '')
            }}
            disabled={formState.contact.trim() === ''}
          >
            <PlusIcon />
          </ButtonComponent>
        </FieldContainer>
        <h3 className={'col-span-12'}>Hostnames:</h3>
        <ul className={'col-span-12'}>
          {formState.hostnames.map((hostname) => (
            <li key={hostname.hostname} className={'grid grid-cols-12 gap-4 w-full pb-2'}>
              <span className={'col-span-7'}>{hostname.hostname}</span>
              <span className={'col-span-3'}>
                <label className={'mr-2'}>Disabled:</label>
                <input
                  type={'checkbox'}
                  checked={hostname.isDisabled}
                  onChange={(e) => {
                    const updatedHostnames = formState.hostnames.map(h => {
                      if (h.hostname === hostname.hostname) {
                        return {  
                          ...h,
                          isDisabled: e.target.checked
                        }
                      }
                      return h
                    })
                    handleInputChange('hostnames', updatedHostnames)
                  }}
                />
              </span>
              <ButtonComponent
                colspan={2}
                onClick={() => {
                  const updatedHostnames = formState.hostnames.filter(h => h.hostname !== hostname.hostname)
                  handleInputChange('hostnames', updatedHostnames)
                }}
              >
                <TrashIcon />
              </ButtonComponent>
              
            </li>
          ))}
        </ul>
        <TextBoxComponent
          colspan={10}
          label={'New Hostname'}
          value={formState.hostname}
          onChange={(e) => {
            handleInputChange('hostname', e.target.value)
          }}
          placeholder={'Add hostname'}
          type={'text'}
          errorText={errors.hostname}
        />
        <FieldContainer colspan={2}>
          <ButtonComponent
            onClick={() => {
              if (formState.hostnames.find(h => h.hostname === formState.hostname))
                return
            
              handleInputChange('hostnames', [ ...formState.hostnames, {
                ...EditAccountHostnameFormProto(),
                hostname: formState.hostname
              }
              ])

              handleInputChange('hostname', '')
            }}
            disabled={formState.hostname.trim() === ''}
          >
            <PlusIcon />
          </ButtonComponent>
        </FieldContainer>
      </div>
    </FormContent>
    <FormFooter
    
      rightChildren={
        <ButtonComponent label={'Save'} buttonHierarchy={'primary'} onClick={handleSubmit} />
      }
      leftChildren={
        cancelEnabled && <ButtonComponent label={'Cancel'} buttonHierarchy={'secondary'} onClick={handleCancel} />
      }
    />
  </FormContainer>
}

export {
  EditAccount
}
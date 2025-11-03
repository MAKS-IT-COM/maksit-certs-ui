import { FC } from 'react'
import { FormContainer, FormContent, FormFooter, FormHeader } from '../components/FormLayout'
import { postData } from '../axiosConfig'
import { GetAccountResponse } from '../models/letsEncryptServer/account/responses/GetAccountResponse'
import { ApiRoutes, GetApiRoute } from '../AppMap'
import { ButtonComponent, RadioGroupComponent, SelectBoxComponent, TextBoxComponent } from '../components/editors'
import { ChallengeType } from '../entities/ChallengeType'
import z, { array, boolean, object, Schema, string } from 'zod'
import { useFormState } from '../hooks/useFormState'
import { enumToArr } from '../functions'
import { PostAccountRequest, PostAccountRequestSchema } from '../models/letsEncryptServer/account/requests/PostAccountRequest'
import { addToast } from '../components/Toast/addToast'
import { useNavigate } from 'react-router-dom'
import { PlusIcon, TrashIcon } from 'lucide-react'
import { FieldContainer } from '../components/editors/FieldContainer'


interface RegisterFormProps {
  description: string
  
  contact: string
  contacts: string[]

  hostname: string
  hostnames: string[]

  challengeType: ChallengeType
  isStaging: boolean
}

const RegisterFormProto = (): RegisterFormProps => ({
  description: '',

  contact: '',
  contacts: [],

  hostname: '',
  hostnames: [],

  challengeType: ChallengeType.http01,
  isStaging: true
})

const RegisterFormSchema: Schema<RegisterFormProps> = object({
  description: string(),
  
  contact: string(),
  contacts: array(string()),

  hostname: string(),
  hostnames: array(string()),

  challengeType: z.enum(ChallengeType),
  isStaging: boolean()
})

interface RegisterProps {
}

const Register: FC<RegisterProps> = () => {

  const navigate = useNavigate()

  const {
    formState,
    errors,
    formIsValid,
    handleInputChange
  } = useFormState<RegisterFormProps>({
    initialState: RegisterFormProto(),
    validationSchema: RegisterFormSchema,
  })

  const handleSubmit = () => {
    if (!formIsValid) return

    const requestData: PostAccountRequest = {
      description: formState.description,
      contacts: formState.contacts,
      hostnames: formState.hostnames,
      challengeType: formState.challengeType,
      isStaging: formState.isStaging,
    }

    const request = PostAccountRequestSchema.safeParse(requestData)
    
    if (!request.success) {
      request.error.issues.forEach(error => {
        addToast(error.message, 'error')
      })
      
      return
    }

    postData<PostAccountRequest, GetAccountResponse>(GetApiRoute(ApiRoutes.ACCOUNT_POST).route, request.data, 120000)
      .then(response => {
        if (!response) return

        navigate('/')
      })
  }

  return <FormContainer>
    <FormHeader>Register</FormHeader>
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
        <h3 className={'col-span-12'}>Contacts:</h3>
        <ul className={'col-span-12'}>
          {formState.contacts.map((contact) => (
            <li key={contact} className={'grid grid-cols-12 gap-4 w-full pb-2'}> 
              <span className={'col-span-10'}>{contact}</span>
              <FieldContainer colspan={2}>
                <ButtonComponent
                  onClick={() => {
                    const updatedContacts = formState.contacts.filter(c => c !== contact)
                    handleInputChange('contacts', updatedContacts)
                  }}
                >
                  <TrashIcon />
                </ButtonComponent>
              </FieldContainer>
            </li>
          ))}
        </ul>
        <TextBoxComponent
          colspan={10}
          label={'New Contact'}
          value={formState.contact}
          onChange={(e) => {
            if (formState.contacts.includes(e.target.value))
              return

            handleInputChange('contact', e.target.value)
          }}
          placeholder={'Add contact'}
          type={'text'}
          errorText={errors.contact}
        />
        <FieldContainer colspan={2}>
          <ButtonComponent
            onClick={() => {
              handleInputChange('contacts', [...formState.contacts, formState.contact])
              handleInputChange('contact', '')
            }}
            disabled={formState.contact.trim() === ''}
          >
            <PlusIcon />
          </ButtonComponent>
        </FieldContainer>
        <div className={'col-span-12'}>
          <SelectBoxComponent
            label={'Challenge Type'}
            options={enumToArr(ChallengeType)
              .map(ct => ({ value: ct.value, label: ct.displayValue }))
              .filter(ct => ct.value !== ChallengeType.dns01)}
            value={formState.challengeType}
            placeholder={'Select Challenge Type'}
            onChange={(e) => handleInputChange('challengeType', e.target.value)}
            errorText={errors.challengeType}
          />
        </div>
        <h3 className={'col-span-12'}>Hostnames:</h3>
        <ul className={'col-span-12'}>
          {formState.hostnames.map((hostname) => (
            <li key={hostname} className={'grid grid-cols-12 gap-4 w-full'}>
              <span className={'col-span-10'}>{hostname}</span>
              <FieldContainer colspan={2}>
                <ButtonComponent
                  onClick={() => {
                    const updatedHostnames = formState.hostnames.filter(h => h !== hostname)
                    handleInputChange('hostnames', updatedHostnames)
                  }}
                >
                  <TrashIcon />
                </ButtonComponent>
              </FieldContainer>
            </li>
          ))}
        </ul>
        <TextBoxComponent
          colspan={10}
          label={'New Hostname'}
          value={formState.hostname}
          onChange={(e) => {
            if (formState.hostnames.includes(e.target.value))
              return

            handleInputChange('hostname', e.target.value)
          }}
          placeholder={'Add hostname'}
          type={'text'}
          errorText={errors.hostname}
        />
        <FieldContainer colspan={2}>
          <ButtonComponent
            onClick={() => {
              handleInputChange('hostnames', [...formState.hostnames, formState.hostname])
              handleInputChange('hostname', '')
            }}
            disabled={formState.hostname.trim() === ''}
          >
            <PlusIcon />
          </ButtonComponent>
        </FieldContainer>
        <RadioGroupComponent
          colspan={12}
          label={'LetsEncrypt Environment'}
          options={[
            { value: 'staging', label: 'Staging' },
            { value: 'production', label: 'Production' }
          ]}

          value={formState.isStaging ? 'staging' : 'production'}
          onChange={(value) => {
            handleInputChange('isStaging', value === 'staging')
          }}
          errorText={errors.isStaging}
        />
      </div>
    </FormContent>
    <FormFooter rightChildren={
      <ButtonComponent
        label={'Create Account'}
        onClick={handleSubmit}
      />
    } />
  </FormContainer>
}


export { Register }
import { FC } from 'react'
import { ButtonComponent } from '../../../components/editors'
import { Offcanvas } from '../../../components/Offcanvas'
import { QRCodeSVG } from 'qrcode.react'
import { FormContainer, FormContent, FormHeader } from '../../../components/FormLayout'


interface EnableTwoFactorProps {
    isOpen?: boolean;
    qrCodeUrl?: string,
    twoFactorRecoveryCodes?: string []
    onClose?: () => void;
}

const EnableTwoFactor: FC<EnableTwoFactorProps> = (props) => {

  const {
    isOpen = false,
    qrCodeUrl,
    twoFactorRecoveryCodes,
    onClose
  } = props

  const handleOnClose = () => {
    onClose?.()
  }

  if (!qrCodeUrl || !twoFactorRecoveryCodes) {
    return null
  }

  return <Offcanvas isOpen={isOpen}>
    <FormContainer>
      <FormHeader>Enable 2FA</FormHeader>
      <FormContent>
        <div className={'grid grid-cols-12 gap-4 w-full h-full content-start'}>
          <p className={'col-span-12'}>Please scan the QR code with your authenticator app.</p>
          <div className={'col-span-12 flex justify-center items-center'}>
            <QRCodeSVG
              value={qrCodeUrl}
              size={256}
              bgColor={'#ffffff'}
              fgColor={'#000000'}
              level={'H'}
            />
          </div>
          <p className={'col-span-12'}><span className={'font-bold'}>Important!</span> Note these recovery codes to use in case you lose access to your Authenticator app. Keep them in a safe place!</p>
          <div className={'col-span-12 flex justify-center items-center'}>
            <ul>
              {twoFactorRecoveryCodes.map((code) => {
                return <li key={code}>{code}</li>
              })}
            </ul>
          </div>
          <ButtonComponent
            colspan={12}
            label={'Back'}
            buttonHierarchy={'secondary'}
            onClick={handleOnClose}
          />
        </div>
      </FormContent>
    </FormContainer>
  </Offcanvas>
}

export {
  EnableTwoFactor
}

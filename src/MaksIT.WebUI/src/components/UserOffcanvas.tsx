import { ButtonComponent, FormContainer, FormContent, FormHeader, Offcanvas } from '@maks-it.com/webui-components'
import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { logout, setHideUserOffcanvas } from '../redux/slices/identitySlice'

const UserOffcanvas = () => {
  const dispatch = useAppDispatch()
  const { identity, showUserOffcanvas } = useAppSelector((s) => s.identity)

  return (
    <Offcanvas isOpen={showUserOffcanvas} colspan={4}>
      <FormContainer>
        <FormHeader>{identity?.username}</FormHeader>
        <FormContent>
          <div className={'flex flex-col justify-between h-full'}>
            <div className={'flex flex-col items-center gap-3'}>
              <ButtonComponent
                label={'Edit User'}
                route={`/user/${identity?.userId}`}
                onClick={() => dispatch(setHideUserOffcanvas())}
              />
            </div>
            <div className={'flex flex-col items-center gap-3'}>
              <ButtonComponent
                label={'Back'}
                buttonHierarchy={'secondary'}
                onClick={() => dispatch(setHideUserOffcanvas())}
              />
              <ButtonComponent
                label={'Logout'}
                buttonHierarchy={'error'}
                onClick={() => void dispatch(logout(false))}
              />
            </div>
          </div>
        </FormContent>
      </FormContainer>
    </Offcanvas>
  )
}

export { UserOffcanvas }

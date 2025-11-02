import { useAppDispatch, useAppSelector } from '../redux/hooks'
import { setShowUserOffcanvas } from '../redux/slices/identitySlice'

const UserButton = () => {
  const dispatch = useAppDispatch()
  //const { identity } = useAppSelector(state => state.identity)

  const identity = {
    username: 'JohnDoe',
    isGlobalAdmin: true
  }

  if (!identity) return <></>

  return <button
    className={'bg-white text-blue-500 px-2 py-1 rounded'}
    onClick={() => {
      dispatch(setShowUserOffcanvas())
    }}>
    {`${identity.username} ${identity.isGlobalAdmin ? '(Global Admin)' : ''}`.trim()}
  </button>
}

export {
  UserButton
}
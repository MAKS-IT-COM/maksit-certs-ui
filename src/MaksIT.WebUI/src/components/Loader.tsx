import { useAppSelector } from '../redux/hooks'
import { RootState } from '../redux/store'


const Loader = () => {
  const loading = useAppSelector((state: RootState) => state.loader.loading)

  if (!loading) return null

  return (
    <div className={'fixed inset-0 flex items-center justify-center bg-gray-800/75 z-50'}>
      <div className={'ease-linear rounded-full border-8 border-t-8 border-gray-200 h-32 w-32 animate-spin'} style={{ borderTopColor: '#3498db' }}></div>
    </div>
  )
}

export { Loader }
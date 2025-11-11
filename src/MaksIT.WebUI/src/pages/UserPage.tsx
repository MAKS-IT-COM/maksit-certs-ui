import { useParams } from 'react-router-dom'
import { EditUser } from '../forms/Users/EditUser'


const UserPage = () => {
  const { userId } = useParams<{ userId: string }>()
  
  return userId ? <EditUser userId={userId} /> : <>User not found</>
}

export {
  UserPage
}
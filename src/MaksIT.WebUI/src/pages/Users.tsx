import { useParams } from 'react-router-dom'
import { EditUser } from '../forms/Users/EditUser'
import { SearchUser } from '../forms/Users/SearchUser'

const Users = () => {
  const { userId } = useParams<{ userId: string }>()

  return userId ? <EditUser userId={userId} /> : <SearchUser />
}

export {
  Users
}

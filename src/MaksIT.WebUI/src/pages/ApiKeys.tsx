import { useParams } from 'react-router-dom'
import { EditApiKey } from '../forms/ApiKeys/EditApiKey'
import { SearchApiKey } from '../forms/ApiKeys/SearchApiKey'

const ApiKeys = () => {
  const { apiKeyId } = useParams<{ apiKeyId: string }>()

  return apiKeyId ? <EditApiKey apiKeyId={apiKeyId} /> : <SearchApiKey />
}

export {
  ApiKeys
}

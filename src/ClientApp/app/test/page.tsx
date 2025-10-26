'use client'

import { ApiRoutes, GetApiRoute } from '@/ApiRoutes'
import { PageContainer } from '@/components/pageContainer'
import { CustomButton } from '@/controls'
import { showToast } from '@/redux/slices/toastSlice'
import { useAppDispatch } from '@/redux/store'
import { httpService } from '@/services/HttpService'

const TestPage = () => {
  const dispatch = useAppDispatch()

  const handleTestAgent = async () => {
    httpService
      .get<string>(GetApiRoute(ApiRoutes.AGENT_TEST))
      .then((response) => {
        dispatch(
          showToast({
            message: JSON.stringify(response),
            type: 'info'
          })
        )
      })
  }

  return (
    <PageContainer title="CertsUI Tests">
      <CustomButton
        type="button"
        onClick={handleTestAgent}
        className="bg-green-500 text-white p-2 rounded ml-2"
      >
        Test Agent
      </CustomButton>
    </PageContainer>
  )
}

export default TestPage

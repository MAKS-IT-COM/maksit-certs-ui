interface PageContainerProps {
  title?: string
  children: React.ReactNode
}

const PageContainer: React.FC<PageContainerProps> = (props) => {
  const { title, children } = props

  return (
    <div className="container mx-auto p-4">
      {title && (
        <h1 className="text-4xl font-bold text-center mb-8">{title}</h1>
      )}
      {children}
    </div>
  )
}

export { PageContainer }

import React from 'react'

interface FooterProps {
  className?: string
}

const Footer = (props: FooterProps) => {
  const { className } = props
  return (
    <footer className={`bg-gray-900 text-white text-center p-4 ${className}`}>
      <p>{`© ${new Date().getFullYear()} MAKS-IT`}</p>
    </footer>
  )
}

export { Footer }

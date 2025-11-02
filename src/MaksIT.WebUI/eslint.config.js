import js from '@eslint/js'
import globals from 'globals'
import react from 'eslint-plugin-react'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      parser: tseslint.parser,
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      react, // Import and use the React plugin
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      '@typescript-eslint': tseslint.plugin,
    },
    rules: {
      // react plugin rules
      'react/jsx-curly-brace-presence': [
        'error',
        { props: 'always', children: 'never' }
      ],

      ...reactHooks.configs.recommended.rules,
      
      // react-refresh plugin rules
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],

      // ESLint core rules
      'quotes': ['error', 'single'], // Enforce single quotes
      'semi': ['error', 'never'], // Disallow semicolons
      'indent': ['error', 2], // Enforce 2-space indentation
      'jsx-quotes': ['error', 'prefer-single'], // Enforce single quotes in JSX attributes

      // @typescript-eslint plugin rules
      '@typescript-eslint/no-unused-vars': ['warn', { 'argsIgnorePattern': '^_', 'ignoreRestSiblings': true }],
      '@typescript-eslint/no-empty-object-type': 'off',
    },
  },
)

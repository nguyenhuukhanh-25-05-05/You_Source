import js from '@eslint/js'
import vue from 'eslint-plugin-vue'
import eslintConfigPrettier from 'eslint-config-prettier'
import globals from 'globals'

export default [
  {
    ignores: ['dist/**', 'node_modules/**', '*.config.js']
  },
  js.configs.recommended,
  ...vue.configs['flat/recommended'],
  {
    files: ['**/*.{js,mjs,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
        ...globals.es2021,
        document: 'readonly',
        window: 'readonly',
        localStorage: 'readonly',
        navigator: 'readonly'
      }
    },
    rules: {
      'vue/multi-word-component-names': 'off',
      'no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
      'vue/max-attributes-per-line': 'off',
      'vue/singleline-element': 'off'
    }
  },
  eslintConfigPrettier
]

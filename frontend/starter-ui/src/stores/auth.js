import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../services/api'

export const useAuthStore = defineStore('auth', () => {
  const user = ref(parseUser())

  const isAuthenticated = computed(() => !!user.value)
  const roles = computed(() => user.value?.roles ?? [])
  const isAdmin = computed(() => roles.value.includes('Admin'))

  function hasRole(role) {
    return roles.value.includes(role)
  }

  function parseUser() {
    try {
      return JSON.parse(localStorage.getItem('user') || 'null')
    } catch {
      return null
    }
  }

  function setUser(info) {
    user.value = info
    if (info) localStorage.setItem('user', JSON.stringify(info))
    else localStorage.removeItem('user')
  }

  async function login(username, password) {
    const { data } = await api.post('/api/auth/login', { username, password })
    setUser(data.data)
  }

  async function register(username, email, password, fullName) {
    const { data } = await api.post('/api/auth/register', {
      username,
      email,
      password,
      fullName
    })
    setUser(data.data)
  }

  async function logout() {
    try {
      await api.post('/api/auth/revoke')
    } finally {
      clearAuth()
    }
  }

  async function refreshAccessToken() {
    const { data } = await api.post('/api/auth/refresh-token', {})
    setUser(data.data)
  }

  async function fetchMe() {
    const { data } = await api.get('/api/auth/me')
    setUser(data.data)
    return data.data
  }

  function clearAuth() {
    user.value = null
    localStorage.removeItem('user')
  }

  return {
    user,
    isAuthenticated,
    isAdmin,
    roles,
    hasRole,
    login,
    register,
    logout,
    refreshAccessToken,
    fetchMe,
    clearAuth,
    setUser
  }
})

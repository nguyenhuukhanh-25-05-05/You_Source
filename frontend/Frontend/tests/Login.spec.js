import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import Login from '../src/views/Login.vue'

const mockStore = {
  login: vi.fn()
}

vi.mock('../src/stores/auth', () => ({
  useAuthStore: () => mockStore
}))

function mountLogin() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'Dashboard', component: { template: '<div>dash</div>' } },
      { path: '/login', name: 'Login', component: Login }
    ]
  })
  return mount(Login, { global: { plugins: [router] } })
}

describe('Login.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockStore.login.mockReset()
    mockStore.login.mockResolvedValue(undefined)
  })

  it('disables button while loading', async () => {
    let resolveLogin
    mockStore.login.mockReturnValueOnce(new Promise((r) => (resolveLogin = r)))
    const wrapper = mountLogin()
    const username = wrapper.find('input[type="text"]')
    const password = wrapper.find('input[type="password"]')
    await username.setValue('alice')
    await password.setValue('Secret123!')
    await wrapper.find('form').trigger('submit.prevent')
    expect(wrapper.find('button').text()).toContain('Logging in')
    resolveLogin()
    await flushPromises()
  })

  it('shows error on login failure', async () => {
    mockStore.login.mockRejectedValueOnce({
      response: { data: { message: 'Invalid username or password' } }
    })
    const wrapper = mountLogin()
    await wrapper.find('input[type="text"]').setValue('alice')
    await wrapper.find('input[type="password"]').setValue('wrong')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(wrapper.text()).toContain('Invalid username or password')
  })
})

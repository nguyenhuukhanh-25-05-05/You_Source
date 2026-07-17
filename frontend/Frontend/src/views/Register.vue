<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100">
    <div class="w-full max-w-md bg-white rounded-lg shadow-md p-8">
      <h1 class="text-2xl font-bold text-center text-gray-800 mb-6">Register</h1>

      <div
        v-if="error"
        class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4"
      >
        {{ error }}
      </div>

      <div
        v-if="errors.length"
        class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4"
      >
        <ul class="list-disc pl-5">
          <li v-for="(e, i) in errors" :key="i">{{ e }}</li>
        </ul>
      </div>

      <form @submit.prevent="handleRegister">
        <div class="mb-4">
          <label class="block text-gray-700 text-sm font-medium mb-2">Full name</label>
          <input
            v-model="form.fullName"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Enter full name"
            required
          />
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 text-sm font-medium mb-2">Username</label>
          <input
            v-model="form.username"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Enter username"
            required
          />
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 text-sm font-medium mb-2">Email</label>
          <input
            v-model="form.email"
            type="email"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Enter email"
            required
          />
        </div>

        <div class="mb-6">
          <label class="block text-gray-700 text-sm font-medium mb-2">Password</label>
          <input
            v-model="form.password"
            type="password"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Min 8 chars, A-z 0-9 & symbol"
            required
            minlength="8"
          />
        </div>

        <button
          type="submit"
          :disabled="loading"
          class="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
        >
          {{ loading ? 'Registering...' : 'Register' }}
        </button>
      </form>

      <p class="mt-4 text-center text-sm text-gray-600">
        Already have an account?
        <router-link to="/login" class="text-blue-600 hover:text-blue-800">Login</router-link>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const form = reactive({ username: '', email: '', password: '', fullName: '' })
const loading = ref(false)
const error = ref('')
const errors = ref([])

async function handleRegister() {
  loading.value = true
  error.value = ''
  errors.value = []
  try {
    await authStore.register(form.username, form.email, form.password, form.fullName)
    router.push('/')
  } catch (err) {
    error.value = err.response?.data?.message || 'Registration failed'
    errors.value = err.response?.data?.errors || []
  } finally {
    loading.value = false
  }
}
</script>

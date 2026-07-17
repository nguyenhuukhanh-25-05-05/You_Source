<template>
  <div class="min-h-screen bg-gray-100">
    <nav class="bg-white shadow-sm">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
          <div class="flex items-center">
            <span class="text-xl font-bold text-gray-800">Starter Admin</span>
          </div>
          <div class="flex items-center gap-4">
            <span class="text-sm text-gray-600">{{ authStore.user?.fullName }}</span>
            <button class="text-sm text-red-600 hover:text-red-800" @click="handleLogout">
              Logout
            </button>
          </div>
        </div>
      </div>
    </nav>

    <div class="flex">
      <aside class="w-64 bg-white min-h-screen shadow-sm">
        <nav class="mt-4">
          <router-link
            to="/"
            class="block px-4 py-2 text-gray-700 hover:bg-gray-100"
            active-class="bg-blue-50 text-blue-700 border-r-2 border-blue-700"
          >
            Dashboard
          </router-link>
        </nav>
      </aside>

      <main class="flex-1 p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

async function handleLogout() {
  await authStore.logout()
  router.push('/login')
}
</script>

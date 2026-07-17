<template>
  <div class="flex items-center justify-between">
    <p class="text-sm text-gray-700">
      Showing {{ startItem }} to {{ endItem }} of {{ totalCount }} items
    </p>
    <div class="flex gap-1">
      <button
        :disabled="page <= 1"
        class="px-3 py-1 text-sm border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
        @click="changePage(page - 1)"
      >
        Previous
      </button>
      <button
        v-for="p in visiblePages"
        :key="p"
        :class="[
          'px-3 py-1 text-sm border rounded',
          p === page ? 'bg-blue-600 text-white' : 'hover:bg-gray-100'
        ]"
        @click="changePage(p)"
      >
        {{ p }}
      </button>
      <button
        :disabled="page >= totalPages"
        class="px-3 py-1 text-sm border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
        @click="changePage(page + 1)"
      >
        Next
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  page: { type: Number, default: 1 },
  pageSize: { type: Number, default: 10 },
  totalCount: { type: Number, default: 0 }
})

const emit = defineEmits(['page-change'])

const totalPages = computed(() => Math.ceil(props.totalCount / props.pageSize))
const startItem = computed(() => (props.page - 1) * props.pageSize + 1)
const endItem = computed(() => Math.min(props.page * props.pageSize, props.totalCount))

const visiblePages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, props.page - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  start = Math.max(1, end - maxVisible + 1)
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

function changePage(newPage) {
  if (newPage >= 1 && newPage <= totalPages.value && newPage !== props.page) {
    emit('page-change', newPage)
  }
}
</script>

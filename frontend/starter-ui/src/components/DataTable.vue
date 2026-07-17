<template>
  <div class="bg-white rounded-lg shadow overflow-hidden">
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
      <h2 class="text-lg font-semibold text-gray-800">{{ title }}</h2>
      <slot name="actions" />
    </div>

    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th
              v-for="col in columns"
              :key="col.key"
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer"
              @click="col.sortable !== false && $emit('sort', col.key)"
            >
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="loading">
            <td :colspan="columns.length" class="px-6 py-8 text-center text-gray-500">
              Loading...
            </td>
          </tr>
          <tr v-else-if="!items.length">
            <td :colspan="columns.length" class="px-6 py-8 text-center text-gray-500">No data</td>
          </tr>
          <tr v-for="(item, index) in items" v-else :key="index" class="hover:bg-gray-50">
            <td
              v-for="col in columns"
              :key="col.key"
              class="px-6 py-4 whitespace-nowrap text-sm text-gray-700"
            >
              <slot :name="col.key" :item="item" :value="item[col.key]">
                {{ item[col.key] }}
              </slot>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="totalCount > 0" class="px-6 py-4 border-t border-gray-200">
      <Pagination
        :page="page"
        :page-size="pageSize"
        :total-count="totalCount"
        @page-change="$emit('page-change', $event)"
      />
    </div>
  </div>
</template>

<script setup>
import Pagination from './Pagination.vue'

defineProps({
  title: { type: String, default: '' },
  columns: { type: Array, required: true },
  items: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  page: { type: Number, default: 1 },
  pageSize: { type: Number, default: 10 },
  totalCount: { type: Number, default: 0 }
})

defineEmits(['sort', 'page-change'])
</script>

<template>
  <div class="geography-filter">
    <select
      :value="region"
      data-testid="region-select"
      aria-label="Région"
      @change="onRegion(($event.target as HTMLSelectElement).value)"
    >
      <option value="">Toutes les régions</option>
      <option v-for="item in regions" :key="item.name" :value="item.name">{{ item.name }} ({{ item.museums }})</option>
    </select>
    <select
      :value="department"
      data-testid="department-select"
      aria-label="Département"
      @change="onDepartment(($event.target as HTMLSelectElement).value)"
    >
      <option value="">Tous les départements</option>
      <option v-for="item in departments" :key="item.name" :value="item.name">{{ item.name }} ({{ item.museums }})</option>
    </select>
  </div>
</template>

<script lang="ts">
import { computed, defineComponent, onMounted, ref } from 'vue';
import ApiService from '@/services/api';
import { RegionGeography } from '@/types/models';

export default defineComponent({
  name: 'GeographyFilter',
  props: {
    region: { type: String, default: '' },
    department: { type: String, default: '' }
  },
  emits: ['update:region', 'update:department', 'change'],
  setup(props, { emit }) {
    const regions = ref<RegionGeography[]>([]);

    // Sans région choisie, tous les départements ; une région restreint la liste aux siens
    const departments = computed(() => {
      const shown = props.region ? regions.value.filter(r => r.name === props.region) : regions.value;
      return shown.flatMap(r => r.departments).sort((a, b) => a.name.localeCompare(b.name, 'fr'));
    });

    onMounted(async () => {
      try {
        regions.value = await ApiService.getMuseumGeography();
      } catch (error) {
        // Sans la liste, les filtres restent vides : la recherche par texte et les autres filtres marchent
        console.error('Erreur lors du chargement des régions:', error);
      }
    });

    const onRegion = (value: string) => {
      emit('update:region', value);
      // Le département choisi n'est gardé que s'il appartient à la région
      const keeps = value === '' || regions.value.some(r => r.name === value && r.departments.some(d => d.name === props.department));
      if (!keeps) emit('update:department', '');
      emit('change');
    };

    const onDepartment = (value: string) => {
      emit('update:department', value);
      emit('change');
    };

    return { regions, departments, onRegion, onDepartment };
  }
});
</script>

<style scoped lang="scss">
.geography-filter {
  display: flex;
  flex-direction: column;
  gap: 8px;

  select {
    width: 100%;
    padding: 8px;
    border: 1px solid #ddd;
    border-radius: 4px;
    background: white;
  }
}
</style>

<template>
  <div class="filter-picker">
    <div v-if="modelValue" class="selected">
      <span class="selected-label" data-testid="filter-label">{{ label || 'Filtre actif' }}</span>
      <button type="button" class="clear" :aria-label="`Retirer le filtre ${placeholder}`" @click="clear">&times;</button>
    </div>
    <div v-else class="picker">
      <input
        type="text"
        v-model="text"
        :placeholder="placeholder"
        autocomplete="off"
        @input="onInput"
        @focus="onInput"
        @blur="close"
      />
      <ul v-if="open && options.length" class="options">
        <li v-for="option in options" :key="option.id" data-testid="filter-option" @mousedown.prevent="choose(option)">{{ option.label }}</li>
      </ul>
      <p v-else-if="open && text && !loading" class="empty">Aucun résultat</p>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType, ref } from 'vue';

export interface PickerOption {
  id: string;
  label: string;
}

const DELAY_MS = 250;

export default defineComponent({
  name: 'FilterPicker',
  props: {
    modelValue: { type: String, default: '' },
    label: { type: String, default: '' },
    placeholder: { type: String, required: true },
    // Cherche les propositions correspondant au texte saisi
    fetchOptions: { type: Function as PropType<(text: string) => Promise<PickerOption[]>>, required: true }
  },
  emits: ['update:modelValue', 'update:label'],
  setup(props, { emit }) {
    const text = ref('');
    const options = ref<PickerOption[]>([]);
    const open = ref(false);
    const loading = ref(false);
    let timer: ReturnType<typeof setTimeout> | undefined;
    let latest = 0;

    const load = async () => {
      const request = ++latest;
      loading.value = true;
      try {
        const found = await props.fetchOptions(text.value.trim());
        if (request === latest) options.value = found;
      } catch (error) {
        if (request === latest) options.value = [];
      } finally {
        if (request === latest) loading.value = false;
      }
    };

    const onInput = () => {
      open.value = true;
      if (timer) clearTimeout(timer);
      timer = setTimeout(load, DELAY_MS);
    };

    const close = () => {
      open.value = false;
    };

    const choose = (option: PickerOption) => {
      emit('update:modelValue', option.id);
      emit('update:label', option.label);
      text.value = '';
      options.value = [];
      close();
    };

    const clear = () => {
      emit('update:modelValue', '');
      emit('update:label', '');
    };

    return { text, options, open, loading, onInput, close, choose, clear };
  }
});
</script>

<style lang="scss" scoped>
.filter-picker {
  position: relative;
}

.selected {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 6px 8px;
  border-radius: 4px;
  background-color: rgba(66, 185, 131, 0.15);
  font-size: 0.9rem;

  .selected-label {
    overflow-wrap: anywhere;
  }

  .clear {
    border: none;
    background: none;
    font-size: 1.2rem;
    line-height: 1;
    cursor: pointer;
  }
}

.options {
  position: absolute;
  z-index: 10;
  left: 0;
  right: 0;
  max-height: 240px;
  margin: 2px 0 0;
  padding: 0;
  overflow-y: auto;
  list-style: none;
  background: #fff;
  border: 1px solid #ddd;
  border-radius: 4px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);

  li {
    padding: 6px 8px;
    font-size: 0.9rem;
    cursor: pointer;

    &:hover {
      background-color: #f0f0f0;
    }
  }
}

.empty {
  margin: 4px 0 0;
  font-size: 0.85rem;
  color: #666;
}
</style>

<template>
  <div class="artwork-card" data-testid="artwork-card" @click="$emit('click')">
    <div class="artwork-image">
      <img v-if="imageUrl" :src="imageUrl" :alt="title" />
      <div v-else class="no-image" aria-hidden="true">Pas d'aperçu</div>
      <span v-if="hasImage" class="image-badge" data-testid="pop-badge" title="Une image existe sur la plateforme POP">Image sur POP</span>
    </div>
    <div class="artwork-info">
      <h3 :title="title" data-testid="artwork-title">{{ shownTitle }}</h3>
      <p v-if="artists && artists.length">
        {{ formattedArtists }}
      </p>
      <p class="date">{{ date || 'Date inconnue' }}</p>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';
import { displayTitle } from '@/utils/title';

export default defineComponent({
  name: 'ArtworkCard',
  props: {
    id: {
      type: String,
      required: true
    },
    title: {
      type: String,
      default: ''
    },
    imageUrl: {
      type: String,
      default: ''
    },
    hasImage: {
      type: Boolean,
      default: false
    },
    date: {
      type: String,
      default: ''
    },
    artists: {
      type: Array as () => { id: string; firstName: string; lastName: string }[],
      default: () => []
    }
  },
  emits: ['click'],
  setup(props) {
    const formattedArtists = computed(() => {
      return props.artists
        .map(artist => `${artist.firstName || ''} ${artist.lastName || ''}`.trim())
        .join(', ');
    });

    const shownTitle = computed(() => displayTitle(props.title));

    return {
      formattedArtists,
      shownTitle
    };
  }
});
</script>

<style scoped lang="scss">
.artwork-card {
  background-color: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: transform 0.3s, box-shadow 0.3s;
  height: 100%;
  display: flex;
  flex-direction: column;

  &:hover {
    transform: translateY(-5px);
    box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  }

  .artwork-image {
    position: relative;
    width: 100%;
    padding-top: 75%; // 4:3 ratio

    .no-image {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f0f2f5;
      color: #8a8f98;
      font-size: 0.9rem;
    }

    .image-badge {
      position: absolute;
      top: 8px;
      right: 8px;
      padding: 2px 8px;
      border-radius: 10px;
      background: rgba(0, 0, 0, 0.65);
      color: #fff;
      font-size: 0.75rem;
    }

    img {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }

  .artwork-info {
    padding: 16px;
    flex-grow: 1;
    display: flex;
    flex-direction: column;

    h3 {
      margin-bottom: 8px;
      color: var(--primary-color);
      font-size: 1.1rem;
      // Ellipsis pour les titres longs
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    p {
      color: #666;
      font-size: 0.9rem;
      margin-bottom: 4px;
      // Ellipsis pour les noms d'artistes longs
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .date {
      margin-top: auto;
      font-size: 0.8rem;
      color: #888;
    }
  }
}
</style>

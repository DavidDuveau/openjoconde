<template>
  <div class="artwork-card" @click="$emit('click')">
    <div class="artwork-image">
      <img :src="imageUrl" :alt="title" />
    </div>
    <div class="artwork-info">
      <h3>{{ title || 'Sans titre' }}</h3>
      <p v-if="artists && artists.length">
        {{ formattedArtists }}
      </p>
      <p class="date">{{ date || 'Date inconnue' }}</p>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';

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
      default: '/placeholder-image.jpg'
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

    return {
      formattedArtists
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

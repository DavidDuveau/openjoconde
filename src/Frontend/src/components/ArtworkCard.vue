<template>
  <div class="artwork-card" @click="navigateToArtwork">
    <div class="artwork-image">
      <img v-if="artwork.imageUrl" :src="artwork.imageUrl" :alt="artwork.title" />
      <div v-else class="no-image">
        <span>Image non disponible</span>
      </div>
    </div>
    <div class="artwork-info">
      <h3 class="artwork-title">{{ artwork.title || 'Sans titre' }}</h3>
      <div class="artwork-artist">
        <template v-if="artwork.artists && artwork.artists.length > 0">
          {{ getArtistName(artwork.artists[0]) }}
          <span v-if="artwork.artists.length > 1"> et {{ artwork.artists.length - 1 }} autre(s)</span>
        </template>
        <template v-else>
          Artiste inconnu
        </template>
      </div>
      <div class="artwork-details">
        <span v-if="artwork.creationDate">{{ artwork.creationDate }}</span>
        <span v-if="artwork.denomination">{{ artwork.denomination }}</span>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';
import { useRouter } from 'vue-router';
import { Artwork, ArtworkArtist } from '@/types/models';

export default defineComponent({
  name: 'ArtworkCard',
  props: {
    artwork: {
      type: Object as PropType<Artwork>,
      required: true
    }
  },
  setup(props) {
    const router = useRouter();

    const navigateToArtwork = () => {
      router.push(`/artwork/${props.artwork.id}`);
    };

    const getArtistName = (artistWork: ArtworkArtist) => {
      if (!artistWork.artist) {
        return 'Artiste inconnu';
      }
      
      const { firstName, lastName } = artistWork.artist;
      if (firstName && lastName) {
        return `${firstName} ${lastName}`;
      } else if (lastName) {
        return lastName;
      } else if (firstName) {
        return firstName;
      } else {
        return 'Artiste inconnu';
      }
    };

    return {
      navigateToArtwork,
      getArtistName
    };
  }
});
</script>

<style lang="scss" scoped>
.artwork-card {
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s, box-shadow 0.3s;
  cursor: pointer;
  background-color: white;
  
  &:hover {
    transform: translateY(-5px);
    box-shadow: 0 15px 20px rgba(0, 0, 0, 0.1);
  }
}

.artwork-image {
  height: 200px;
  overflow: hidden;
  background-color: #f8f9fa;
  
  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.3s;
  }
  
  .no-image {
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #aaa;
  }
  
  .artwork-card:hover & img {
    transform: scale(1.05);
  }
}

.artwork-info {
  padding: 15px;
  text-align: left;
}

.artwork-title {
  margin: 0 0 5px 0;
  font-size: 1.1em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.artwork-artist {
  color: #42b983;
  font-size: 0.9em;
  margin-bottom: 8px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.artwork-details {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  font-size: 0.8em;
  color: #666;
  
  span {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}
</style>

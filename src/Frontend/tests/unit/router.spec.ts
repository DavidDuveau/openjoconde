import { describe, expect, it } from 'vitest';
import router from '@/router';

describe('routeur', () => {
  it.each([
    ['/', 'home'],
    ['/artworks', 'artworks'],
    ['/search', 'search'],
    ['/artworks/a1', 'artwork-detail'],
    ['/artists', 'artists'],
    ['/artists/a1', 'artist-detail'],
    ['/museums', 'museums'],
    ['/museums/m1', 'museum-detail'],
    ['/periods', 'periods'],
    ['/techniques', 'techniques'],
    ['/thematiques', 'thematiques'],
    ['/thematiques/portraits', 'thematique'],
    ['/about', 'about'],
    ['/inconnue/page', 'not-found']
  ])('%s mène à la route %s, dont la vue se charge', async (chemin, nom) => {
    const resolue = router.resolve(chemin);

    expect(resolue.name).toBe(nom);
    const composant = resolue.matched[0].components!.default as unknown;
    if (typeof composant === 'function') {
      expect(await (composant as () => Promise<unknown>)()).toBeTruthy();
    }
  });

  it("garde l'identifiant dans les paramètres", () => {
    expect(router.resolve('/museums/m42').params.id).toBe('m42');
  });

  it('mène à une ancre avec un décalage, sinon en haut de la page', () => {
    const defilement = router.options.scrollBehavior!;
    const de = router.resolve('/');

    expect(defilement(router.resolve('/about#sources'), de, null)).toEqual({ el: '#sources', top: 80 });
    expect(defilement(router.resolve('/about'), de, null)).toEqual({ top: 0 });
  });
});

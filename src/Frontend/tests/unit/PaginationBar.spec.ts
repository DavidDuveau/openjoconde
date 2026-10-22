import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PaginationBar from '@/components/PaginationBar.vue';

const numeros = (page: number, totalPages: number) =>
  mount(PaginationBar, { props: { page, totalPages } })
    .findAll('.pagination-number')
    .map((b) => b.text());

describe('PaginationBar', () => {
  it("ne s'affiche pas quand il n'y a qu'une page", () => {
    expect(mount(PaginationBar, { props: { page: 1, totalPages: 1 } }).find('nav').exists()).toBe(false);
  });

  it.each([
    [1, 10, ['1', '2', '3', '4', '5']],
    [5, 10, ['3', '4', '5', '6', '7']],
    [10, 10, ['6', '7', '8', '9', '10']],
    [2, 3, ['1', '2', '3']]
  ])('page %i sur %i : montre au plus cinq numéros autour de la page courante', (page, total, attendus) => {
    expect(numeros(page, total)).toEqual(attendus);
  });

  it('désactive « Précédent » sur la première page et « Suivant » sur la dernière', () => {
    const premiere = mount(PaginationBar, { props: { page: 1, totalPages: 3 } }).findAll('.pagination-button');
    expect(premiere[0].attributes('disabled')).toBeDefined();
    expect(premiere[1].attributes('disabled')).toBeUndefined();

    const derniere = mount(PaginationBar, { props: { page: 3, totalPages: 3 } }).findAll('.pagination-button');
    expect(derniere[0].attributes('disabled')).toBeUndefined();
    expect(derniere[1].attributes('disabled')).toBeDefined();
  });

  it('émet la page demandée', async () => {
    const vue = mount(PaginationBar, { props: { page: 2, totalPages: 5 } });
    const [precedent, suivant] = vue.findAll('.pagination-button');

    await precedent.trigger('click');
    await suivant.trigger('click');
    await vue.findAll('.pagination-number')[3].trigger('click');

    expect(vue.emitted('change')).toEqual([[1], [3], [4]]);
  });

  it('marque la page courante', () => {
    const vue = mount(PaginationBar, { props: { page: 2, totalPages: 5 } });

    expect(vue.get('.pagination-number.active').text()).toBe('2');
  });
});

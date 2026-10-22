import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import FilterPicker from '@/components/FilterPicker.vue';

const options = [
  { id: 'a', label: 'huile sur toile' },
  { id: 'b', label: 'huile sur bois' }
];

function monter(props: Record<string, unknown> = {}, fetchOptions = vi.fn().mockResolvedValue(options)) {
  const wrapper = mount(FilterPicker, { props: { placeholder: 'Rechercher une technique', fetchOptions, ...props } });
  return { wrapper, fetchOptions };
}

describe('FilterPicker', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("attend la fin de la saisie avant de chercher, puis liste les propositions", async () => {
    const { wrapper, fetchOptions } = monter();

    await wrapper.find('input').setValue('huile');
    expect(fetchOptions).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(250);
    await flushPromises();

    expect(fetchOptions).toHaveBeenCalledWith('huile');
    expect(wrapper.findAll('.options li').map(li => li.text())).toEqual(['huile sur toile', 'huile sur bois']);
  });

  it("ne lance qu'une recherche pour une saisie rapide", async () => {
    const { wrapper, fetchOptions } = monter();

    await wrapper.find('input').setValue('h');
    await wrapper.find('input').setValue('hu');
    await wrapper.find('input').setValue('hui');
    await vi.advanceTimersByTimeAsync(250);

    expect(fetchOptions).toHaveBeenCalledTimes(1);
    expect(fetchOptions).toHaveBeenCalledWith('hui');
  });

  it("émet l'identifiant et le libellé choisis", async () => {
    const { wrapper } = monter();
    await wrapper.find('input').setValue('huile');
    await vi.advanceTimersByTimeAsync(250);
    await flushPromises();

    await wrapper.findAll('.options li')[1].trigger('mousedown');

    expect(wrapper.emitted('update:modelValue')).toEqual([['b']]);
    expect(wrapper.emitted('update:label')).toEqual([['huile sur bois']]);
  });

  it('affiche le filtre choisi et permet de le retirer', async () => {
    const { wrapper } = monter({ modelValue: 'a', label: 'huile sur toile' });

    expect(wrapper.find('.selected-label').text()).toBe('huile sur toile');
    expect(wrapper.find('input').exists()).toBe(false);

    await wrapper.find('button.clear').trigger('click');

    expect(wrapper.emitted('update:modelValue')).toEqual([['']]);
    expect(wrapper.emitted('update:label')).toEqual([['']]);
  });

  it('affiche « Filtre actif » quand le libellé manque', () => {
    const { wrapper } = monter({ modelValue: 'a', label: '' });

    expect(wrapper.find('.selected-label').text()).toBe('Filtre actif');
  });

  it("dit qu'il n'y a aucun résultat quand l'API échoue", async () => {
    const { wrapper } = monter({}, vi.fn().mockRejectedValue(new Error('réseau')));

    await wrapper.find('input').setValue('xyz');
    await vi.advanceTimersByTimeAsync(250);
    await flushPromises();

    expect(wrapper.find('.empty').text()).toBe('Aucun résultat');
  });

  it('ne garde que la réponse de la dernière recherche', async () => {
    let finPremiere: (v: unknown) => void = () => undefined;
    const fetchOptions = vi
      .fn()
      .mockImplementationOnce(() => new Promise(resolve => { finPremiere = resolve; }))
      .mockResolvedValueOnce([{ id: 'z', label: 'seconde' }]);
    const { wrapper } = monter({}, fetchOptions);

    await wrapper.find('input').setValue('a');
    await vi.advanceTimersByTimeAsync(250);
    await wrapper.find('input').setValue('ab');
    await vi.advanceTimersByTimeAsync(250);
    await flushPromises();
    finPremiere([{ id: 'p', label: 'première, en retard' }]);
    await flushPromises();

    expect(wrapper.findAll('.options li').map(li => li.text())).toEqual(['seconde']);
  });
});

import { describe, expect, it } from 'vitest';
import { popNoticeUrl } from '@/utils/pop';

describe('popNoticeUrl', () => {
  it('construit l\'adresse de la notice POP d\'une référence Joconde', () => {
    expect(popNoticeUrl('M0569000236')).toBe('https://www.pop.culture.gouv.fr/notice/joconde/M0569000236');
  });

  it.each([null, undefined, '', 'M05 69', '../x', 'a/b', 'x?y=1'])('refuse la référence %s', reference => {
    expect(popNoticeUrl(reference as string | null | undefined)).toBeNull();
  });
});

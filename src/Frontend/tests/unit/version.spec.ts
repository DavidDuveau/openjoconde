import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import pkg from '../../package.json';

describe('version du produit', () => {
  it('est la même dans le front (package.json) et dans le back (Directory.Build.props)', () => {
    const props = readFileSync(resolve(process.cwd(), '../Backend/Directory.Build.props'), 'utf8');

    expect(props).toContain(`<Version>${pkg.version}</Version>`);
  });

  it('est injectée au build', () => {
    expect(__APP_VERSION__).toBe(pkg.version);
  });
});

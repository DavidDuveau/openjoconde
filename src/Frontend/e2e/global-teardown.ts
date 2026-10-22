import { dropDatabase } from './pile.mjs';

export default async function globalTeardown() {
  dropDatabase();
}

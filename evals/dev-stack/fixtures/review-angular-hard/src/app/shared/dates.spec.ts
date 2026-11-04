import { toLocalDate } from './dates';

describe('toLocalDate', () => {
  it('gives local midnight of the date', () => {
    const date = toLocalDate('2026-03-01');

    expect(date.getFullYear()).toBe(2026);
    expect(date.getMonth()).toBe(2);
    expect(date.getDate()).toBe(1);
    expect(date.getHours()).toBe(0);
  });
});

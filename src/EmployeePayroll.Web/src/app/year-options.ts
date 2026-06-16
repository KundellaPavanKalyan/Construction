/** Earliest year shown in payroll/attendance pickers (inclusive). */
const DEFAULT_START_YEAR = 2000;

/** Descending list: current year down to start year — no future years. */
export function pastAndCurrentYears(startYear = DEFAULT_START_YEAR): number[] {
  const current = new Date().getFullYear();
  const from = Math.min(Math.max(startYear, 1900), current);
  const years: number[] = [];
  for (let y = current; y >= from; y--) {
    years.push(y);
  }
  return years;
}

export function currentCalendarYear(): number {
  return new Date().getFullYear();
}

export function currentCalendarMonth(): number {
  return new Date().getMonth() + 1;
}

/** Clamp stored year to allowed range (past + current only). */
export function clampPayrollYear(year: number, startYear = DEFAULT_START_YEAR): number {
  const cy = currentCalendarYear();
  return Math.min(Math.max(year, startYear), cy);
}

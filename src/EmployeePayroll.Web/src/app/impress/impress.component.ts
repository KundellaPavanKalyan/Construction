import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ImpressRow } from '../models';
import { PayrollApiService } from '../payroll-api.service';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

@Component({
  selector: 'app-impress',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './impress.component.html',
  styleUrl: './impress.component.scss'
})
export class ImpressComponent implements OnInit {
  private readonly api = inject(PayrollApiService);

  month = currentCalendarMonth();
  year = currentCalendarYear();
  periodOpen = false;
  draftMonth = currentCalendarMonth();
  draftYear = currentCalendarYear();
  rows: ImpressRow[] = [];
  busy = false;
  error: string | null = null;
  success: string | null = null;

  readonly monthNames = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December'
  ];

  readonly yearOptions = pastAndCurrentYears();

  get periodLabel(): string {
    const m = Math.min(Math.max(this.month, 1), 12);
    return `${this.monthNames[m - 1]} ${this.year}`;
  }

  ngOnInit(): void {
    this.load();
  }

  openPeriodPanel(): void {
    this.draftMonth = this.month;
    this.draftYear = this.year;
    this.periodOpen = true;
  }

  closePeriodPanel(): void {
    this.periodOpen = false;
  }

  applyPeriodPanel(): void {
    this.month = this.draftMonth;
    this.year = clampPayrollYear(this.draftYear);
    this.periodOpen = false;
    this.load();
  }

  load(): void {
    this.error = null;
    this.success = null;
    this.busy = true;
    this.api.getImpressRows(this.month, this.year).subscribe({
      next: (list) => {
        this.busy = false;
        this.rows = list.map((r) => ({ ...r }));
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load names. Create the month on Dashboard and add attendance first.';
      }
    });
  }

  rowTotal(row: ImpressRow): number {
    return (row.week1 || 0) + (row.week2 || 0) + (row.week3 || 0) + (row.week4 || 0);
  }

  onWeekChange(row: ImpressRow): void {
    row.total = this.rowTotal(row);
  }

  formatAmount(value: number): string {
    if (value == null || value === 0) return '';
    return Number.isInteger(value) ? String(value) : value.toFixed(2);
  }

  saveAll(): void {
    this.error = null;
    this.success = null;
    for (const row of this.rows) {
      if (row.week1 < 0 || row.week2 < 0 || row.week3 < 0 || row.week4 < 0) {
        this.error = 'Weekly amounts cannot be negative.';
        return;
      }
    }
    this.busy = true;
    this.api
      .saveImpress({
        month: this.month,
        year: this.year,
        rows: this.rows.map((r) => ({
          employeeId: r.employeeId,
          week1: r.week1 || 0,
          week2: r.week2 || 0,
          week3: r.week3 || 0,
          week4: r.week4 || 0
        }))
      })
      .subscribe({
        next: (res) => {
          this.busy = false;
          this.success = res.message ?? 'Weekly totals saved to payroll Advance.';
          this.load();
        },
        error: (err) => {
          this.busy = false;
          this.error = err.error?.message ?? 'Save failed.';
        }
      });
  }
}

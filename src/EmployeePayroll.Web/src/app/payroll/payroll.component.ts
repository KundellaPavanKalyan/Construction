import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PagedResult, PayrollRow } from '../models';
import { PayrollApiService, PayrollListFilters } from '../payroll-api.service';
import { ExportDropdownComponent } from '../export-dropdown/export-dropdown.component';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

@Component({
  selector: 'app-payroll',
  standalone: true,
  imports: [CommonModule, FormsModule, ExportDropdownComponent],
  templateUrl: './payroll.component.html',
  styleUrl: './payroll.component.scss'
})
export class PayrollComponent implements OnInit {
  private readonly api = inject(PayrollApiService);

  month = currentCalendarMonth();
  year = currentCalendarYear();
  page = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;
  rows: PayrollRow[] = [];
  busy = false;
  error: string | null = null;
  info: string | null = null;

  filterOpen = false;
  periodOpen = false;
  draftMonth = currentCalendarMonth();
  draftYear = currentCalendarYear();

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

  useEmployee = false;
  useRole = false;
  useQualification = false;
  useSalaryRange = false;

  search = '';
  role = '';
  qualification = '';
  minSalary: number | null = null;
  maxSalary: number | null = null;

  ngOnInit(): void {
    this.load();
  }

  filtersPayload(): PayrollListFilters {
    return {
      search: this.useEmployee && this.search.trim() ? this.search.trim() : undefined,
      role: this.useRole && this.role.trim() ? this.role.trim() : undefined,
      qualification: this.useQualification && this.qualification.trim() ? this.qualification.trim() : undefined,
      minSalary: this.useSalaryRange ? this.minSalary : null,
      maxSalary: this.useSalaryRange ? this.maxSalary : null
    };
  }

  load(): void {
    this.error = null;
    this.info = null;
    this.busy = true;
    this.api.getPayrollPage(this.month, this.year, this.page, this.pageSize, this.filtersPayload()).subscribe({
      next: (res: PagedResult<PayrollRow>) => {
        this.busy = false;
        this.rows = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        if (res.totalCount === 0) {
          this.info =
            'No payroll rows for this month. Use Dashboard → Create new month (existing months are never overwritten).';
        }
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load payroll. Is the API running?';
      }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  openFilterPanel(): void {
    this.periodOpen = false;
    this.filterOpen = true;
  }

  closeFilterPanel(): void {
    this.filterOpen = false;
  }

  openPeriodPanel(): void {
    this.filterOpen = false;
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
    this.applyFilters();
  }

  applyFilterPanel(): void {
    this.filterOpen = false;
    this.applyFilters();
  }

  saveRow(row: PayrollRow): void {
    this.error = null;
    if (row.daysPresent < 0 || row.daysPresent > 31) {
      this.error = 'Days present must be between 0 and 31.';
      return;
    }
    if (row.otHours < 0 || row.advanceAmount < 0) {
      this.error = 'OT hours and advance cannot be negative.';
      return;
    }
    this.busy = true;
    this.api
      .updatePayroll(row.payrollId, {
        daysPresent: row.daysPresent,
        otHours: row.otHours,
        advanceAmount: row.advanceAmount
      })
      .subscribe({
        next: (updated) => {
          this.busy = false;
          Object.assign(row, updated);
        },
        error: (err) => {
          this.busy = false;
          const msg = err.error?.message ?? err.error?.title ?? 'Update failed.';
          this.error = typeof msg === 'string' ? msg : JSON.stringify(err.error);
        }
      });
  }

  export(): void {
    this.error = null;
    this.busy = true;
    this.api.exportPayroll(this.month, this.year, this.filtersPayload()).subscribe({
      next: (res) => {
        this.busy = false;
        const blob = res.body;
        if (!blob) {
          this.error = 'Empty export.';
          return;
        }
        const cd = res.headers.get('content-disposition');
        let filename = `Payroll_${this.month}_${this.year}.xlsx`;
        if (cd) {
          const star = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(cd);
          const plain = /filename="?([^";]+)"?/i.exec(cd);
          if (star?.[1]) filename = decodeURIComponent(star[1].trim());
          else if (plain?.[1]) filename = plain[1].trim();
        }
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.busy = false;
        this.error = 'Export failed (no data for this month or server error).';
      }
    });
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.load();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.load();
    }
  }
}

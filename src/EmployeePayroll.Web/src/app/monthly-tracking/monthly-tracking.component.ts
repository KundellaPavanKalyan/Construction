import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MonthlyTrackingRow, SaveMonthlyTrackingRequest } from '../models';
import { ExtractedInvoiceDataComponent } from '../extracted-invoice-data/extracted-invoice-data.component';
import { TrackingApiService } from '../tracking-api.service';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

@Component({
  selector: 'app-monthly-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule, ExtractedInvoiceDataComponent],
  templateUrl: './monthly-tracking.component.html',
  styleUrl: './monthly-tracking.component.scss'
})
export class MonthlyTrackingComponent implements OnInit {
  private readonly api = inject(TrackingApiService);

  month = currentCalendarMonth();
  year = currentCalendarYear();
  periodOpen = false;
  draftMonth = currentCalendarMonth();
  draftYear = currentCalendarYear();
  rows: MonthlyTrackingRow[] = [];
  busy = false;
  error: string | null = null;
  modalOpen = false;
  editingId: number | null = null;
  form: SaveMonthlyTrackingRequest = {
    month: this.month,
    year: this.year,
    projectSiteName: 'Monthly tracking',
    workDescription: null,
    status: 'In Progress',
    remarks: ''
  };

  readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  readonly yearOptions = pastAndCurrentYears();
  readonly statusOptions = ['Planned', 'In Progress', 'Completed', 'On Hold'];

  get periodLabel(): string {
    const m = Math.min(Math.max(this.month, 1), 12);
    return `${this.monthNames[m - 1]} ${this.year}`;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error = null;
    this.busy = true;
    this.api.getMonthly(this.month, this.year).subscribe({
      next: (list) => {
        this.busy = false;
        this.rows = list;
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load monthly tracking.';
      }
    });
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

  openCreate(): void {
    this.editingId = null;
    this.form = {
      month: this.month,
      year: this.year,
      projectSiteName: 'Monthly tracking',
      workDescription: null,
      status: 'In Progress',
      remarks: ''
    };
    this.modalOpen = true;
  }

  openEdit(row: MonthlyTrackingRow): void {
    this.editingId = row.monthlyTrackingId;
    this.form = {
      month: row.month,
      year: row.year,
      projectSiteName: row.projectSiteName,
      workDescription: row.workDescription,
      status: row.status,
      remarks: row.remarks
    };
    this.modalOpen = true;
  }

  closeModal(): void {
    this.modalOpen = false;
  }

  save(): void {
    this.error = null;
    this.busy = true;
    const body = {
      ...this.form,
      month: this.month,
      year: this.year,
      projectSiteName: 'Monthly tracking',
      workDescription: null
    };
    const req = this.editingId
      ? this.api.updateMonthly(this.editingId, body)
      : this.api.createMonthly(body);
    req.subscribe({
      next: () => {
        this.busy = false;
        this.modalOpen = false;
        this.load();
      },
      error: () => {
        this.busy = false;
        this.error = 'Save failed.';
      }
    });
  }

  remove(row: MonthlyTrackingRow): void {
    if (!confirm('Delete this monthly tracking entry?')) return;
    this.busy = true;
    this.api.deleteMonthly(row.monthlyTrackingId).subscribe({
      next: () => {
        this.busy = false;
        this.load();
      },
      error: () => {
        this.busy = false;
        this.error = 'Delete failed.';
      }
    });
  }
}

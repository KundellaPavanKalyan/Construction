import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MaterialTrackingRow, SaveMaterialTrackingRequest } from '../models';
import { TrackingApiService } from '../tracking-api.service';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

@Component({
  selector: 'app-material-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './material-tracking.component.html',
  styleUrl: './material-tracking.component.scss'
})
export class MaterialTrackingComponent implements OnInit {
  private readonly api = inject(TrackingApiService);

  month = currentCalendarMonth();
  year = currentCalendarYear();
  periodOpen = false;
  draftMonth = currentCalendarMonth();
  draftYear = currentCalendarYear();
  rows: MaterialTrackingRow[] = [];
  busy = false;
  error: string | null = null;
  modalOpen = false;
  editingId: number | null = null;
  form: SaveMaterialTrackingRequest = {
    month: this.month,
    year: this.year,
    materialName: '',
    quantity: 1,
    unit: 'nos',
    unitRate: 0,
    supplierName: '',
    receivedDate: null,
    remarks: ''
  };

  readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  readonly yearOptions = pastAndCurrentYears();

  get periodLabel(): string {
    const m = Math.min(Math.max(this.month, 1), 12);
    return `${this.monthNames[m - 1]} ${this.year}`;
  }

  get lineTotal(): number {
    return Math.round((this.form.quantity || 0) * (this.form.unitRate || 0) * 100) / 100;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error = null;
    this.busy = true;
    this.api.getMaterial(this.month, this.year).subscribe({
      next: (list) => {
        this.busy = false;
        this.rows = list;
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load material tracking.';
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
      materialName: '',
      quantity: 1,
      unit: 'nos',
      unitRate: 0,
      supplierName: '',
      receivedDate: null,
      remarks: ''
    };
    this.modalOpen = true;
  }

  openEdit(row: MaterialTrackingRow): void {
    this.editingId = row.materialTrackingId;
    this.form = {
      month: row.month,
      year: row.year,
      materialName: row.materialName,
      quantity: row.quantity,
      unit: row.unit,
      unitRate: row.unitRate,
      supplierName: row.supplierName,
      receivedDate: row.receivedDate,
      remarks: row.remarks
    };
    this.modalOpen = true;
  }

  closeModal(): void {
    this.modalOpen = false;
  }

  save(): void {
    if (!this.form.materialName.trim()) {
      this.error = 'Material name is required.';
      return;
    }
    if (this.form.quantity <= 0) {
      this.error = 'Quantity must be positive.';
      return;
    }
    this.error = null;
    this.busy = true;
    const body: SaveMaterialTrackingRequest = {
      ...this.form,
      month: this.month,
      year: this.year,
      materialName: this.form.materialName.trim()
    };
    const req = this.editingId
      ? this.api.updateMaterial(this.editingId, body)
      : this.api.createMaterial(body);
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

  remove(row: MaterialTrackingRow): void {
    if (!confirm(`Delete "${row.materialName}"?`)) return;
    this.busy = true;
    this.api.deleteMaterial(row.materialTrackingId).subscribe({
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

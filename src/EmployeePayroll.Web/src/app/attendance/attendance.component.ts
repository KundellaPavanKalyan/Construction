import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceEmployeeRow, CreateEmployeeRequest, SaveAttendanceRequest } from '../models';
import { PayrollApiService } from '../payroll-api.service';
import { ExportDropdownComponent } from '../export-dropdown/export-dropdown.component';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

export interface AttendanceGridRow {
  employeeId: number;
  employeeName: string;
  role: string | null;
  dailyWage: number;
  payrollId: number;
  advanceAmount: number;
  present: Record<number, 'P' | 'A' | ''>;
  ot: Record<number, number>;
}

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule, ExportDropdownComponent],
  templateUrl: './attendance.component.html',
  styleUrl: './attendance.component.scss'
})
export class AttendanceComponent implements OnInit {
  private readonly api = inject(PayrollApiService);

  month = currentCalendarMonth();
  year = currentCalendarYear();
  periodOpen = false;
  draftMonth = currentCalendarMonth();
  draftYear = currentCalendarYear();
  rows: AttendanceGridRow[] = [];
  busy = false;
  error: string | null = null;

  employeeModalOpen = false;
  newEmp: CreateEmployeeRequest = { employeeName: '', qualification: '', role: '', dailyWage: 0 };

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

  dayNumbers(): number[] {
    const dim = new Date(this.year, this.month, 0).getDate();
    return Array.from({ length: dim }, (_, i) => i + 1);
  }

  openPeriodPanel(): void {
    this.employeeModalOpen = false;
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

  openEmployeeModal(): void {
    this.periodOpen = false;
    this.employeeModalOpen = true;
  }

  closeEmployeeModal(): void {
    this.employeeModalOpen = false;
  }

  load(): void {
    this.error = null;
    this.busy = true;
    this.api.getAttendanceGrid(this.month, this.year).subscribe({
      next: (list) => {
        this.busy = false;
        this.rows = list.map((r) => this.toGridRow(r));
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load attendance. Create the month on the Dashboard first.';
      }
    });
  }

  private toGridRow(r: AttendanceEmployeeRow): AttendanceGridRow {
    const present: Record<number, 'P' | 'A' | ''> = {};
    const ot: Record<number, number> = {};
    try {
      const pj = JSON.parse(r.presentByDayJson || '{}') as Record<string, string>;
      for (const [k, v] of Object.entries(pj)) {
        const d = parseInt(k, 10);
        if (isNaN(d) || d < 1 || d > 31) continue;
        const u = (v || '').toUpperCase();
        present[d] = u === 'P' ? 'P' : u === 'A' ? 'A' : '';
      }
    } catch {
      /* ignore */
    }
    try {
      const oj = JSON.parse(r.otByDayJson || '{}') as Record<string, number>;
      for (const [k, v] of Object.entries(oj)) {
        const d = parseInt(k, 10);
        if (isNaN(d) || d < 1 || d > 31) continue;
        ot[d] = Number(v) || 0;
      }
    } catch {
      /* ignore */
    }
    return {
      employeeId: r.employeeId,
      employeeName: r.employeeName,
      role: r.role,
      dailyWage: r.dailyWage,
      payrollId: r.payrollId,
      advanceAmount: r.advanceAmount,
      present,
      ot
    };
  }

  setMark(row: AttendanceGridRow, day: number, v: 'P' | 'A' | ''): void {
    row.present[day] = v;
  }

  setMarkStr(row: AttendanceGridRow, day: number, v: string): void {
    const x = v === 'P' ? 'P' : v === 'A' ? 'A' : '';
    this.setMark(row, day, x);
  }

  setOt(row: AttendanceGridRow, day: number, v: string | number): void {
    const n = typeof v === 'number' ? v : parseFloat(String(v));
    row.ot[day] = Number.isFinite(n) && !Number.isNaN(n) ? n : 0;
  }

  getOt(row: AttendanceGridRow, day: number): number | '' {
    return row.ot[day] && row.ot[day] > 0 ? row.ot[day] : '';
  }

  getMark(row: AttendanceGridRow, day: number): 'P' | 'A' | '' {
    return row.present[day] ?? '';
  }

  saveAll(): void {
    this.error = null;
    const payload: SaveAttendanceRequest = {
      month: this.month,
      year: this.year,
      rows: this.rows.map((row) => ({
        employeeId: row.employeeId,
        presentByDayJson: JSON.stringify(
          Object.fromEntries(
            Object.entries(row.present)
              .map(([k, v]) => [String(k), v])
              .filter(([, v]) => v === 'P' || v === 'A')
          )
        ),
        otByDayJson: JSON.stringify(
          Object.fromEntries(
            Object.entries(row.ot)
              .filter(([, v]) => Number(v) > 0)
              .map(([k, v]) => [String(k), Number(v)])
          )
        )
      }))
    };
    this.busy = true;
    this.api.saveAttendance(payload).subscribe({
      next: () => {
        this.busy = false;
        this.load();
      },
      error: (err) => {
        this.busy = false;
        this.error = err.error?.message ?? 'Save failed.';
      }
    });
  }

  addEmployee(): void {
    this.error = null;
    if (!this.newEmp.employeeName.trim()) {
      this.error = 'Name is required.';
      return;
    }
    if (this.newEmp.dailyWage <= 0) {
      this.error = 'Daily wage must be positive.';
      return;
    }
    this.busy = true;
    this.api
      .createEmployee({
        employeeName: this.newEmp.employeeName.trim(),
        qualification: this.newEmp.qualification?.trim() || null,
        role: this.newEmp.role?.trim() || null,
        dailyWage: this.newEmp.dailyWage
      })
      .subscribe({
        next: () => {
          this.busy = false;
          this.employeeModalOpen = false;
          this.newEmp = { employeeName: '', qualification: '', role: '', dailyWage: 0 };
          this.load();
        },
        error: () => {
          this.busy = false;
          this.error = 'Could not add employee.';
        }
      });
  }

  remove(row: AttendanceGridRow): void {
    if (!confirm(`Remove ${row.employeeName} from the system?`)) return;
    this.busy = true;
    this.api.deleteEmployee(row.employeeId).subscribe({
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

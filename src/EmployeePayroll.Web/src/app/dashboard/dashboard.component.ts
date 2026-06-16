import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollApiService } from '../payroll-api.service';
import { pastAndCurrentYears, currentCalendarYear, currentCalendarMonth } from '../year-options';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  private readonly api = inject(PayrollApiService);

  createOpen = false;
  newMonth = currentCalendarMonth();
  newYear = currentCalendarYear();
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

  readonly years = pastAndCurrentYears();

  openCreate(): void {
    this.error = null;
    this.success = null;
    this.createOpen = true;
  }

  closeCreate(): void {
    this.createOpen = false;
  }

  confirmCreate(): void {
    this.error = null;
    this.success = null;
    this.busy = true;
    this.api.createPayrollMonth({ month: this.newMonth, year: this.newYear }).subscribe({
      next: () => {
        this.busy = false;
        this.success = `Payroll month created for ${this.monthNames[this.newMonth - 1]} ${this.newYear}.`;
        this.createOpen = false;
      },
      error: (err) => {
        this.busy = false;
        const msg = err.error?.message ?? err.error?.title ?? 'Could not create month.';
        this.error = typeof msg === 'string' ? msg : JSON.stringify(err.error);
      }
    });
  }
}

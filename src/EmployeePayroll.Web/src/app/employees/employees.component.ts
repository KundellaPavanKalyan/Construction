import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Employee, CreateEmployeeRequest } from '../models';
import { PayrollApiService } from '../payroll-api.service';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.scss'
})
export class EmployeesComponent implements OnInit {
  private readonly api = inject(PayrollApiService);

  employees: Employee[] = [];
  search = '';
  roleFilter = '';
  qualificationFilter = '';
  error: string | null = null;
  saving = false;
  filterOpen = false;

  modalOpen = false;
  editingId: number | null = null;
  form: CreateEmployeeRequest = this.emptyForm();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error = null;
    this.api.getEmployees(this.search.trim() || undefined, this.roleFilter.trim() || undefined, this.qualificationFilter.trim() || undefined).subscribe({
      next: (rows) => (this.employees = rows),
      error: () => (this.error = 'Could not load employees. Is the API running?')
    });
  }

  openFilterPanel(): void {
    this.modalOpen = false;
    this.filterOpen = true;
  }

  closeFilterPanel(): void {
    this.filterOpen = false;
  }

  applyFilters(): void {
    this.filterOpen = false;
    this.load();
  }

  clearFilters(): void {
    this.search = '';
    this.roleFilter = '';
    this.qualificationFilter = '';
    this.applyFilters();
  }

  openCreate(): void {
    this.filterOpen = false;
    this.editingId = null;
    this.form = this.emptyForm();
    this.modalOpen = true;
  }

  openEdit(emp: Employee): void {
    this.editingId = emp.employeeId;
    this.form = {
      employeeName: emp.employeeName,
      qualification: emp.qualification ?? '',
      role: emp.role ?? '',
      dailyWage: emp.dailyWage
    };
    this.modalOpen = true;
  }

  closeModal(): void {
    this.modalOpen = false;
  }

  save(): void {
    this.error = null;
    if (!this.form.employeeName.trim()) {
      this.error = 'Employee name is required.';
      return;
    }
    if (this.form.dailyWage <= 0) {
      this.error = 'Daily wage must be positive.';
      return;
    }
    this.saving = true;
    const payload: CreateEmployeeRequest = {
      employeeName: this.form.employeeName.trim(),
      qualification: this.form.qualification?.trim() || null,
      role: this.form.role?.trim() || null,
      dailyWage: this.form.dailyWage
    };
    const req$ =
      this.editingId == null
        ? this.api.createEmployee(payload)
        : this.api.updateEmployee(this.editingId, payload);
    req$.subscribe({
      next: () => {
        this.saving = false;
        this.modalOpen = false;
        this.load();
      },
      error: (err) => {
        this.saving = false;
        const msg = err.error?.message ?? err.error?.title ?? 'Save failed.';
        this.error = typeof msg === 'string' ? msg : JSON.stringify(err.error);
      }
    });
  }

  remove(emp: Employee): void {
    if (!confirm(`Delete ${emp.employeeName}? Payroll history will be removed.`)) return;
    this.api.deleteEmployee(emp.employeeId).subscribe({
      next: () => this.load(),
      error: () => (this.error = 'Delete failed.')
    });
  }

  private emptyForm(): CreateEmployeeRequest {
    return { employeeName: '', qualification: '', role: '', dailyWage: 0 };
  }
}

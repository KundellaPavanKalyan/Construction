import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  AttendanceEmployeeRow,
  CreateEmployeeRequest,
  CreatePayrollMonthRequest,
  Employee,
  PagedResult,
  PayrollRow,
  SaveAttendanceRequest,
  SaveImpressRequest,
  ImpressRow,
  UpdatePayrollRequest
} from './models';

export interface PayrollListFilters {
  search?: string;
  role?: string;
  qualification?: string;
  minSalary?: number | null;
  maxSalary?: number | null;
  sortBy?: string;
  sortDir?: string;
}

@Injectable({ providedIn: 'root' })
export class PayrollApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getEmployees(search?: string, role?: string, qualification?: string): Observable<Employee[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    if (qualification) params = params.set('qualification', qualification);
    return this.http.get<Employee[]>(`${this.base}/api/employees`, { params });
  }

  createEmployee(body: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(`${this.base}/api/employees`, body);
  }

  updateEmployee(id: number, body: CreateEmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.base}/api/employees/${id}`, body);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/employees/${id}`);
  }

  getAttendanceGrid(month: number, year: number): Observable<AttendanceEmployeeRow[]> {
    const params = new HttpParams().set('month', month).set('year', year);
    return this.http.get<AttendanceEmployeeRow[]>(`${this.base}/api/attendance`, { params });
  }

  saveAttendance(body: SaveAttendanceRequest): Observable<unknown> {
    return this.http.post(`${this.base}/api/attendance/save`, body);
  }

  getImpressRows(month: number, year: number): Observable<ImpressRow[]> {
    const params = new HttpParams().set('month', month).set('year', year);
    return this.http.get<ImpressRow[]>(`${this.base}/api/impress`, { params });
  }

  saveImpress(body: SaveImpressRequest): Observable<{ saved: number; message: string }> {
    return this.http.post<{ saved: number; message: string }>(`${this.base}/api/impress/save`, body);
  }

  getPayrollPage(
    month: number,
    year: number,
    page: number,
    pageSize: number,
    filters: PayrollListFilters
  ): Observable<PagedResult<PayrollRow>> {
    let params = new HttpParams()
      .set('month', month)
      .set('year', year)
      .set('page', page)
      .set('pageSize', pageSize);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.role) params = params.set('role', filters.role);
    if (filters.qualification) params = params.set('qualification', filters.qualification);
    if (filters.minSalary != null) params = params.set('minSalary', filters.minSalary);
    if (filters.maxSalary != null) params = params.set('maxSalary', filters.maxSalary);
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.sortDir) params = params.set('sortDir', filters.sortDir);
    return this.http.get<PagedResult<PayrollRow>>(`${this.base}/api/payroll`, { params });
  }

  createPayrollMonth(body: CreatePayrollMonthRequest): Observable<unknown> {
    return this.http.post(`${this.base}/api/payroll/months`, body);
  }

  updatePayroll(payrollId: number, body: UpdatePayrollRequest): Observable<PayrollRow> {
    return this.http.put<PayrollRow>(`${this.base}/api/payroll/${payrollId}`, body);
  }

  exportPayroll(month: number, year: number, filters: PayrollListFilters): Observable<HttpResponse<Blob>> {
    let params = new HttpParams().set('month', month).set('year', year);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.role) params = params.set('role', filters.role);
    if (filters.qualification) params = params.set('qualification', filters.qualification);
    if (filters.minSalary != null) params = params.set('minSalary', filters.minSalary);
    if (filters.maxSalary != null) params = params.set('maxSalary', filters.maxSalary);
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.sortDir) params = params.set('sortDir', filters.sortDir);
    return this.http.get(`${this.base}/api/payroll/export`, {
      params,
      responseType: 'blob',
      observe: 'response'
    });
  }
}

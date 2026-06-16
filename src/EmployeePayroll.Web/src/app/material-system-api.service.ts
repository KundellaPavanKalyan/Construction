import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface MaterialGridRow {
  materialName: string;
  quantities: (number | null)[];
  rowTotal: number;
}

export interface MaterialGrid {
  mode: string;
  entityName: string;
  month: number;
  year: number;
  invoiceColumns: string[];
  rows: MaterialGridRow[];
  grandTotal: number;
  hint?: string | null;
}

export interface MaterialUploadResult {
  invoiceId: number;
  invoiceNumber: string | null;
  vendorName: string | null;
  projectName: string | null;
  materialsCount: number;
  grid: MaterialGrid;
}

@Injectable({ providedIn: 'root' })
export class MaterialSystemApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  listProjects(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/api/material-system/projects`);
  }

  listVendors(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/api/material-system/vendors`);
  }

  createProject(name: string): Observable<{ id: number; name: string }> {
    return this.http.post<{ id: number; name: string }>(`${this.base}/api/material-system/projects`, { name });
  }

  createVendor(name: string): Observable<{ id: number; name: string }> {
    return this.http.post<{ id: number; name: string }>(`${this.base}/api/material-system/vendors`, { name });
  }

  deleteProject(name: string): Observable<void> {
    const params = new HttpParams().set('name', name);
    return this.http.delete<void>(`${this.base}/api/material-system/projects`, { params });
  }

  deleteVendor(name: string): Observable<void> {
    const params = new HttpParams().set('name', name);
    return this.http.delete<void>(`${this.base}/api/material-system/vendors`, { params });
  }

  getGrid(
    mode: 'project' | 'vendor',
    name: string,
    month: number,
    year: number,
    search?: string,
    vendor?: string,
    project?: string
  ): Observable<MaterialGrid> {
    let params = new HttpParams()
      .set('mode', mode)
      .set('name', name)
      .set('month', month)
      .set('year', year);
    if (search) params = params.set('search', search);
    if (vendor) params = params.set('vendor', vendor);
    if (project) params = params.set('project', project);
    return this.http.get<MaterialGrid>(`${this.base}/api/material-system/grid`, { params });
  }

  uploadInvoice(
    mode: 'project' | 'vendor',
    name: string,
    month: number,
    year: number,
    file: File
  ): Observable<MaterialUploadResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    const params = new HttpParams()
      .set('mode', mode)
      .set('name', name)
      .set('month', month)
      .set('year', year);
    return this.http.post<MaterialUploadResult>(`${this.base}/api/material-system/upload`, form, { params });
  }

  exportGrid(
    mode: 'project' | 'vendor',
    name: string,
    month: number,
    year: number,
    search?: string,
    vendor?: string,
    project?: string
  ): Observable<HttpResponse<Blob>> {
    let params = new HttpParams()
      .set('mode', mode)
      .set('name', name)
      .set('month', month)
      .set('year', year);
    if (search) params = params.set('search', search);
    if (vendor) params = params.set('vendor', vendor);
    if (project) params = params.set('project', project);
    return this.http.get(`${this.base}/api/material-system/export`, {
      params,
      responseType: 'blob',
      observe: 'response'
    });
  }
}

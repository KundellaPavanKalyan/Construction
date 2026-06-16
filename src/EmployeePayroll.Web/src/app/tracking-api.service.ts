import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  MaterialTrackingRow,
  MonthlyTrackingRow,
  SaveMaterialTrackingRequest,
  SaveMonthlyTrackingRequest
} from './models';

@Injectable({ providedIn: 'root' })
export class TrackingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMonthly(month: number, year: number): Observable<MonthlyTrackingRow[]> {
    const params = new HttpParams().set('month', month).set('year', year);
    return this.http.get<MonthlyTrackingRow[]>(`${this.base}/api/monthly-tracking`, { params });
  }

  createMonthly(body: SaveMonthlyTrackingRequest): Observable<MonthlyTrackingRow> {
    return this.http.post<MonthlyTrackingRow>(`${this.base}/api/monthly-tracking`, body);
  }

  updateMonthly(id: number, body: SaveMonthlyTrackingRequest): Observable<MonthlyTrackingRow> {
    return this.http.put<MonthlyTrackingRow>(`${this.base}/api/monthly-tracking/${id}`, body);
  }

  deleteMonthly(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/monthly-tracking/${id}`);
  }

  getMaterial(month: number, year: number): Observable<MaterialTrackingRow[]> {
    const params = new HttpParams().set('month', month).set('year', year);
    return this.http.get<MaterialTrackingRow[]>(`${this.base}/api/material-tracking`, { params });
  }

  createMaterial(body: SaveMaterialTrackingRequest): Observable<MaterialTrackingRow> {
    return this.http.post<MaterialTrackingRow>(`${this.base}/api/material-tracking`, body);
  }

  updateMaterial(id: number, body: SaveMaterialTrackingRequest): Observable<MaterialTrackingRow> {
    return this.http.put<MaterialTrackingRow>(`${this.base}/api/material-tracking/${id}`, body);
  }

  deleteMaterial(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/material-tracking/${id}`);
  }
}

import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { InvoiceDetail, InvoiceListItem } from './models';

@Injectable({ providedIn: 'root' })
export class InvoiceApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(): Observable<InvoiceListItem[]> {
    return this.http.get<InvoiceListItem[]>(`${this.base}/api/invoices`);
  }

  get(id: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.base}/api/invoices/${id}`);
  }

  upload(file: File): Observable<{ invoice: InvoiceDetail }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ invoice: InvoiceDetail }>(`${this.base}/api/invoices/upload`, form);
  }

  downloadFile(id: number, inline = false): Observable<HttpResponse<Blob>> {
    const url = inline
      ? `${this.base}/api/invoices/${id}/file?inline=true`
      : `${this.base}/api/invoices/${id}/file`;
    return this.http.get(url, {
      responseType: 'blob',
      observe: 'response'
    });
  }

  update(id: number, body: UpdateInvoiceRequest): Observable<InvoiceDetail> {
    return this.http.put<InvoiceDetail>(`${this.base}/api/invoices/${id}`, body);
  }

  exportExcel(): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/api/invoices/export`, {
      responseType: 'blob',
      observe: 'response'
    });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/invoices/${id}`);
  }
}

export interface UpdateInvoiceRequest {
  invoiceNumber: string | null;
  invoiceDate: string | null;
  vendorName: string | null;
  projectName: string | null;
  sgstAmount: number | null;
  cgstAmount: number | null;
  igstAmount: number | null;
  transportCharges: number | null;
  basicTotal: number | null;
  totalAmount: number | null;
}

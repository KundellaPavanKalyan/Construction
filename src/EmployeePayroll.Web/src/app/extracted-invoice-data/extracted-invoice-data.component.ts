import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InvoiceApiService, UpdateInvoiceRequest } from '../invoice-api.service';
import { InvoiceDetail, InvoiceListItem } from '../models';
import { ExportDropdownComponent } from '../export-dropdown/export-dropdown.component';

@Component({
  selector: 'app-extracted-invoice-data',
  standalone: true,
  imports: [CommonModule, FormsModule, ExportDropdownComponent],
  templateUrl: './extracted-invoice-data.component.html',
  styleUrl: './extracted-invoice-data.component.scss'
})
export class ExtractedInvoiceDataComponent implements OnInit {
  private readonly api = inject(InvoiceApiService);

  @Input() showTable = true;

  allInvoices: InvoiceListItem[] = [];
  invoices: InvoiceListItem[] = [];
  busy = false;
  error: string | null = null;
  success: string | null = null;
  filterOpen = false;
  invoiceFilter = '';
  dateFilter = '';
  vendorFilter = '';

  reviewOpen = false;
  review: ReviewForm | null = null;

  ngOnInit(): void {
    if (this.showTable) this.load();
  }

  load(): void {
    this.error = null;
    this.busy = true;
    this.api.list().subscribe({
      next: (rows) => {
        this.busy = false;
        this.allInvoices = rows;
        this.applyInvoiceFilters(false);
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load invoices. Is the API running?';
      }
    });
  }

  openFilterPanel(): void {
    this.filterOpen = true;
  }

  closeFilterPanel(): void {
    this.filterOpen = false;
  }

  applyInvoiceFilters(close = true): void {
    const invoice = this.invoiceFilter.trim().toLowerCase();
    const date = this.dateFilter.trim().toLowerCase();
    const vendor = this.vendorFilter.trim().toLowerCase();

    this.invoices = this.allInvoices.filter((row) => {
      const invoiceText = `${row.invoiceNumber ?? ''} ${row.originalFileName ?? ''}`.toLowerCase();
      const dateText = (row.invoiceDate ?? '').toLowerCase();
      const vendorText = (row.vendorName ?? '').toLowerCase();

      return (!invoice || invoiceText.includes(invoice))
        && (!date || dateText.includes(date))
        && (!vendor || vendorText.includes(vendor));
    });

    if (close) this.filterOpen = false;
  }

  clearInvoiceFilters(): void {
    this.invoiceFilter = '';
    this.dateFilter = '';
    this.vendorFilter = '';
    this.applyInvoiceFilters();
  }

  exportExcel(): void {
    if (this.invoices.length === 0) {
      this.error = 'No invoices to export.';
      return;
    }
    this.error = null;
    this.busy = true;
    this.api.exportExcel().subscribe({
      next: (res) => {
        this.busy = false;
        const blob = res.body;
        if (!blob) {
          this.error = 'Empty export.';
          return;
        }
        const cd = res.headers.get('content-disposition');
        let filename = 'Invoices.xlsx';
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
        this.success = 'Exported to Excel.';
      },
      error: (err) => {
        this.busy = false;
        this.error = this.readError(err, 'Export failed.');
      }
    });
  }

  openReview(invoice: InvoiceDetail): void {
    this.review = {
      invoiceId: invoice.invoiceId,
      originalFileName: invoice.originalFileName,
      extractionNotes: invoice.extractionNotes,
      invoiceNumber: invoice.invoiceNumber ?? '',
      invoiceDate: invoice.invoiceDate ?? '',
      vendorName: invoice.vendorName ?? '',
      projectName: invoice.projectName ?? '',
      sgstAmount: invoice.sgstAmount,
      cgstAmount: invoice.cgstAmount,
      igstAmount: invoice.igstAmount,
      transportCharges: invoice.transportCharges,
      basicTotal: invoice.basicTotal,
      totalAmount: invoice.totalAmount
    };
    this.reviewOpen = true;
  }

  editRow(row: InvoiceListItem): void {
    this.busy = true;
    this.api.get(row.invoiceId).subscribe({
      next: (detail) => {
        this.busy = false;
        this.openReview(detail);
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load invoice for editing.';
      }
    });
  }

  saveReview(): void {
    if (!this.review) return;
    const body: UpdateInvoiceRequest = {
      invoiceNumber: this.review.invoiceNumber.trim() || null,
      invoiceDate: this.review.invoiceDate.trim() || null,
      vendorName: this.review.vendorName.trim() || null,
      projectName: this.review.projectName.trim() || null,
      sgstAmount: this.review.sgstAmount,
      cgstAmount: this.review.cgstAmount,
      igstAmount: this.review.igstAmount,
      transportCharges: this.review.transportCharges,
      basicTotal: this.review.basicTotal,
      totalAmount: this.review.totalAmount
    };
    this.busy = true;
    this.api.update(this.review.invoiceId, body).subscribe({
      next: () => {
        this.busy = false;
        this.reviewOpen = false;
        this.review = null;
        this.success = 'Invoice saved.';
        if (this.showTable) this.load();
      },
      error: (err) => {
        this.busy = false;
        this.error = this.readError(err, 'Could not save invoice changes.');
      }
    });
  }

  cancelReview(): void {
    this.reviewOpen = false;
    this.review = null;
  }

  openOriginal(row: InvoiceListItem): void {
    const lower = row.originalFileName.toLowerCase();
    const isPdf = lower.endsWith('.pdf');
    const isImage = lower.endsWith('.jpg') || lower.endsWith('.jpeg') || lower.endsWith('.png');
    this.api.downloadFile(row.invoiceId, true).subscribe({
      next: (res) => {
        const blob = res.body;
        if (!blob) {
          this.error = 'Could not open file.';
          return;
        }
        const url = URL.createObjectURL(blob);
        if (isPdf || isImage) {
          window.open(url, '_blank', 'noopener,noreferrer');
        } else {
          const a = document.createElement('a');
          a.href = url;
          a.download = row.originalFileName;
          a.click();
        }
        setTimeout(() => URL.revokeObjectURL(url), 120_000);
      },
      error: () => {
        this.error = 'Could not open original file.';
      }
    });
  }

  remove(row: InvoiceListItem): void {
    if (!confirm(`Delete invoice "${row.invoiceNumber || row.originalFileName}"?`)) return;
    this.busy = true;
    this.api.delete(row.invoiceId).subscribe({
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

  formatAmount(value: number | null | undefined): string {
    if (value == null) return '';
    return Number.isInteger(value) ? String(value) : value.toFixed(2);
  }

  parseAmount(raw: string): number | null {
    const t = raw.trim().replace(/,/g, '');
    if (!t) return null;
    const n = Number(t);
    return Number.isFinite(n) ? n : null;
  }

  private readError(err: { error?: { message?: string }; status?: number }, fallback: string): string {
    const msg = err.error?.message ?? fallback;
    return typeof msg === 'string' ? msg : fallback;
  }
}

interface ReviewForm {
  invoiceId: number;
  originalFileName: string;
  extractionNotes: string | null;
  invoiceNumber: string;
  invoiceDate: string;
  vendorName: string;
  projectName: string;
  sgstAmount: number | null;
  cgstAmount: number | null;
  igstAmount: number | null;
  transportCharges: number | null;
  basicTotal: number | null;
  totalAmount: number | null;
}

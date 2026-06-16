import { CommonModule } from '@angular/common';
import { Component, ViewChild, inject } from '@angular/core';
import { InvoiceApiService } from '../invoice-api.service';
import { ExtractedInvoiceDataComponent } from '../extracted-invoice-data/extracted-invoice-data.component';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [CommonModule, ExtractedInvoiceDataComponent],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss'
})
export class InvoicesComponent {
  private readonly api = inject(InvoiceApiService);

  @ViewChild('invoiceReview') invoiceReview?: ExtractedInvoiceDataComponent;

  busy = false;
  error: string | null = null;
  success: string | null = null;
  duplicate: string | null = null;
  selectedFile: File | null = null;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.error = null;
    this.success = null;
    this.duplicate = null;
  }

  upload(): void {
    if (!this.selectedFile) {
      this.error = 'Choose an invoice file first.';
      return;
    }
    const name = this.selectedFile.name.toLowerCase();
    const ok = ['.pdf', '.docx', '.jpg', '.jpeg', '.png'].some((ext) => name.endsWith(ext));
    if (!ok) {
      this.error = 'Supported formats: PDF, DOCX, JPG, JPEG, PNG.';
      return;
    }
    this.error = null;
    this.success = null;
    this.duplicate = null;
    this.busy = true;
    this.api.upload(this.selectedFile).subscribe({
      next: (res) => {
        this.busy = false;
        this.selectedFile = null;
        this.success = 'Extraction completed successfully.';
        this.invoiceReview?.openReview(res.invoice);
      },
      error: (err) => {
        this.busy = false;
        const message = this.readError(err, 'Upload or extraction failed.');
        if (err.status === 409) {
          this.duplicate = message;
          return;
        }
        this.error = message;
      }
    });
  }

  private readError(err: { error?: { message?: string }; status?: number }, fallback: string): string {
    const msg = err.error?.message ?? fallback;
    return typeof msg === 'string' ? msg : fallback;
  }
}

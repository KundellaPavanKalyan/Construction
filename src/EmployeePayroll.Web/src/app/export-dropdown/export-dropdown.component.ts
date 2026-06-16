import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-export-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './export-dropdown.component.html',
  styleUrl: './export-dropdown.component.scss'
})
export class ExportDropdownComponent {
  @Input() disabled = false;
  @Input() canExport = true;
  @Input() tableSelector = '';
  @Input() fileBaseName = 'Export';
  @Input() title = 'Export';
  @Input() clientExcel = false;
  @Output() excel = new EventEmitter<void>();

  exportExcel(): void {
    if (this.disabled) return;
    if (this.clientExcel) {
      const table = this.getTable();
      if (!table) return;
      const html = this.buildDocumentHtml(this.prepareTableHtml(table));
      const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
      this.downloadBlob(blob, `${this.safeFileName()}.xls`);
      return;
    }
    this.excel.emit();
  }

  exportWord(): void {
    const table = this.getTable();
    if (!table) return;
    const html = this.buildDocumentHtml(this.prepareTableHtml(table));
    const blob = new Blob([html], { type: 'application/msword' });
    this.downloadBlob(blob, `${this.safeFileName()}.doc`);
  }

  exportPdf(): void {
    const table = this.getTable();
    if (!table) return;
    const popup = window.open('', '_blank', 'noopener,noreferrer');
    if (!popup) return;

    popup.document.open();
    popup.document.write(this.buildDocumentHtml(this.prepareTableHtml(table)));
    popup.document.close();
    popup.focus();
    popup.print();
  }

  private getTable(): HTMLTableElement | null {
    if (this.disabled || !this.canExport || !this.tableSelector) return null;
    return document.querySelector<HTMLTableElement>(this.tableSelector);
  }

  private prepareTableHtml(table: HTMLTableElement): string {
    const clone = table.cloneNode(true) as HTMLTableElement;

    clone.querySelectorAll('input').forEach((input) => {
      const text = document.createTextNode((input as HTMLInputElement).value);
      input.replaceWith(text);
    });

    clone.querySelectorAll('select').forEach((select) => {
      const selected = (select as HTMLSelectElement).selectedOptions.item(0)?.textContent?.trim() ?? '';
      select.replaceWith(document.createTextNode(selected));
    });

    clone.querySelectorAll('button').forEach((button) => button.remove());
    return clone.outerHTML;
  }

  private buildDocumentHtml(tableHtml: string): string {
    return `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <title>${this.escapeHtml(this.title)}</title>
  <style>
    body { font-family: Arial, sans-serif; }
    h2 { margin-bottom: 16px; }
    table { border-collapse: collapse; width: 100%; }
    th, td { border: 1px solid #999; padding: 6px; }
    th { background: #f2f2f2; }
    .text-end { text-align: right; }
  </style>
</head>
<body>
  <h2>${this.escapeHtml(this.title)}</h2>
  ${tableHtml}
</body>
</html>`;
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  private safeFileName(): string {
    return this.fileBaseName.replace(/[\\/:*?"<>|]+/g, '_').trim() || 'Export';
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MaterialGrid, MaterialSystemApiService } from '../material-system-api.service';
import { ExportDropdownComponent } from '../export-dropdown/export-dropdown.component';
import {
  pastAndCurrentYears,
  currentCalendarYear,
  currentCalendarMonth,
  clampPayrollYear
} from '../year-options';

@Component({
  selector: 'app-material-tracking-board',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ExportDropdownComponent],
  templateUrl: './material-tracking-board.component.html',
  styleUrl: './material-tracking-board.component.scss'
})
export class MaterialTrackingBoardComponent implements OnInit {
  private readonly api = inject(MaterialSystemApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  mode: 'project' | 'vendor' = 'project';
  entityName = '';
  menuNames: string[] = [];
  grid: MaterialGrid | null = null;

  month = currentCalendarMonth();
  year = currentCalendarYear();
  search = '';
  vendorFilter = '';
  projectFilter = '';
  filterOpen = false;

  busy = false;
  error: string | null = null;
  success: string | null = null;

  readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  readonly yearOptions = pastAndCurrentYears();

  get menuPath(): string {
    return this.mode === 'project' ? '/app/material-tracking/projects' : '/app/material-tracking/vendors';
  }

  get boardTitle(): string {
    return this.mode === 'project' ? 'Projects Material Tracking' : 'Vendor Material Tracking';
  }

  get invoiceHeaders(): string[] {
    return this.grid?.invoiceColumns ?? [];
  }

  get periodLabel(): string {
    const m = Math.min(Math.max(this.month, 1), 12);
    return `${this.monthNames[m - 1]} ${this.year}`;
  }

  ngOnInit(): void {
    this.route.data.subscribe((d) => {
      this.mode = d['mode'] === 'vendor' ? 'vendor' : 'project';
      this.loadMenu();
    });
    this.route.paramMap.subscribe((p) => {
      const raw = p.get('name');
      this.entityName = raw ? decodeURIComponent(raw) : '';
      if (this.entityName) this.loadGrid();
    });
  }

  loadMenu(): void {
    const req = this.mode === 'project' ? this.api.listProjects() : this.api.listVendors();
    req.subscribe({
      next: (list) => (this.menuNames = list),
      error: () => {}
    });
  }

  loadGrid(): void {
    this.error = null;
    this.busy = true;
    this.api
      .getGrid(
        this.mode,
        this.entityName,
        this.month,
        this.year,
        this.search.trim() || undefined,
        this.vendorFilter.trim() || undefined,
        this.projectFilter.trim() || undefined
      )
      .subscribe({
        next: (g) => {
          this.busy = false;
          this.grid = g;
        },
        error: () => {
          this.busy = false;
          this.error = 'Could not load material table.';
        }
      });
  }

  selectEntity(name: string): void {
    void this.router.navigate([this.menuPath, encodeURIComponent(name)]);
  }

  applyFilters(): void {
    this.year = clampPayrollYear(this.year);
    this.filterOpen = false;
    this.loadGrid();
  }

  openFilterPanel(): void {
    this.filterOpen = true;
  }

  closeFilterPanel(): void {
    this.filterOpen = false;
  }

  exportExcel(): void {
    this.error = null;
    this.busy = true;
    this.api
      .exportGrid(
        this.mode,
        this.entityName,
        this.month,
        this.year,
        this.search.trim() || undefined,
        this.vendorFilter.trim() || undefined,
        this.projectFilter.trim() || undefined
      )
      .subscribe({
        next: (res) => {
          this.busy = false;
          const blob = res.body;
          if (!blob) {
            this.error = 'Empty export.';
            return;
          }
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Material_${this.entityName}.xlsx`;
          a.click();
          URL.revokeObjectURL(url);
          this.success = 'Exported to Excel.';
        },
        error: () => {
          this.busy = false;
          this.error = 'Export failed.';
        }
      });
  }

  qtyAt(row: { quantities: (number | null)[] }, index: number): number | null {
    return row.quantities[index] ?? null;
  }
}

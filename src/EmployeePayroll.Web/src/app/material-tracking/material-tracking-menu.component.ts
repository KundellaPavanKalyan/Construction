import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MaterialSystemApiService } from '../material-system-api.service';

@Component({
  selector: 'app-material-tracking-menu',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './material-tracking-menu.component.html',
  styleUrl: './material-tracking-menu.component.scss'
})
export class MaterialTrackingMenuComponent implements OnInit {
  private readonly api = inject(MaterialSystemApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  mode: 'project' | 'vendor' = 'project';
  names: string[] = [];
  newName = '';
  busy = false;
  error: string | null = null;

  get title(): string {
    return this.mode === 'project' ? 'Projects Menu' : 'Vendor Menu';
  }

  get basePath(): string {
    return this.mode === 'project' ? '/app/material-tracking/projects' : '/app/material-tracking/vendors';
  }

  ngOnInit(): void {
    this.route.data.subscribe((d) => {
      this.mode = d['mode'] === 'vendor' ? 'vendor' : 'project';
      this.load();
    });
  }

  load(): void {
    this.error = null;
    this.busy = true;
    const req = this.mode === 'project' ? this.api.listProjects() : this.api.listVendors();
    req.subscribe({
      next: (list) => {
        this.busy = false;
        this.names = list;
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not load list.';
      }
    });
  }

  addName(): void {
    const name = this.newName.trim();
    if (!name) return;
    this.busy = true;
    const req = this.mode === 'project' ? this.api.createProject(name) : this.api.createVendor(name);
    req.subscribe({
      next: () => {
        this.busy = false;
        this.newName = '';
        this.load();
      },
      error: () => {
        this.busy = false;
        this.error = 'Could not add name.';
      }
    });
  }

  open(name: string): void {
    void this.router.navigate([this.basePath, encodeURIComponent(name)]);
  }

  remove(name: string, event: Event): void {
    event.stopPropagation();
    const label = this.mode === 'project' ? 'project' : 'vendor';
    if (!confirm(`Delete ${label} "${name}"? Invoices will be unlinked from this ${label}.`)) return;

    this.error = null;
    this.busy = true;
    const req = this.mode === 'project' ? this.api.deleteProject(name) : this.api.deleteVendor(name);
    req.subscribe({
      next: () => {
        this.busy = false;
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.busy = false;
        const msg = typeof err.error?.message === 'string' ? err.error.message : null;
        this.error = msg ?? `Could not delete ${label}. Restart the API if delete was recently added.`;
      }
    });
  }
}

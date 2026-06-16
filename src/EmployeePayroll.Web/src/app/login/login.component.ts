import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  username = '';
  password = '';
  error: string | null = null;
  busy = false;

  submit(): void {
    this.error = null;
    if (!this.username.trim() || !this.password) {
      this.error = 'Username and password are required.';
      return;
    }
    this.busy = true;
    this.auth.login(this.username.trim(), this.password).subscribe({
      next: () => {
        this.busy = false;
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/app/dashboard';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.busy = false;
        const msg = err.error?.message ?? 'Login failed.';
        this.error = typeof msg === 'string' ? msg : 'Invalid credentials.';
      }
    });
  }
}

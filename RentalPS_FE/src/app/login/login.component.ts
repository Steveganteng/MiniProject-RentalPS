import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../services/auth/auth.service';
import { LoginService } from '../services/login/login.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly loginService = inject(LoginService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  loginForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  isSubmitting = false;
  errorMessage = '';

  onSubmitHandler(): void {
    if (this.loginForm.invalid || this.isSubmitting) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.isSubmitting = true;

    this.loginService
      .login(this.loginForm.getRawValue())
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (response) => {
          const role = response.role.toLowerCase();
          const dashboard =
            role === 'admin'
              ? '/admin/dashboard'
              : role === 'customer'
                ? '/customer/dashboard'
                : null;

          if (!dashboard) {
            this.errorMessage = 'Role akun tidak dikenali.';
            return;
          }

          this.authService.setSession(response.token, response.role);
          void this.router.navigateByUrl(dashboard);
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.message;
          this.errorMessage =
            typeof message === 'string'
              ? message
              : error.status === 0
                ? 'Tidak dapat terhubung ke server. Pastikan API sedang berjalan.'
                : 'Login gagal. Periksa email dan password Anda.';
        }
      });
  }
}

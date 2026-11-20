import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { RegistrationService } from '../services/register/registration.service';

const passwordsMatchValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  return password === confirmPassword ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly registrationService = inject(RegistrationService);

  registerForm = this.formBuilder.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatchValidator }
  );

  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  onSubmitHandler(): void {
    if (this.registerForm.invalid || this.isSubmitting) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.isSubmitting = true;

    this.registrationService
      .register(this.registerForm.getRawValue())
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (response) => {
          this.successMessage = response.message || 'Registrasi berhasil. Silakan login.';
          this.registerForm.reset();
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.message;
          if (typeof message === 'string') {
            this.errorMessage = message;
            return;
          }

          const validationErrors = error.error?.errors;
          if (validationErrors && typeof validationErrors === 'object') {
            const firstError = Object.values(validationErrors)
              .flat()
              .find((value): value is string => typeof value === 'string');
            if (firstError) {
              this.errorMessage = firstError;
              return;
            }
          }

          this.errorMessage =
            error.status === 0
              ? 'Tidak dapat terhubung ke server. Pastikan API sedang berjalan.'
              : error.status === 409
                ? 'Email sudah terdaftar.'
                : 'Registrasi gagal. Periksa kembali data Anda.';
        }
      });
  }
}

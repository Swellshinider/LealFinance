import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { RegisterRequest } from '../../core/models/auth';
import { AuthService } from '../../core/services/auth.service';

/**
 * Creates a validator that checks password confirmation fields.
 */
function passwordMatchValidator(): ValidatorFn {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const password = formGroup.get('password')?.value as string;
    const confirmPassword = formGroup.get('confirmPassword')?.value as string;

    return password && confirmPassword && password !== confirmPassword
      ? { passwordMismatch: true }
      : null;
  };
}

/**
 * Registration view for new users.
 */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly formBuilder = inject(FormBuilder);

  /** Error message shown after failed registration attempts. */
  public errorMessage = '';

  /** Success message shown after successful registration. */
  public successMessage = '';

  /** Indicates submission in progress. */
  public isSubmitting = false;

  /** Maximum allowed e-mail length. */
  public readonly emailMaxLength = 256;

  /** Maximum allowed full-name length. */
  public readonly fullNameMaxLength = 120;

  /** Reactive registration form. */
  public readonly registerForm = this.formBuilder.nonNullable.group(
    {
      fullName: ['', [Validators.required, Validators.maxLength(this.fullNameMaxLength)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordMatchValidator() }
  );

  public constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  /**
   * Returns password strength guidance while user types.
   */
  public get passwordStrengthMessage(): string {
    const password = this.registerForm.controls.password.value;
    if (!password) {
      return 'Use at least 12 characters with uppercase, lowercase, number and symbol.';
    }

    const score = this.getPasswordStrengthScore(password);
    if (score <= 2) {
      return 'Weak master password. Strengthen it before creating your account.';
    }

    if (score === 3) {
      return 'Moderate master password. Consider adding more complexity.';
    }

    if (score === 4) {
      return 'Strong master password.';
    }

    return 'Very strong master password.';
  }

  /**
   * Returns whether the current strength should be shown as warning.
   */
  public get isPasswordStrengthWarning(): boolean {
    const password = this.registerForm.controls.password.value;
    return !!password && this.getPasswordStrengthScore(password) <= 2;
  }

  /**
   * Submits registration data.
   */
  public onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    const payload: RegisterRequest = {
      fullName: this.registerForm.controls.fullName.value,
      email: this.registerForm.controls.email.value,
      password: this.registerForm.controls.password.value
    };

    this.authService
      .register(payload)
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'Registration successful. You can now log in.';
          this.registerForm.reset();
          void this.router.navigate(['/login']);
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 409) {
            this.errorMessage = 'A user with this email already exists.';
            return;
          }

          if (error.status === 0) {
            this.errorMessage = 'Cannot reach the server right now. Please try again in a moment.';
            return;
          }

          this.errorMessage = 'Registration failed. Please try again.';
        }
      });
  }

  private getPasswordStrengthScore(password: string): number {
    let score = 0;

    if (password.length >= 8) {
      score += 1;
    }

    if (password.length >= 12) {
      score += 1;
    }

    if (/[A-Z]/.test(password)) {
      score += 1;
    }

    if (/[a-z]/.test(password)) {
      score += 1;
    }

    if (/\d/.test(password)) {
      score += 1;
    }

    if (/[^A-Za-z0-9]/.test(password)) {
      score += 1;
    }

    return score;
  }
}

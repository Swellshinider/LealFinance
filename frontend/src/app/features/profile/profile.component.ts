import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, NgZone, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';

import {
  EnableTwoFactorRequest,
  RecoverPasswordRequest,
  TwoFactorSetupResponse,
  UpdateProfileRequest,
  UserProfile
} from '../../core/models/auth';
import { AuthService } from '../../core/services/auth.service';

/**
 * Profile page allowing authenticated users to update identity and avatar.
 */
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly maxPhotoBytes = 5 * 1024 * 1024;

  @ViewChild('profilePhotoInput')
  private profilePhotoInput?: ElementRef<HTMLInputElement>;

  /** Current profile photo preview URL. */
  public profilePhotoPreview: string | null = null;

  /** Error message shown when save fails. */
  public errorMessage = '';

  /** Validation message related to selected photo constraints. */
  public photoValidationMessage = '';

  /** Success message shown after successful update. */
  public successMessage = '';

  /** Selected image file name shown near the upload button. */
  public selectedPhotoFileName = 'No file selected';

  /** Indicates profile loading in progress. */
  public isLoading = true;

  /** Indicates profile save in progress. */
  public isSaving = false;

  /** Indicates two-factor setup submission in progress. */
  public isEnablingTwoFactor = false;

  /** Indicates password recovery request in progress. */
  public isRecoveringPassword = false;

  /** Current OTPAUTH URI used for authenticator setup. */
  public otpAuthUri = '';

  /** Generated QR image source for authenticator setup. */
  public otpQrCodeDataUrl = '';

  /** Current setup view mode inside the modal. */
  public twoFactorSetupMode: 'qr' | 'link' = 'qr';

  /** Controls visibility of the mandatory 2FA modal. */
  public showTwoFactorSetupModal = false;

  /** Current manual shared key used for authenticator setup. */
  public twoFactorManualKey = '';

  /** Error message for two-factor setup section. */
  public twoFactorErrorMessage = '';

  /** Success message for two-factor setup section. */
  public twoFactorSuccessMessage = '';

  /** Error message for password recovery section. */
  public passwordRecoveryErrorMessage = '';

  /** Success message for password recovery section. */
  public passwordRecoverySuccessMessage = '';

  /** Maximum allowed full name length. */
  public readonly fullNameMaxLength = 120;

  /** Maximum allowed e-mail length. */
  public readonly emailMaxLength = 256;

  /** Reactive profile form. */
  public readonly profileForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(this.fullNameMaxLength)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(this.emailMaxLength)]],
    profilePhotoUrl: ['']
  });

  /** Reactive form for enabling two-factor authentication. */
  public readonly enableTwoFactorForm = this.formBuilder.nonNullable.group({
    verificationCode: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]]
  });

  /** Reactive form for recovering password with two-factor code. */
  public readonly recoverPasswordForm = this.formBuilder.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    verificationCode: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]]
  });

  public constructor(
    private readonly authService: AuthService
  ) {}

  /**
   * Returns whether two-factor is currently enabled.
   */
  public get isTwoFactorEnabled(): boolean {
    return this.authService.getCurrentUserProfile()?.isTwoFactorEnabled ?? false;
  }

  /**
   * Returns whether save should be disabled.
   */
  public get isSaveDisabled(): boolean {
    return this.isSaving || this.isLoading || !!this.photoValidationMessage;
  }

  /**
   * Loads the current profile on page init.
   */
  public ngOnInit(): void {
    this.authService
      .getProfile()
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isLoading = false;
          });
        })
      )
      .subscribe({
        next: (profile: UserProfile) => {
          this.runInAngular(() => {
            this.profileForm.patchValue({
              fullName: profile.fullName,
              email: profile.email,
              profilePhotoUrl: profile.profilePhotoUrl ?? ''
            });
            this.profilePhotoPreview = profile.profilePhotoUrl;

            if (!profile.isTwoFactorEnabled) {
              this.showTwoFactorSetupModal = true;
              this.loadTwoFactorSetup();
            }
            else
            {
              this.showTwoFactorSetupModal = false;
            }
          });
        },
        error: () => {
          this.runInAngular(() => {
            this.errorMessage = 'Unable to load your profile right now.';
          });
        }
      });
  }

  /**
   * Loads the authenticator setup data for the current user.
   */
  public loadTwoFactorSetup(): void {
    this.twoFactorErrorMessage = '';
    this.twoFactorSuccessMessage = '';
    this.twoFactorSetupMode = 'qr';
    this.otpQrCodeDataUrl = '';

    this.authService.getTwoFactorSetup().subscribe({
      next: (setup: TwoFactorSetupResponse) => {
        this.runInAngular(() => {
          this.twoFactorManualKey = setup.manualEntryKey;
          this.otpAuthUri = setup.otpAuthUri;
          this.otpQrCodeDataUrl = this.buildQrCodeImageUrl(setup.otpAuthUri);
          this.showTwoFactorSetupModal = !this.isTwoFactorEnabled;
        });
      },
      error: () => {
        this.runInAngular(() => {
          this.twoFactorErrorMessage = 'Unable to load authenticator setup details right now.';
          this.showTwoFactorSetupModal = !this.isTwoFactorEnabled;
        });
      }
    });
  }

  /**
   * Switches between QR and direct-link setup options.
   */
  public setTwoFactorSetupMode(mode: 'qr' | 'link'): void {
    this.twoFactorSetupMode = mode;
  }

  /**
   * Enables two-factor authentication using the provided authenticator code.
   */
  public enableTwoFactor(): void {
    this.twoFactorErrorMessage = '';
    this.twoFactorSuccessMessage = '';

    if (this.enableTwoFactorForm.invalid) {
      this.enableTwoFactorForm.markAllAsTouched();
      return;
    }

    this.isEnablingTwoFactor = true;

    const payload: EnableTwoFactorRequest = {
      verificationCode: this.enableTwoFactorForm.controls.verificationCode.value.trim()
    };

    this.authService
      .enableTwoFactor(payload)
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isEnablingTwoFactor = false;
          });
        })
      )
      .subscribe({
        next: () => {
          this.runInAngular(() => {
            this.twoFactorSuccessMessage = 'Two-factor authentication is now enabled.';
            this.enableTwoFactorForm.reset({ verificationCode: '' });
            this.showTwoFactorSetupModal = false;
          });
        },
        error: () => {
          this.runInAngular(() => {
            this.twoFactorErrorMessage = 'Invalid verification code. Please try again with a fresh code.';
          });
        }
      });
  }

  /**
   * Recovers the login password using a valid 2FA code.
   */
  public recoverPassword(): void {
    this.passwordRecoveryErrorMessage = '';
    this.passwordRecoverySuccessMessage = '';

    if (this.recoverPasswordForm.invalid) {
      this.recoverPasswordForm.markAllAsTouched();
      return;
    }

    this.isRecoveringPassword = true;

    const payload: RecoverPasswordRequest = {
      newPassword: this.recoverPasswordForm.controls.newPassword.value,
      verificationCode: this.recoverPasswordForm.controls.verificationCode.value.trim()
    };

    this.authService
      .recoverPassword(payload)
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isRecoveringPassword = false;
          });
        })
      )
      .subscribe({
        next: () => {
          this.runInAngular(() => {
            this.passwordRecoverySuccessMessage = 'Password recovered successfully.';
            this.recoverPasswordForm.reset({
              newPassword: '',
              verificationCode: ''
            });
          });
        },
        error: (error: HttpErrorResponse) => {
          this.runInAngular(() => {
            if (error.status === 400) {
              this.passwordRecoveryErrorMessage = 'Invalid code or password rules were not satisfied.';
              return;
            }

            this.passwordRecoveryErrorMessage = 'Could not recover password. Please try again.';
          });
        }
      });
  }

  /**
   * Handles image file selection and converts it to a data URL.
   */
  public onPhotoSelected(event: Event): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.photoValidationMessage = '';

    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.photoValidationMessage = 'Please select a valid image file.';
      this.selectedPhotoFileName = 'No file selected';
      input.value = '';
      return;
    }

    if (file.size > this.maxPhotoBytes) {
      this.photoValidationMessage = 'Profile photo is too large. Please choose an image up to 5 MB.';
      this.selectedPhotoFileName = 'No file selected';
      input.value = '';
      return;
    }

    this.selectedPhotoFileName = file.name;

    const reader = new FileReader();
    reader.onload = () => {
      const value = typeof reader.result === 'string' ? reader.result : '';
      this.runInAngular(() => {
        this.profileForm.controls.profilePhotoUrl.setValue(value);
        this.profilePhotoPreview = value || null;
      });
    };
    reader.readAsDataURL(file);
  }

  /**
   * Clears the selected profile photo.
   */
  public clearPhoto(): void {
    this.profileForm.controls.profilePhotoUrl.setValue('');
    this.profilePhotoPreview = null;
    this.photoValidationMessage = '';
    this.successMessage = '';
    this.selectedPhotoFileName = 'No file selected';

    if (this.profilePhotoInput?.nativeElement) {
      this.profilePhotoInput.nativeElement.value = '';
    }
  }

  /**
   * Submits profile updates.
   */
  public saveProfile(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.photoValidationMessage) {
      this.errorMessage = this.photoValidationMessage;
      return;
    }

    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;

    const payload: UpdateProfileRequest = {
      fullName: this.profileForm.controls.fullName.value.trim(),
      email: this.profileForm.controls.email.value.trim(),
      profilePhotoUrl: this.profileForm.controls.profilePhotoUrl.value.trim() || null
    };

    this.authService
      .updateProfile(payload)
      .pipe(
        finalize(() => {
          this.runInAngular(() => {
            this.isSaving = false;
          });
        })
      )
      .subscribe({
        next: (profile: UserProfile) => {
          this.runInAngular(() => {
            this.profileForm.patchValue({
              fullName: profile.fullName,
              email: profile.email,
              profilePhotoUrl: profile.profilePhotoUrl ?? ''
            });
            this.profilePhotoPreview = profile.profilePhotoUrl;
            this.photoValidationMessage = '';
            this.successMessage = 'Profile updated successfully.';
          });
        },
        error: (error: HttpErrorResponse) => {
          this.runInAngular(() => {
            if (error.status === 409) {
              this.errorMessage = 'This email is already in use by another account.';
              return;
            }

            this.errorMessage = 'Could not update your profile. Please try again.';
          });
        }
      });
  }

  private runInAngular(action: () => void): void {
    this.ngZone.run(() => {
      action();
      this.changeDetectorRef.detectChanges();
    });
  }

  private buildQrCodeImageUrl(otpAuthUri: string): string {
    if (!otpAuthUri) {
      return '';
    }

    return `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(otpAuthUri)}`;
  }
}

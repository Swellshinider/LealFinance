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

import { AuthService, UpdateProfileRequest, UserProfile } from '../../core/services/auth.service';

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

  /** Indicates profile loading in progress. */
  public isLoading = true;

  /** Indicates profile save in progress. */
  public isSaving = false;

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

  public constructor(
    private readonly authService: AuthService
  ) {}

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
      input.value = '';
      return;
    }

    if (file.size > this.maxPhotoBytes) {
      this.photoValidationMessage = 'Profile photo is too large. Please choose an image up to 5 MB.';
      input.value = '';
      return;
    }

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
}

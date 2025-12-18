import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';

// Custom validator to check if two fields match
export function passwordMatchValidator(controlName: string, matchingControlName: string) {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const control = formGroup.get(controlName);
    const matchingControl = formGroup.get(matchingControlName);

    if (matchingControl?.errors && !matchingControl.errors['passwordMismatch']) {
      // return if another validator has already found an error on the matchingControl
      return null;
    }

    if (control?.value !== matchingControl?.value) {
      matchingControl?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      matchingControl?.setErrors(null);
      return null;
    }
  };
}

@Component({
  selector: 'app-profile-page',
  templateUrl: './profile-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
})
export class ProfilePageComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private userService = inject(UserService);

  readonly currentUser = this.authService.currentUser;
  
  readonly profileStatus = signal<{ type: 'success' | 'error', message: string } | null>(null);
  readonly passwordStatus = signal<{ type: 'success' | 'error', message: string } | null>(null);

  readonly profileForm = this.fb.group({
    name: ['', Validators.required],
    email: [{ value: '', disabled: true }, [Validators.required, Validators.email]],
    role: [{ value: '', disabled: true }, Validators.required],
    mobileNumber: ['', [Validators.pattern(/^[0-9]{10}$/)]],
    addressLine1: [''],
    addressLine2: [''],
    city: [''],
    state: [''],
    zipCode: [''],
    panNumber: ['', [Validators.pattern(/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/)]]
  });

  readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordMatchValidator('newPassword', 'confirmPassword') });

  constructor() {
    effect(() => {
      const user = this.currentUser();
      if (user) {
        this.profileForm.patchValue({
          name: user.name,
          email: user.email,
          role: user.role,
          mobileNumber: user.mobileNumber,
          addressLine1: user.addressLine1,
          addressLine2: user.addressLine2,
          city: user.city,
          state: user.state,
          zipCode: user.zipCode,
          panNumber: user.panNumber,
        });
      }
    });
  }

  updateProfile() {
    if (this.profileForm.invalid) return;
    const user = this.currentUser();
    if (!user) return;
    
    const formValue = this.profileForm.value;
    
    this.userService.saveUser({ 
      ...user, 
      name: formValue.name!,
      mobileNumber: formValue.mobileNumber!,
      addressLine1: formValue.addressLine1!,
      addressLine2: formValue.addressLine2!,
      city: formValue.city!,
      state: formValue.state!,
      zipCode: formValue.zipCode!,
      panNumber: formValue.panNumber!,
    });
    
    this.profileStatus.set({ type: 'success', message: 'Profile updated successfully!' });
    this.profileForm.markAsPristine();
    setTimeout(() => this.profileStatus.set(null), 3000);
  }

  async updatePassword() {
    if (this.passwordForm.invalid) return;
    this.passwordStatus.set(null);

    const { currentPassword, newPassword } = this.passwordForm.value;
    const result = await this.authService.changePassword(currentPassword!, newPassword!);
    
    if (result.success) {
        this.passwordStatus.set({ type: 'success', message: result.message });
        this.passwordForm.reset();
    } else {
        this.passwordStatus.set({ type: 'error', message: result.message });
    }
    setTimeout(() => this.passwordStatus.set(null), 3000);
  }
}
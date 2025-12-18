import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
})
export class LoginPageComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  readonly loginError = signal<string | null>(null);
  readonly isLoading = signal(false);

  async onSubmit() {
    if (this.loginForm.invalid) {
      return;
    }
    this.isLoading.set(true);
    this.loginError.set(null);

    const { email, password } = this.loginForm.value;

    const success = await this.authService.login(email!, password!);

    if (!success) {
      this.loginError.set('Invalid email or password. Please try again.');
    }
    // On success, the app component will automatically switch views.
    this.isLoading.set(false);
  }
}

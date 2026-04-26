import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h2>Welcome Back</h2>
        <p>Login to book your next journey</p>
        
        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>Email Address</label>
            <input type="email" formControlName="email" placeholder="Enter your email">
          </div>
          
          <div class="form-group">
            <label>Password</label>
            <input type="password" formControlName="password" placeholder="Enter your password">
          </div>
          
          <div *ngIf="error" class="error-message">
            {{ error }}
          </div>
          
          <button type="submit" [disabled]="loginForm.invalid || loading">
            {{ loading ? 'Logging in...' : 'Login' }}
          </button>
        </form>
        
        <div class="auth-footer">
          Don't have an account? <a routerLink="/register">Register here</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: #fcfcfc;
      font-family: 'Inter', sans-serif;
    }
    .auth-card {
      background: white;
      padding: 3.5rem;
      border: 1px solid #eee;
      border-radius: 24px;
      box-shadow: 0 15px 45px rgba(0,0,0,0.06);
      width: 100%;
      max-width: 420px;
      text-align: center;
    }
    h2 { margin-bottom: 0.5rem; color: #000; font-weight: 900; font-size: 1.75rem; letter-spacing: -0.025em; }
    p { margin-bottom: 2.5rem; color: #888; font-size: 0.95rem; font-weight: 500; }
    .form-group { text-align: left; margin-bottom: 1.5rem; }
    label { display: block; margin-bottom: 0.5rem; font-weight: 700; color: #000; font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.05em; color: #888; }
    input {
      width: 100%;
      padding: 0.85rem 1rem;
      border: 1px solid #eee;
      border-radius: 12px;
      font-size: 0.95rem;
      font-weight: 600;
      transition: all 0.2s;
      box-sizing: border-box;
      background: #f9f9f9;
    }
    input:focus { outline: none; background: #fff; border-color: #000; }
    button {
      width: 100%;
      padding: 1rem;
      background: #000;
      color: white;
      border: none;
      border-radius: 30px;
      font-size: 0.9rem;
      font-weight: 900;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      cursor: pointer;
      transition: all 0.2s;
      margin-top: 1rem;
    }
    button:hover:not(:disabled) { background: #222; }
    button:disabled { background: #eee; color: #aaa; cursor: not-allowed; }
    .error-message { color: #ff5252; background: #fff1f1; border-radius: 12px; padding: 0.75rem 1rem; margin-bottom: 1.5rem; font-size: 0.8rem; font-weight: 700; text-align: left; border: 1px solid #ffcfcf; }
    .auth-footer { margin-top: 2rem; font-size: 0.85rem; color: #888; font-weight: 500; }
    .auth-footer a { color: #000; text-decoration: none; font-weight: 800; }
  `]
})
export class LoginComponent {
  loginForm: FormGroup;
  loading = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    // If already logged in, skip the login page with role check
    const user = this.authService.currentUser();
    if (user) {
      this.redirectUser(user);
    }

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  private redirectUser(user: any) {
    const role = user.role;
    if (role === 2 || role === 'Admin') {
      this.router.navigate(['/admin']);
    } else if (role === 1 || role === 'Operator') {
      this.router.navigate(['/operator']);
    } else {
      this.router.navigate(['/home']);
    }
  }

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.loading = true;
    this.error = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: (user) => this.redirectUser(user),
      error: (err) => {
        this.error = err.error || 'Login failed. Please check your credentials.';
        this.loading = false;
      }
    });
  }
}


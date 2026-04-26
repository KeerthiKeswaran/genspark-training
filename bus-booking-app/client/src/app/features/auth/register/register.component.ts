import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { UserRole } from '../../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h2>Create Account</h2>
        <p>Join our community of travelers</p>
        
        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          <div class="form-row">
            <div class="form-group">
              <label>Full Name</label>
              <input type="text" formControlName="fullName" placeholder="John Doe">
            </div>
            <div class="form-group">
              <label>Phone Number</label>
              <input type="text" formControlName="phone" placeholder="+1234567890">
            </div>
          </div>
          
          <div class="form-group">
            <label>Email Address</label>
            <input type="email" formControlName="email" placeholder="email@example.com">
          </div>
          
          <div class="form-group">
            <label>Password</label>
            <input type="password" formControlName="password" placeholder="Minimum 6 characters">
          </div>
          
          <div class="form-group">
            <label>I am registering as a:</label>
            <div class="role-selection">
              <label class="role-option">
                <input type="radio" formControlName="role" [value]="UserRole.Customer">
                <span>Customer</span>
              </label>
              <label class="role-option">
                <input type="radio" formControlName="role" [value]="UserRole.Operator">
                <span>Operator</span>
              </label>
              <label class="role-option">
                <input type="radio" formControlName="role" [value]="UserRole.Admin">
                <span>Admin</span>
              </label>
            </div>
          </div>

          <div class="operator-details" *ngIf="registerForm.get('role')?.value === UserRole.Operator">
            <div class="form-group">
              <label>Company Name</label>
              <input type="text" formControlName="companyName" placeholder="Travels Pvt Ltd">
            </div>
            <div class="form-group">
              <label>Business Address</label>
              <input type="text" formControlName="address" placeholder="123, Business Hub, City">
            </div>
          </div>
          
          <div *ngIf="error" class="error-message">
            {{ error }}
          </div>
          
          <button type="submit" [disabled]="registerForm.invalid || loading">
            {{ loading ? 'Creating account...' : 'Register' }}
          </button>
        </form>
        
        <div class="auth-footer">
          Already have an account? <a routerLink="/login">Login here</a>
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
      padding: 1rem;
    }
    .auth-card {
      background: white;
      padding: 3.5rem;
      border: 1px solid #eee;
      border-radius: 24px;
      box-shadow: 0 15px 45px rgba(0,0,0,0.06);
      width: 100%;
      max-width: 520px;
      text-align: center;
    }
    h2 { margin-bottom: 0.5rem; color: #000; font-weight: 900; font-size: 1.75rem; letter-spacing: -0.025em; }
    p { margin-bottom: 2rem; color: #888; font-size: 0.95rem; font-weight: 500; }
    .form-row { display: flex; gap: 1rem; }
    .form-group { text-align: left; margin-bottom: 1.2rem; flex: 1; }
    label { display: block; margin-bottom: 0.4rem; font-weight: 700; color: #888; font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.05em; }
    input[type="text"], input[type="email"], input[type="password"] {
      width: 100%;
      padding: 0.8rem 1rem;
      border: 1px solid #eee;
      border-radius: 12px;
      font-size: 0.95rem;
      font-weight: 600;
      box-sizing: border-box;
      background: #f9f9f9;
      transition: all 0.2s;
    }
    input:focus { outline: none; background: #fff; border-color: #000; }
    .role-selection {
      display: flex;
      justify-content: space-around;
      background: #f9f9f9;
      padding: 0.75rem;
      border-radius: 12px;
      border: 1px solid #eee;
      margin-top: 0.5rem;
    }
    .role-option {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      cursor: pointer;
      font-weight: 700;
      font-size: 0.7rem;
      text-transform: uppercase;
      color: #666;
    }
    .role-option input {
       accent-color: #000;
    }
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
      margin-top: 1rem;
      transition: all 0.2s;
    }
    button:hover:not(:disabled) { background: #222; }
    button:disabled { background: #eee; color: #aaa; cursor: not-allowed; }
    .error-message { color: #ff5252; background: #fff1f1; border-radius: 12px; padding: 0.75rem 1rem; margin-bottom: 1.5rem; font-size: 0.8rem; font-weight: 700; text-align: left; border: 1px solid #ffcfcf; }
    .auth-footer { margin-top: 1.5rem; font-size: 0.85rem; color: #888; font-weight: 500; }
    .auth-footer a { color: #000; text-decoration: none; font-weight: 800; }

    .operator-details {
      background: #f8fafc;
      padding: 1rem;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      margin-top: 1rem;
      margin-bottom: 1.5rem;
      animation: fadeIn 0.3s ease-out;
      text-align: left;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class RegisterComponent {
  registerForm: FormGroup;
  loading = false;
  error = '';
  UserRole = UserRole;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      role: [UserRole.Customer, [Validators.required]],
      companyName: [''],
      address: ['']
    });
  }

  onSubmit() {
    if (this.registerForm.invalid) return;

    const requestData = { ...this.registerForm.value };
    if (requestData.role !== UserRole.Operator) {
      delete requestData.companyName;
      delete requestData.address;
    }

    this.authService.register(requestData).subscribe({
      next: () => {
        this.loading = false;
        alert('Registration successful! Please login.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.error = err.error || 'Registration failed. Please try again.';
        this.loading = false;
      }
    });
  }
}

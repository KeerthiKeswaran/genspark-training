import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class RegisterComponent implements OnInit {
  public name = '';
  public email = '';
  public mobileNumber = '';
  public password = '';
  public otp = '';
  
  public isOtpSent = signal(false);
  public errorMessage = signal<string | null>(null);
  public successMessage = signal<string | null>(null);
  public isSubmitting = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {}

  public sendOtp(): void {
    if (!this.email) {
      this.errorMessage.set('Please enter your email to receive an OTP.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.sendOtp(this.email, 'registration').subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isOtpSent.set(true);
        this.successMessage.set('OTP has been sent to your email.');
        setTimeout(() => this.successMessage.set(null), 5000);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send OTP. Please check your email.');
      }
    });
  }

  public onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.name || !this.email || !this.mobileNumber || !this.password || !this.otp) {
      this.errorMessage.set('All fields are required.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const payload = {
      name: this.name,
      email: this.email,
      mobileNumber: this.mobileNumber,
      password: this.password,
      consentedTermsId: 10000,
      hasMarketingConsent: true,
      otp: this.otp
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Registration failed. Please check details or OTP.');
      }
    });
  }

}

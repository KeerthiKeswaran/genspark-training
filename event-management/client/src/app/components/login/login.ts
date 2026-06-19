import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent implements OnInit {
  public email = '';
  public password = '';
  public errorMessage = signal<string | null>(null);
  public isSubmitting = signal(false);
  public showPassword = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {}

  public togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  public onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.email || !this.password) {
      this.errorMessage.set('Please provide both email and password.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        // Navigate back to home
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Invalid email or password. Please try again.');
      }
    });
  }
}

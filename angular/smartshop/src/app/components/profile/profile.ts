import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-profile',
  imports: [AsyncPipe],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Expose current user observable to template
  protected readonly currentUser$ = this.authService.currentUser$;

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}

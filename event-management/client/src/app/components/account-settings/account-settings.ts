import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { AppStoreService } from '../../store/app-store.service';
import { ActionTypes } from '../../store/actions/app.actions';
import { UserModel } from '../../models/user.model';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.css'
})
export class AccountSettingsComponent implements OnInit, OnDestroy {
  private store = inject(AppStoreService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private subscriptions = new Subscription();

  // Active Tab
  public activeTab = signal<'profile' | 'password' | 'close'>('profile');

  // User Profile State
  public currentUser: UserModel | null = null;
  public profileName = '';
  public profileEmail = '';
  public profilePhone = '';

  // Profile Edit states
  public isEditingProfile = signal(false);
  public isSavingProfile = signal(false);
  public profileSuccessMessage = signal<string | null>(null);
  public profileErrorMessage = signal<string | null>(null);

  // Password reset flow states
  public showPasswordOtpModal = signal(false);
  public showNewPasswordModal = signal(false);
  public passwordOtpCode = '';
  
  public newPassword = '';
  public confirmPassword = '';
  
  public isRequestingPasswordOtp = signal(false);
  public isVerifyingPasswordOtp = signal(false);
  public isSubmittingNewPassword = signal(false);
  
  public passwordSuccessMessage = signal<string | null>(null);
  public passwordErrorMessage = signal<string | null>(null);
  public passwordOtpError = signal<string | null>(null);
  public passwordResetError = signal<string | null>(null);

  // Close Account States
  public selectedReason = '';
  public otherReasonExplanation = '';
  public typedConfirmName = '';
  public otpCode = '';
  
  public isOtpSent = signal(false);
  public isSubmittingClosure = signal(false);
  public closureErrorMessage = signal<string | null>(null);
  
  // Final Thanking screen state
  public isAccountClosed = signal(false);

  // Available closure reasons
  public closureReasons = [
    { value: 'no_longer_using', label: 'I am no longer using this platform' },
    { value: 'privacy_concerns', label: 'I have privacy or data storage concerns' },
    { value: 'too_many_emails', label: 'I receive too many promotional emails' },
    { value: 'better_alternative', label: 'I found a better alternative event platform' },
    { value: 'other', label: 'Other (Please specify below)' }
  ];

  ngOnInit(): void {
    // Redirect if not logged in
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(isLoggedIn => {
        if (!isLoggedIn && !this.isAccountClosed()) {
          this.router.navigate(['/login']);
        }
      })
    );

    // Watch user profile
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => {
        if (user) {
          this.currentUser = user;
          this.profileName = user.name;
          this.profileEmail = user.email;
          this.profilePhone = user.mobile_Number || '';
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  // 1. Update Profile - Enable/Disable Editing Mode
  public enableEditProfile(): void {
    this.isEditingProfile.set(true);
    this.profileSuccessMessage.set(null);
    this.profileErrorMessage.set(null);
  }

  public cancelEditProfile(): void {
    this.isEditingProfile.set(false);
    if (this.currentUser) {
      this.profileName = this.currentUser.name;
      this.profileEmail = this.currentUser.email;
      this.profilePhone = this.currentUser.mobile_Number || '';
    }
    this.profileErrorMessage.set(null);
  }

  public updateProfile(): void {
    if (!this.profileName.trim() || !this.profileEmail.trim()) {
      this.profileErrorMessage.set('Name and Email are required.');
      return;
    }

    this.isSavingProfile.set(true);
    this.profileSuccessMessage.set(null);
    this.profileErrorMessage.set(null);

    // Commented out server HTTP call:
    /*
    this.http.put('http://localhost:5106/api/user/profile', {
      name: this.profileName,
      mobileNumber: this.profilePhone
    }).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.isEditingProfile.set(false);
        this.profileSuccessMessage.set('Profile details updated successfully.');
        
        // Dispatch to update global state
        const updatedUser = {
          ...this.currentUser,
          name: this.profileName,
          email: this.profileEmail,
          mobile_Number: this.profilePhone
        };
        this.store.dispatch({
          type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
          payload: updatedUser
        });

        setTimeout(() => this.profileSuccessMessage.set(null), 4000);
      },
      error: () => {
        this.isSavingProfile.set(false);
        this.profileErrorMessage.set('Failed to save profile. Please try again.');
      }
    });
    */

    // MOCK Profile API call
    of({
      ...this.currentUser,
      name: this.profileName,
      email: this.profileEmail,
      mobile_Number: this.profilePhone
    }).pipe(delay(800)).subscribe({
      next: (updatedUser) => {
        this.isSavingProfile.set(false);
        this.isEditingProfile.set(false);
        this.profileSuccessMessage.set('Profile details updated successfully.');
        
        // Dispatch to update global state
        this.store.dispatch({
          type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
          payload: updatedUser
        });

        setTimeout(() => this.profileSuccessMessage.set(null), 4000);
      },
      error: () => {
        this.isSavingProfile.set(false);
        this.profileErrorMessage.set('Failed to save profile. Please try again.');
      }
    });
  }

  // 2. Password Reset Flow:
  // Step A: Trigger Password Change and send OTP
  public requestPasswordChange(): void {
    this.isRequestingPasswordOtp.set(true);
    this.passwordErrorMessage.set(null);
    this.passwordOtpError.set(null);
    
    // COMMENTED OUT: Actual API Call to request Password Reset OTP code:
    /*
    this.http.post('/api/auth/password-reset-otp', { email: this.currentUser?.email }).subscribe({
      next: () => {
        this.isRequestingPasswordOtp.set(false);
        this.showPasswordOtpModal.set(true);
      },
      error: (err) => {
        this.isRequestingPasswordOtp.set(false);
        this.passwordErrorMessage.set('Failed to request verification code. Please try again.');
      }
    });
    */

    // Simulate requesting OTP success
    of({ success: true }).pipe(delay(800)).subscribe(() => {
      this.isRequestingPasswordOtp.set(false);
      this.showPasswordOtpModal.set(true);
      this.passwordOtpCode = '';
    });
  }

  // Step B: Verify the verification OTP
  public verifyPasswordOtp(): void {
    if (!this.passwordOtpCode || this.passwordOtpCode.length !== 6) {
      this.passwordOtpError.set('Please enter a valid 6-digit verification code.');
      return;
    }

    this.isVerifyingPasswordOtp.set(true);
    this.passwordOtpError.set(null);

    // COMMENTED OUT: Actual API Call to verify Password Reset OTP code:
    /*
    this.http.post('/api/auth/verify-password-otp', { email: this.currentUser?.email, otp: this.passwordOtpCode }).subscribe({
      next: () => {
        this.isVerifyingPasswordOtp.set(false);
        this.showPasswordOtpModal.set(false);
        this.showNewPasswordModal.set(true);
      },
      error: (err) => {
        this.isVerifyingPasswordOtp.set(false);
        this.passwordOtpError.set('Invalid OTP code. Verification failed.');
      }
    });
    */

    // Simulate OTP validation success
    of({ success: true }).pipe(delay(900)).subscribe(() => {
      this.isVerifyingPasswordOtp.set(false);
      this.showPasswordOtpModal.set(false);
      this.showNewPasswordModal.set(true);
      
      this.newPassword = '';
      this.confirmPassword = '';
      this.passwordResetError.set(null);
    });
  }

  // Step C: Submit New Password & Reset Password
  public submitNewPassword(): void {
    if (!this.newPassword || !this.confirmPassword) {
      this.passwordResetError.set('Please fill in all password fields.');
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.passwordResetError.set('Passwords do not match.');
      return;
    }

    if (this.newPassword.length < 6) {
      this.passwordResetError.set('Password must be at least 6 characters.');
      return;
    }

    this.isSubmittingNewPassword.set(true);
    this.passwordResetError.set(null);

    // COMMENTED OUT: Actual API Call to change password:
    /*
    this.http.post('/api/auth/change-password', { newPassword: this.newPassword }).subscribe({
      next: () => {
        this.isSubmittingNewPassword.set(false);
        this.showNewPasswordModal.set(false);
        this.passwordSuccessMessage.set('Your password has been changed successfully.');
        setTimeout(() => this.passwordSuccessMessage.set(null), 4000);
      },
      error: () => {
        this.isSubmittingNewPassword.set(false);
        this.passwordResetError.set('Failed to update password. Please try again.');
      }
    });
    */

    // Simulate successful password update
    of({ success: true }).pipe(delay(1000)).subscribe(() => {
      this.isSubmittingNewPassword.set(false);
      this.showNewPasswordModal.set(false);
      this.passwordSuccessMessage.set('Your password has been changed successfully.');
      setTimeout(() => this.passwordSuccessMessage.set(null), 4000);
    });
  }

  public closePasswordOtpModal(): void {
    this.showPasswordOtpModal.set(false);
    this.passwordOtpCode = '';
    this.passwordOtpError.set(null);
  }

  public closeNewPasswordModal(): void {
    this.showNewPasswordModal.set(false);
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordResetError.set(null);
  }

  // 3. Close Account - Step 1: Send OTP
  public requestAccountClosure(): void {
    if (!this.selectedReason) {
      this.closureErrorMessage.set('Please select a reason for closing your account.');
      return;
    }

    if (this.selectedReason === 'other' && !this.otherReasonExplanation.trim()) {
      this.closureErrorMessage.set('Please explain your reason in the text box.');
      return;
    }

    if (this.typedConfirmName.trim() !== this.currentUser?.name) {
      this.closureErrorMessage.set('Verification Name does not match your profile name.');
      return;
    }

    this.isSubmittingClosure.set(true);
    this.closureErrorMessage.set(null);

    // MOCK OTP send logic (matching register flow behaviour)
    of({ success: true }).pipe(delay(1000)).subscribe({
      next: () => {
        this.isSubmittingClosure.set(false);
        this.isOtpSent.set(true);
      },
      error: () => {
        this.isSubmittingClosure.set(false);
        this.closureErrorMessage.set('An error occurred. Please try again.');
      }
    });
  }

  // 3. Close Account - Step 2: Confirm OTP & Close
  public confirmAccountClosure(): void {
    if (!this.otpCode || this.otpCode.length !== 6) {
      this.closureErrorMessage.set('Please enter a valid 6-digit OTP code.');
      return;
    }

    this.isSubmittingClosure.set(true);
    this.closureErrorMessage.set(null);

    // COMMENTED OUT: Actual API Call to backend for closure with OTP confirmation:
    /*
    const closurePayload = {
      reason: this.selectedReason,
      explanation: this.selectedReason === 'other' ? this.otherReasonExplanation : '',
      confirmName: this.typedConfirmName,
      otp: this.otpCode
    };
    this.http.post('/api/user/close-account', closurePayload).subscribe({
      next: () => {
        // success handler
      },
      error: (err) => {
        // error handler
      }
    });
    */

    // Simulate OTP Verification Success
    of({ success: true }).pipe(delay(1200)).subscribe({
      next: () => {
        this.isSubmittingClosure.set(false);
        this.isAccountClosed.set(true);

        // Clear local storage completely
        if (typeof window !== 'undefined') {
          localStorage.clear();
        }

        // Dispatch logout action to clear global store state
        this.store.dispatch({ type: ActionTypes.LOGOUT });
      },
      error: () => {
        this.isSubmittingClosure.set(false);
        this.closureErrorMessage.set('Invalid OTP. Please try again.');
      }
    });
  }

  // Cancel OTP verification and go back to reason select
  public cancelClosureOtp(): void {
    this.isOtpSent.set(false);
    this.otpCode = '';
    this.closureErrorMessage.set(null);
  }

  public navigateHome(): void {
    this.router.navigate(['/']);
  }
}

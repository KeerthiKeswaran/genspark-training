import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, throwError } from 'rxjs';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { UserModel, AuthResponse } from '../models/user.model';
import { mockUser, mockLoginResponse, mockRegisterResponse } from '../data/auth.mock';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = 'http://localhost:5106/api';

  constructor(
    private http: HttpClient,
    private store: AppStoreService
  ) {
    // If token exists, load profile automatically on startup
    const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
    if (token) {
      this.loadProfile().subscribe();
    }
  }

  public login(email: string, password: string): Observable<AuthResponse> {
    this.store.dispatch({ type: ActionTypes.LOGIN_START });
    
    // Commented out server HTTP call:
    /*
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/user/login`, { email, password }).pipe(
      tap((res) => {
        if (res.token) {
          localStorage.setItem('token', res.token);
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: res.token, user: null }
          });
          this.loadProfile().subscribe();
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Login failed. Please check your credentials.';
        this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: errMsg });
        return throwError(() => err);
      })
    );
    */

    // Simulated Mock Login logic:
    if (email === 'keshwarankeerthi@gmail.com' && password === 'SecurePassword123!') {
      if (typeof window !== 'undefined') {
        localStorage.setItem('token', mockLoginResponse.token);
      }
      this.store.dispatch({
        type: ActionTypes.LOGIN_SUCCESS,
        payload: { token: mockLoginResponse.token, user: null }
      });
      
      // Auto-load mock profile
      this.loadProfile().subscribe();
      return of(mockLoginResponse);
    } else {
      const mockError = { error: { message: 'Invalid email or password. Please check your credentials.' } };
      this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: mockError.error.message });
      return throwError(() => mockError);
    }
  }

  public register(payload: any): Observable<AuthResponse> {
    this.store.dispatch({ type: ActionTypes.LOGIN_START });
    
    // Commented out server HTTP call:
    /*
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/user/register`, payload).pipe(
      tap((res) => {
        if (res.token) {
          localStorage.setItem('token', res.token);
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: res.token, user: null }
          });
          this.loadProfile().subscribe();
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Registration failed.';
        this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: errMsg });
        return throwError(() => err);
      })
    );
    */

    // Simulated Mock Register logic:
    if (typeof window !== 'undefined') {
      localStorage.setItem('token', mockRegisterResponse.token);
    }
    this.store.dispatch({
      type: ActionTypes.LOGIN_SUCCESS,
      payload: { token: mockRegisterResponse.token, user: null }
    });
    this.loadProfile().subscribe();
    return of(mockRegisterResponse);
  }

  public sendOtp(email: string, purpose: string): Observable<any> {
    // Commented out server HTTP call:
    /*
    return this.http.post(`${this.baseUrl}/auth/user/send-otp`, { email, purpose });
    */

    // Simulated Mock response:
    return of({ message: 'OTP sent successfully.' });
  }

  public resetPassword(payload: any): Observable<any> {
    // Commented out server HTTP call:
    /*
    return this.http.post(`${this.baseUrl}/auth/user/reset-password`, payload);
    */

    // Simulated Mock response:
    return of({ message: 'Password reset successfully.' });
  }

  public loadProfile(): Observable<UserModel> {
    this.store.dispatch({ type: ActionTypes.LOAD_USER_PROFILE });
    
    // Commented out server HTTP call:
    /*
    return this.http.get<UserModel>(`${this.baseUrl}/user/profile`).pipe(
      tap((user) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
          payload: user
        });
        if (user.interested_Region_Id) {
          this.store.dispatch({
            type: ActionTypes.SET_REGION,
            payload: user.interested_Region_Id
          });
          localStorage.setItem('currentRegionId', user.interested_Region_Id);
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Failed to load user profile.';
        this.store.dispatch({ type: ActionTypes.LOAD_USER_PROFILE_FAIL, payload: errMsg });
        this.logout();
        return throwError(() => err);
      })
    );
    */

    // Simulated Mock profile data:
    const activeUser: UserModel = {
      ...mockUser,
      interested_Region_Id: (typeof window !== 'undefined' ? localStorage.getItem('currentRegionId') : null) || 'REG01'
    };

    this.store.dispatch({
      type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
      payload: activeUser
    });
    
    return of(activeUser);
  }

  public selectRegion(regionId: string): Observable<any> {
    // Commented out server HTTP call:
    /*
    return this.http.post(`${this.baseUrl}/user/select-regions`, { regionId }).pipe(...);
    */

    // Simulated Mock response:
    this.store.dispatch({
      type: ActionTypes.SET_REGION,
      payload: regionId
    });
    if (typeof window !== 'undefined') {
      localStorage.setItem('currentRegionId', regionId);
    }
    
    const user = this.store.state.auth.user;
    if (user) {
      this.store.dispatch({
        type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
        payload: { ...user, interested_Region_Id: regionId }
      });
    }

    return of({ message: 'Interested regions updated successfully.' });
  }

  public logout(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('token');
    }
    this.store.dispatch({ type: ActionTypes.LOGOUT });
  }
}

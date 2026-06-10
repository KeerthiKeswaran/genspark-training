import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { User } from '../interfaces/user.interface';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  
  // BehaviorSubject to maintain logged-in user state
  private readonly currentUserSubject = new BehaviorSubject<User | null>(this.getStoredUser());
  
  // Observable for components to subscribe/display user data
  public readonly currentUser$: Observable<User | null> = this.currentUserSubject.asObservable();

  private getStoredUser(): User | null {
    const stored = localStorage.getItem('currentUser');
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch {
        return null;
      }
    }
    return null;
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  public isLoggedIn(): boolean {
    return this.currentUserValue !== null;
  }

  login(username: string, password: string): Observable<User> {
    return this.http.post<User>('https://dummyjson.com/auth/login', {
      username,
      password
    }).pipe(
      tap((user: User) => {
        // Store user in local storage to keep user logged in between page refreshes
        localStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUserSubject.next(user);
      }),
      catchError((error) => {
        console.error('Login request failed', error);
        return throwError(() => new Error(error.error?.message || 'Authentication failed. Please check your credentials.'));
      })
    );
  }

  logout(): void {
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }
}

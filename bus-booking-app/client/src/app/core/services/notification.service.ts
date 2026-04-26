import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface Notification {
  id: string;
  title: string;
  message: string;
  createdAt: string;
  isRead: boolean;
  type: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = 'http://localhost:5171/api/Notifications';
  private http = inject(HttpClient);
  
  notifications = signal<Notification[]>([]);
  unreadCount = signal(0);

  loadNotifications(userId: string) {
    this.http.get<Notification[]>(`${this.apiUrl}/user/${userId}`).subscribe(data => {
      this.notifications.set(data);
      this.unreadCount.set(data.filter(n => !n.isRead).length);
    });
  }

  markAsRead(notificationId: string) {
    this.http.put(`${this.apiUrl}/${notificationId}/read`, {}).subscribe(() => {
      this.notifications.update(list => 
        list.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
      );
      this.unreadCount.update(c => Math.max(0, c - 1));
    });
  }

  clearAll(userId: string) {
    this.http.delete(`${this.apiUrl}/user/${userId}/clear`).subscribe(() => {
      this.notifications.set([]);
      this.unreadCount.set(0);
    });
  }
}

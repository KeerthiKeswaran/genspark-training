import { Injectable, signal } from '@angular/core';

export interface Toast {
  message: string;
  type: 'success' | 'error' | 'info';
  visible: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toast = signal<Toast>({ message: '', type: 'info', visible: false });

  show(message: string, type: 'success' | 'error' | 'info' = 'success') {
    this.toast.set({ message, type, visible: true });
    setTimeout(() => {
      this.toast.set({ ...this.toast(), visible: false });
    }, 5000);
  }
}

import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, FeeSetting } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-fee-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="loading-bar" *ngIf="isLoading"></div>
    <div class="header">
      <h1>Platform Settings</h1>
      <p>Configure global platform settings, such as convenience fees.</p>
    </div>

    <div class="card" [class.loading-fade]="isLoading">
      <h2>Platform Convenience Fee</h2>
      <div class="form-group">
        <label>Fee Type</label>
        <select [(ngModel)]="settings.feeType" class="input" [disabled]="isLoading">
          <option value="Fixed">Fixed Amount (₹)</option>
          <option value="Percentage">Percentage (%)</option>
        </select>
      </div>
      
      <div class="form-group">
        <label>Fee Value</label>
        <div class="input-group">
          <span class="prefix">{{ settings.feeType === 'Percentage' ? '%' : '₹' }}</span>
          <input type="number" [(ngModel)]="settings.feeValue" class="input flex-1" [disabled]="isLoading" />
        </div>
      </div>

      <div class="form-group">
        <label>Operator Commission (%)</label>
        <div class="input-group">
          <span class="prefix">%</span>
          <input type="number" [(ngModel)]="settings.commissionPercentage" class="input flex-1" step="0.1" [disabled]="isLoading" />
        </div>
        <p class="hint">Percentage of base ticket price deducted from operator payout.</p>
      </div>

      <button (click)="saveSettings()" class="btn-primary" [disabled]="isLoading">
        {{ isLoading ? 'Saving...' : 'Save Settings' }}
      </button>
    </div>
  `,
  styles: [`
    .loading-bar { height: 4px; background: #2563eb; position: fixed; top: 0; left: 0; width: 100%; animation: pulse 2s infinite; z-index: 1000; }
    @keyframes pulse { 0% { opacity: 0.3; } 50% { opacity: 1; } 100% { opacity: 0.3; } }
    .loading-fade { opacity: 0.7; pointer-events: none; }
    
    .header h1 { font-size: 1.8rem; margin: 0 0 0.5rem 0; color: #0f172a; }
    .header p { color: #64748b; margin-top: 0; margin-bottom: 2rem; }
    .card { background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); max-width: 500px; transition: opacity 0.3s ease; }
    .card h2 { font-size: 1.2rem; margin-top: 0; margin-bottom: 1.5rem; }
    .form-group { margin-bottom: 1.5rem; }
    .form-group label { display: block; font-weight: 600; margin-bottom: 0.5rem; color: #475569; font-size: 0.9rem; }
    .input { width: 100%; padding: 0.75rem; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 1rem; box-sizing: border-box; }
    .input-group { display: flex; align-items: center; border: 1px solid #cbd5e1; border-radius: 8px; overflow: hidden; }
    .input-group .prefix { background: #f1f5f9; padding: 0.75rem 1rem; color: #64748b; font-weight: 600; border-right: 1px solid #cbd5e1; }
    .input-group .input { border: none; border-radius: 0; }
    .flex-1 { flex: 1; }
    .btn-primary { background: #2563eb; color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 8px; cursor: pointer; font-weight: 600; width: 100%; font-size: 1rem; }
    .btn-primary:hover { background: #1d4ed8; }
    .btn-primary:disabled { background: #94a3b8; cursor: not-allowed; }
    .hint { font-size: 0.8rem; color: #64748b; margin-top: 0.5rem; }
  `]
})
export default class FeeSettingsComponent implements OnInit {
  adminService = inject(AdminService);
  toastService = inject(ToastService);
  cdr = inject(ChangeDetectorRef);
  
  isLoading = false;
  settings: FeeSetting = {
    feeType: 'Fixed',
    feeValue: 50.00,
    commissionPercentage: 10.00
  };

  ngOnInit() {
    this.isLoading = true;
    this.adminService.getFeeSettings().subscribe({
      next: (data) => {
        if (data) {
          this.settings = data;
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  saveSettings() {
    this.isLoading = true;
    this.adminService.updateFeeSettings(this.settings).subscribe({
      next: () => {
        this.toastService.show('Platform fee settings updated successfully.');
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toastService.show('Failed to update settings. Please check your inputs.', 'error');
        console.error('Save error:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
}

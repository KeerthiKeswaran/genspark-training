import { Component, OnInit, inject, ChangeDetectorRef, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AdminService, Operator } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-operator-approvals',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="header">
      <h1>Platform Command Center</h1>
      <p>Manage pending registrations and fleet authorizations from a single unified queue.</p>
    </div>

    <div class="card">
      <div class="section-header">
        <div class="stats">
          <div class="stat-group">
            <span class="stat-label">Total Requests</span>
            <span class="stat-value">{{ combinedApprovals().length }}</span>
          </div>
          <span class="refresh-indicator" [class.syncing]="isLoading()">
            <span class="dot"></span> {{ isLoading() ? 'Syncing...' : 'Live' }}
          </span>
        </div>
        
        <div class="pagination-controls" *ngIf="totalPages > 1">
          <button (click)="prevPage()" [disabled]="currentPage === 1" class="page-btn">←</button>
          <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
          <button (click)="nextPage()" [disabled]="currentPage === totalPages" class="page-btn">→</button>
        </div>
      </div>

      <div class="table-responsive">
        <table class="table">
          <thead>
            <tr>
              <th>Category</th>
              <th>Primary Details</th>
              <th>Reference Context</th>
              <th>Current Status</th>
              <th class="text-right">Management Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of paginatedApprovals" class="approval-row">
              <td class="type-cell">
                <div class="type-wrapper" [class]="item.type.toLowerCase()">
                  <span class="icon">{{ item.type === 'Operator' ? '🏢' : '🚌' }}</span>
                  <span class="type-text">{{ item.type }}</span>
                </div>
              </td>
              <td>
                <ng-container *ngIf="item.type === 'Operator'">
                  <div class="primary-text">{{ item.companyName || item.CompanyName }}</div>
                  <div class="secondary-text">{{ item.email || item.Email }}</div>
                </ng-container>
                <ng-container *ngIf="item.type === 'Bus'">
                  <div class="primary-text">{{ item.busNumber || item.BusNumber }}</div>
                  <div class="secondary-text">{{ item.busType || item.BusType }} • {{ item.totalSeats || item.TotalSeats }} seats</div>
                </ng-container>
              </td>
              <td>
                <ng-container *ngIf="item.type === 'Operator'">
                  <div class="context-info">
                    <span>✉️ {{ item.email || item.Email }}</span>
                    <span>📞 {{ item.phone || item.Phone }}</span>
                  </div>
                </ng-container>
                <ng-container *ngIf="item.type === 'Bus'">
                  <div class="primary-text">{{ item.companyName || item.CompanyName }}</div>
                  <div class="secondary-text">Owner: {{ item.operatorName || item.OperatorName }}</div>
                </ng-container>
              </td>
              <td>
                <div class="status-container">
                  <span class="status-pill" [class]="getStatusClass(item)">
                    {{ getStatusText(item) }}
                  </span>
                  <div class="reason-tooltip" *ngIf="item.status === 'Rejected' || item.status === 2 || item.rejectionReason || item.RejectionReason">
                     "{{ item.rejectionReason || item.RejectionReason || 'No reason provided' }}"
                  </div>
                </div>
              </td>
              <td class="text-right">
                <div class="action-group">
                  <ng-container *ngIf="isPending(item)">
                    <button (click)="approve(item)" class="btn-action approve">Approve</button>
                    <button (click)="deny(item)" class="btn-action deny">Deny</button>
                  </ng-container>
                  
                  <ng-container *ngIf="isApproved(item)">
                    <button *ngIf="item.type === 'Operator'" (click)="deactivate(item.id)" class="btn-action deactivate">Deactivate</button>
                  </ng-container>
                  
                  <ng-container *ngIf="isRejected(item)">
                    <span class="status-pill rejected">Denied</span>
                  </ng-container>

                  <ng-container *ngIf="isDeactivated(item)">
                    <button (click)="activate(item)" class="btn-action activate">Activate</button>
                  </ng-container>
                </div>
              </td>
            </tr>
            <tr *ngIf="combinedApprovals().length === 0">
              <td colspan="5" class="empty-state">
                <div class="empty-content">
                  <span class="empty-icon">✅</span>
                  <p>All registration requests have been processed.</p>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .header h1 { font-size: 2rem; font-weight: 800; margin: 0 0 0.5rem 0; color: #0f172a; letter-spacing: -0.025em; }
    .header p { color: #64748b; font-size: 1.1rem; margin-bottom: 2.5rem; }
    
    .card { background: white; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.05); overflow: hidden; }
    .section-header { display: flex; justify-content: space-between; align-items: center; padding: 1.5rem; background: #f8fafc; border-bottom: 1px solid #e2e8f0; }
    
    .stats { display: flex; align-items: center; gap: 1.5rem; }
    .stat-group { display: flex; flex-direction: column; }
    .stat-label { font-size: 0.75rem; text-transform: uppercase; font-weight: 700; color: #94a3b8; letter-spacing: 0.05em; }
    .stat-value { font-size: 1.25rem; font-weight: 800; color: #1e293b; }

    .refresh-indicator { font-size: 0.85rem; font-weight: 600; color: #22c55e; display: flex; align-items: center; gap: 0.5rem; padding: 0.4rem 0.75rem; background: #f0fdf4; border-radius: 20px; }
    .refresh-indicator .dot { width: 8px; height: 8px; background: currentColor; border-radius: 50%; }
    .refresh-indicator.syncing { color: #3b82f6; background: #eff6ff; }
    .refresh-indicator.syncing .dot { animation: pulse 1.5s infinite; }
    @keyframes pulse { 0% { transform: scale(0.95); opacity: 0.5; } 50% { transform: scale(1.1); opacity: 1; } 100% { transform: scale(0.95); opacity: 0.5; } }

    .pagination-controls { display: flex; align-items: center; gap: 1.25rem; }
    .page-btn { background: white; border: 1px solid #cbd5e1; width: 32px; height: 32px; border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: center; font-weight: 800; transition: all 0.2s; }
    .page-btn:hover:not(:disabled) { border-color: #3b82f6; color: #3b82f6; transform: translateY(-1px); }
    .page-btn:disabled { opacity: 0.3; cursor: not-allowed; }
    .page-info { font-size: 0.85rem; font-weight: 700; color: #475569; min-width: 90px; text-align: center; }

    .table { width: 100%; border-collapse: collapse; }
    .table th { padding: 1rem 1.5rem; text-align: left; background: white; font-size: 0.75rem; text-transform: uppercase; font-weight: 700; color: #64748b; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0; }
    .table td { padding: 1.25rem 1.5rem; border-bottom: 1px solid #f1f5f9; vertical-align: middle; }
    .approval-row:hover { background: #f8fafc; }

    .type-wrapper { display: flex; align-items: center; gap: 0.75rem; padding: 0.4rem 0.8rem; border-radius: 10px; width: fit-content; }
    .type-wrapper.operator { background: #eff6ff; color: #1d4ed8; }
    .type-wrapper.bus { background: #fff7ed; color: #c2410c; }
    .type-wrapper .icon { font-size: 1.2rem; }
    .type-text { font-size: 0.8rem; font-weight: 700; }

    .primary-text { font-size: 0.95rem; font-weight: 700; color: #1e293b; }
    .secondary-text { font-size: 0.85rem; color: #64748b; margin-top: 0.1rem; }
    .context-info { display: flex; flex-direction: column; gap: 0.2rem; font-size: 0.85rem; color: #475569; }

    .status-pill { padding: 0.3rem 0.75rem; border-radius: 20px; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.025em; }
    .status-pill.approved { background: #dcfce7; color: #15803d; }
    .status-pill.pending { background: #fef9c3; color: #a16207; }
    .status-pill.pending-re-review { background: #ffedd5; color: #9a3412; border: 1px solid #fed7aa; }
    .status-pill.rejected { background: #fee2e2; color: #b91c1c; }
    .status-pill.deactivated { background: #f1f5f9; color: #475569; }
    .reason-tooltip { font-size: 0.75rem; color: #ef4444; font-style: italic; margin-top: 0.4rem; max-width: 180px; }

    .action-group { display: flex; gap: 0.6rem; justify-content: flex-end; align-items: center; }
    .text-right { text-align: right; }

    .btn-action { padding: 0.5rem 1rem; border-radius: 8px; font-weight: 700; font-size: 0.8rem; cursor: pointer; transition: all 0.2s; border: none; }
    .btn-action.approve { background: #2563eb; color: white; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2); }
    .btn-action.approve:hover { background: #1d4ed8; transform: translateY(-1px); }
    .btn-action.deny { background: white; color: #ef4444; border: 1px solid #fecaca; }
    .btn-action.deny:hover { background: #fef2f2; }
    .btn-action.deactivate { background: #fee2e2; color: #b91c1c; font-size: 0.75rem; }
    .btn-action.activate { background: #dcfce7; color: #15803d; border: 1px solid #86efac; }
    .btn-action.activate:hover { background: #bbf7d0; }
    
    .action-complete { font-size: 0.8rem; font-weight: 700; color: #16a34a; margin-right: 0.5rem; }

    .empty-state { padding: 5rem !important; text-align: center; }
    .empty-icon { font-size: 3rem; display: block; margin-bottom: 1rem; }
    .empty-content p { color: #64748b; font-weight: 600; }
  `]
})
export default class OperatorApprovalsComponent implements OnInit, OnDestroy {
  adminService = inject(AdminService);
  toastService = inject(ToastService);
  cdr = inject(ChangeDetectorRef);
  
  combinedApprovals = signal<any[]>([]);
  isLoading = signal(false);
  refreshInterval: any;
  
  // Pagination
  currentPage = 1;
  pageSize = 10;

  ngOnInit() {
    // Small timeout to ensure component is fully ready
    setTimeout(() => this.loadData(), 100);
    this.refreshInterval = setInterval(() => this.loadData(true), 15000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  get totalPages(): number {
    return Math.ceil(this.combinedApprovals().length / this.pageSize) || 1;
  }

  get paginatedApprovals(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.combinedApprovals().slice(start, start + this.pageSize);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage() {
    if (this.currentPage > 1) this.currentPage--;
  }

  loadData(isBackground = false) {
    if (!isBackground) this.isLoading.set(true);
    
    forkJoin({
      operators: this.adminService.getOperators(),
      buses: this.adminService.getPendingBuses()
    }).subscribe({
      next: (res) => {
        const ops = res.operators.map(o => ({ ...o, type: 'Operator' }));
        const buses = res.buses.map(b => ({ ...b, type: 'Bus' }));
        
        const combined = [...ops, ...buses];
        
        // Sorting: Pending first
        combined.sort((a, b) => {
          const sA = this.getStatusText(a);
          const sB = this.getStatusText(b);
          if (sA === 'Pending' && sB !== 'Pending') return -1;
          if (sA !== 'Pending' && sB === 'Pending') return 1;
          return 0;
        });

        this.combinedApprovals.set(combined);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  // Status Helpers for safe UI rendering
  getStatusText(item: any): string {
    const s = item.status;
    if (s === 0 || s === 'Pending' || !s) {
      const reason = (item.rejectionReason || item.RejectionReason || '').toLowerCase();
      return (item.type === 'Operator' && (reason.includes('resubmitted') || reason.includes('re-review'))) 
        ? 'Pending Review' 
        : 'Pending';
    }
    if (s === 1 || s === 'Approved') return 'Approved';
    if (s === 2 || s === 'Rejected') return 'Rejected';
    if (s === 3 || s === 'Deactivated') return 'Deactivated';
    return String(s);
  }

  getStatusClass(item: any): string {
    return this.getStatusText(item).toLowerCase().replace(/\s+/g, '-');
  }

  isPending(item: any): boolean {
    const text = this.getStatusText(item);
    return text === 'Pending' || text === 'Pending Review';
  }

  isApproved(item: any): boolean {
    return this.getStatusText(item) === 'Approved';
  }

  isRejected(item: any): boolean {
    return this.getStatusText(item) === 'Rejected';
  }

  isDeactivated(item: any): boolean {
    return this.getStatusText(item) === 'Deactivated';
  }

  approve(item: any) {
    const confirmMsg = item.type === 'Operator' ? 'Approve this operator account?' : 'Approve this bus for scheduling?';
    if (confirm(confirmMsg)) {
      let obs;
      if (item.type === 'Operator') {
        // If it's an operator and currently deactivated, use activate. 
        // Otherwise use the initial approve.
        obs = this.getStatusText(item) === 'Deactivated' 
          ? this.adminService.activateOperator(item.id) 
          : this.adminService.approveOperator(item.id);
      } else {
        obs = this.adminService.approveBus(item.id);
      }

      obs.subscribe(() => {
        this.toastService.show(`${item.type} approved successfully.`);
        this.loadData();
      });
    }
  }

  activate(item: any) {
    if (confirm(`Reactivate this ${item.type.toLowerCase()}?`)) {
      this.adminService.activateOperator(item.id).subscribe(() => {
        this.toastService.show('Operator account reactivated.');
        this.loadData();
      });
    }
  }

  deny(item: any) {
    const reason = prompt(`Please state the reason for rejecting this ${item.type.toLowerCase()}:`);
    if (reason) {
      const obs = item.type === 'Operator' 
        ? this.adminService.denyOperator(item.id, reason) 
        : this.adminService.denyBus(item.id, reason);

      obs.subscribe(() => {
        this.toastService.show(`${item.type} registration rejected.`, 'info');
        this.loadData();
      });
    }
  }

  deactivate(id: string) {
    if (confirm('WARNING: Deactivating this operator will CANCEL all their future trips and REFUND all affected customers. Proceed?')) {
      this.adminService.deactivateOperator(id).subscribe(() => {
        this.toastService.show('Operator deactivated and trips cancelled.', 'info');
        this.loadData();
      });
    }
  }
}

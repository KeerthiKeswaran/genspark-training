import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-operator-analytics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="analytics-container">
      <header class="header">
        <h1>Analytics</h1>
        <p>Detailed performance insights for your fleet and routes.</p>
      </header>

      <div class="placeholder-grid">
        <div class="card">
          <h3>Monthly Revenue Trend</h3>
          <div class="chart-placeholder">
            <span>Chart implementation pending...</span>
          </div>
        </div>
        <div class="card">
          <h3>Occupancy Rates</h3>
          <div class="chart-placeholder">
            <span>Chart implementation pending...</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .analytics-container { padding: 2rem; max-width: 1200px; margin: 0 auto; }
    .header { margin-bottom: 2rem; }
    .header h1 { font-size: 1.75rem; color: #1e293b; }
    .header p { color: #64748b; }

    .placeholder-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; }
    .card { background: white; padding: 2rem; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }
    .card h3 { font-size: 1.1rem; color: #0f172a; margin-bottom: 2rem; }

    .chart-placeholder { height: 200px; background: #f8fafc; border: 2px dashed #e2e8f0; border-radius: 12px; display: flex; align-items: center; justify-content: center; color: #94a3b8; font-style: italic; }
  `]
})
export default class AnalyticsComponent {}

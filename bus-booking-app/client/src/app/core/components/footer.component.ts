import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="footer">
      <div class="footer-content">
        <div class="footer-links">
          <a href="#">About Us</a>
          <a href="#">Support</a>
          <a href="#">Terms</a>
          <a href="#">Privacy</a>
        </div>
        <div class="footer-copy">© 2026 BUSBOOK. All rights reserved.</div>
      </div>
    </footer>
  `,
  styles: [`
    .footer { background: #fff; padding: 4rem 4rem 2rem; border-top: 1px solid #eee; }
    .footer-content { max-width: 1200px; margin: 0 auto; display: flex; flex-direction: column; align-items: center; gap: 2rem; }
    .footer-logo { font-size: 1.5rem; font-weight: 900; letter-spacing: -0.05em; }
    .footer-links { display: flex; gap: 2rem; }
    .footer-links a { text-decoration: none; color: #666; font-size: 0.85rem; font-weight: 600; transition: color 0.2s; }
    .footer-links a:hover { color: #000; }
    .footer-copy { font-size: 0.75rem; color: #999; font-weight: 500; }
  `]
})
export class FooterComponent {}

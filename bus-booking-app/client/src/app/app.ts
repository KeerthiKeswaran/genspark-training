import { Component, signal, OnInit, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';

import { HeaderComponent } from './core/components/header.component';
import { FooterComponent } from './core/components/footer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, HeaderComponent, FooterComponent],
  template: `
    <app-header *ngIf="showChrome"></app-header>
    <main class="main-content">
      <router-outlet></router-outlet>
    </main>
    <app-footer *ngIf="showChrome"></app-footer>
  `,
  styles: [`
    .main-content { min-height: 80vh; }
  `]
})
export class App implements OnInit {
  protected readonly title = signal('client');
  showChrome = true;

  private router = inject(Router);

  private readonly AUTH_ROUTES = ['/login', '/register'];

  ngOnInit() {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      this.showChrome = !this.AUTH_ROUTES.some(r => e.urlAfterRedirects?.startsWith(r));
    });
  }
}

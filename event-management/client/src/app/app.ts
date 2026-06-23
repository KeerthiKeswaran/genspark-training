import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('client');
  private router = inject(Router);
  private isInitialNavigation = true;
  private previousPathname = '';

  constructor() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (typeof window === 'undefined') {
        return;
      }
      
      const currentPathname = window.location.pathname;
      const pathChanged = currentPathname !== this.previousPathname;
      this.previousPathname = currentPathname;

      // On initial navigation (first route match on page load/reload), skip manual scroll restoration
      if (this.isInitialNavigation) {
        this.isInitialNavigation = false;
        return;
      }

      // Scroll to top only if the user navigated to a different url path (a new page)
      if (pathChanged) {
        window.scrollTo(0, 0);
      }
    });
  }
}

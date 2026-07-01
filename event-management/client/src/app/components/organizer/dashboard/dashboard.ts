import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { EventService } from '../../../services/event.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

@Component({
  selector: 'app-organizer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FooterComponent, NavbarComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class OrganizerDashboardComponent implements OnInit {
  public totalEvents = signal(0);
  public ticketsSold = signal(0);
  public netEarnings = signal(0);
  public upcomingEvents = signal<any[]>([]);
  public isLoading = signal(true);

  constructor(
    private eventService: EventService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.isLoading.set(true);
    this.eventService.getMyDashboard().subscribe({
      next: (data) => {
        this.totalEvents.set(data.totalEvents ?? 0);
        this.ticketsSold.set(data.ticketsSold ?? 0);
        this.netEarnings.set(data.netEarnings ?? 0);
        this.upcomingEvents.set(data.upcomingEvents ?? []);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  public navigateToCreate(): void {
    this.router.navigate(['/myevents/create']);
  }
}

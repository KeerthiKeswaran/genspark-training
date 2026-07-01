import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../services/event.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

@Component({
  selector: 'app-organizer-events',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule, FooterComponent, NavbarComponent],
  templateUrl: './events-list.html',
  styleUrl: './events-list.css'
})
export class OrganizerEventsComponent implements OnInit {
  public allEvents = signal<any[]>([]);
  public isLoading = signal(true);

  // Filters
  public filterStatus = signal<string>('all');
  public searchKeyword = signal<string>('');

  // Computed filtered list
  public filteredEvents = computed(() => {
    let list = this.allEvents();
    
    // Status filter
    const status = this.filterStatus();
    if (status !== 'all') {
      list = list.filter(e => e.status.toLowerCase() === status.toLowerCase());
    }

    // Keyword filter
    const kw = this.searchKeyword().trim().toLowerCase();
    if (kw) {
      list = list.filter(e => e.title.toLowerCase().includes(kw) || (e.venue_Name && e.venue_Name.toLowerCase().includes(kw)));
    }

    return list;
  });

  constructor(
    private eventService: EventService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  public loadEvents(): void {
    this.isLoading.set(true);
    this.eventService.getMyEvents().subscribe({
      next: (events) => {
        this.allEvents.set(events);
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

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);

  // Base API URL (proxied through Nginx /api/)
  private readonly apiUrl = '/api/weatherforecast';

  // Signals for reactive state
  readonly forecastList = signal<WeatherForecast[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly tempUnit = signal<'C' | 'F'>('C');
  readonly searchQuery = signal<string>('');

  // Computed signal for filtered forecast cards
  readonly filteredForecasts = computed(() => {
    const list = this.forecastList();
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) {
      return list;
    }
    return list.filter(item =>
      item.summary?.toLowerCase().includes(query) ||
      item.date.includes(query)
    );
  });

  // Analytics computed signals
  readonly averageTemp = computed(() => {
    const list = this.forecastList();
    if (list.length === 0) return 0;
    const sum = list.reduce((acc, item) => acc + item.temperatureC, 0);
    return Math.round(sum / list.length);
  });

  readonly hottestDay = computed(() => {
    const list = this.forecastList();
    if (list.length === 0) return null;
    return list.reduce((prev, current) => (prev.temperatureC > current.temperatureC) ? prev : current);
  });

  readonly coldestDay = computed(() => {
    const list = this.forecastList();
    if (list.length === 0) return null;
    return list.reduce((prev, current) => (prev.temperatureC < current.temperatureC) ? prev : current);
  });

  ngOnInit(): void {
    this.fetchForecast();
  }

  fetchForecast(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.http.get<WeatherForecast[]>(this.apiUrl).subscribe({
      next: (data) => {
        this.forecastList.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('API Error:', err);
        this.errorMessage.set('Could not fetch weather forecast. Ensure the backend server is running on port 5062.');
        this.isLoading.set(false);
      }
    });
  }

  toggleUnit(): void {
    this.tempUnit.update(unit => unit === 'C' ? 'F' : 'C');
  }

  // Get dynamic weather icons based on forecast summary
  getWeatherIcon(summary: string): string {
    const s = summary.toLowerCase();
    if (s.includes('freeze') || s.includes('brace')) return '❄️';
    if (s.includes('chill') || s.includes('cool')) return '🍃';
    if (s.includes('warm') || s.includes('mild')) return '🌤️';
    if (s.includes('balmy')) return '🌅';
    if (s.includes('hot') || s.includes('swelter') || s.includes('scorch')) return '🔥';
    return '☀️';
  }

  // Get dynamic color scheme based on temperature
  getTempClass(tempC: number): string {
    if (tempC < 0) return 'temp-freezing';
    if (tempC < 15) return 'temp-cool';
    if (tempC < 30) return 'temp-warm';
    return 'temp-hot';
  }
}

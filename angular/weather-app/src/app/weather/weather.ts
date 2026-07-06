import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { WeatherService, WeatherForecast } from '../service/weather.service';

@Component({
  selector: 'app-weather',
  imports: [DatePipe],
  templateUrl: './weather.html',
  styleUrl: './weather.css',
})
export class Weather implements OnInit {
  weatherService = inject(WeatherService);
  forecasts = signal<WeatherForecast[]>([]);

  ngOnInit(): void {
    this.loadForecasts();
  }

  loadForecasts(): void {
    this.weatherService.getWeatherForecast().subscribe(data => {
      this.forecasts.set(data);
    });
  }
}

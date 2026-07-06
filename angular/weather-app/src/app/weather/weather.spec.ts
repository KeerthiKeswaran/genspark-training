import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Weather } from './weather';
import { WeatherService } from '../service/weather.service';
import { By } from '@angular/platform-browser';

describe('WeatherComponent', () => {
  let component: Weather;
  let fixture: ComponentFixture<Weather>;
  let weatherServiceMock: any;

  beforeEach(async () => {
    // Create a plain mock object
    weatherServiceMock = {
      getWeatherForecast: () => of([])
    };

    await TestBed.configureTestingModule({
      imports: [Weather],
      providers: [
        { provide: WeatherService, useValue: weatherServiceMock }
      ]
    }).compileComponents();
  });

  it('should create the component', () => {
    weatherServiceMock.getWeatherForecast = () => of([]);
    fixture = TestBed.createComponent(Weather);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    expect(component).toBeTruthy();
  });

  it('should display weather data when service returns data', () => {
    const mockData = [
      {
        date: '2026-07-07',
        temperatureC: 30,
        temperatureF: 85,
        summary: 'Sweltering'
      },
      {
        date: '2026-07-08',
        temperatureC: 40,
        temperatureF: 103,
        summary: 'Hot'
      }
    ];

    weatherServiceMock.getWeatherForecast = () => of(mockData);
    fixture = TestBed.createComponent(Weather);
    component = fixture.componentInstance;
    fixture.detectChanges(); // triggers ngOnInit

    expect(component.forecasts().length).toBe(2);

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(2);

    // Verify first row data
    const firstRowCols = rows[0].queryAll(By.css('td'));
    expect(firstRowCols[0].nativeElement.textContent).toContain('Jul 7, 2026');
    expect(firstRowCols[0].nativeElement.textContent).toContain('Tuesday');
    expect(firstRowCols[1].nativeElement.textContent.trim()).toContain('30');
    expect(firstRowCols[3].nativeElement.textContent.trim()).toContain('Sweltering');
  });

  it('should display loading message when no data is returned', () => {
    weatherServiceMock.getWeatherForecast = () => of([]);
    fixture = TestBed.createComponent(Weather);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const loadingCell = fixture.debugElement.query(By.css('.loading'));
    expect(loadingCell).toBeTruthy();
    expect(loadingCell.nativeElement.textContent.trim()).toBe('Loading forecast data...');
  });
});

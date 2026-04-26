import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { BookingService } from '../../../core/services/booking.service';
import { LocationService, City, Hub } from '../../../core/services/location.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-passenger-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="form-container">
      <div class="form-header">
        <h1>Passenger Details</h1>
      </div>

      <div class="journey-summary-card">
        <div class="summary-info">
          <span class="route">
            <ng-container *ngIf="journeyDetails; else loadingRoute">
              {{ journeyDetails.source }} → {{ journeyDetails.destination }}
            </ng-container>
            <ng-template #loadingRoute>Loading route details...</ng-template>
          </span>
          <span class="meta" *ngIf="journeyDetails">
            {{ journeyDetails.busName }} <span class="bus-number-badge">({{ journeyDetails.busNumber }})</span> • {{ journeyDetails.departureTime | date:'medium' }}
          </span>
          <span class="meta" *ngIf="!journeyDetails">Fetching journey info...</span>
        </div>
        <div class="seat-chips">
          <span class="chip" *ngFor="let s of selectedSeats">Seat {{ s }}</span>
        </div>
      </div>

      <form [formGroup]="bookingForm" (ngSubmit)="onSubmit()">
        <!-- Boarding and Dropping Points -->
        <div class="passenger-card">
          <div class="card-title">
            <h3>Boarding & Dropping Points</h3>
          </div>
          <div class="inputs-grid" style="grid-template-columns: 1fr 1fr;">
            <div class="input-group">
              <label>Boarding Point</label>
              <select formControlName="boardingPointId">
                <option value="">Select Boarding Point</option>
                <option *ngFor="let hub of boardingHubs" [value]="hub.id">{{ hub.name }}</option>
              </select>
              <span class="error" *ngIf="bookingForm.get('boardingPointId')?.invalid && bookingForm.get('boardingPointId')?.touched">
                Please select a boarding point
              </span>
            </div>
            
            <div class="input-group">
              <label>Dropping Point</label>
              <select formControlName="droppingPointId">
                <option value="">Select Dropping Point</option>
                <option *ngFor="let hub of droppingHubs" [value]="hub.id">{{ hub.name }}</option>
              </select>
              <span class="error" *ngIf="bookingForm.get('droppingPointId')?.invalid && bookingForm.get('droppingPointId')?.touched">
                Please select a dropping point
              </span>
            </div>
          </div>
        </div>

        <!-- Passengers -->
        <div formArrayName="passengers">
          <div *ngFor="let p of passengers.controls; let i = index" [formGroupName]="i" class="passenger-card">
            <div class="card-title">
              <h3>Passenger {{ i + 1 }} - Seat {{ selectedSeats[i] }}</h3>
            </div>
            
            <div class="inputs-grid">
              <div class="input-group">
                <label>Full Name</label>
                <input type="text" formControlName="name" placeholder="Enter name">
                <span class="error" *ngIf="p.get('name')?.invalid && p.get('name')?.touched">
                  <span *ngIf="p.get('name')?.errors?.['required']">Name is required</span>
                  <span *ngIf="p.get('name')?.errors?.['minlength']">Name must be at least 3 characters</span>
                </span>
              </div>
              
              <div class="input-group">
                <label>Age</label>
                <input type="number" formControlName="age" placeholder="Age">
                <span class="error" *ngIf="p.get('age')?.invalid && p.get('age')?.touched">
                  <span *ngIf="p.get('age')?.errors?.['required']">Age is required</span>
                  <span *ngIf="p.get('age')?.errors?.['min'] || p.get('age')?.errors?.['max']">Age must be between 1 and 100</span>
                </span>
              </div>
              
              <div class="input-group">
                <label>Gender</label>
                <select formControlName="gender">
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" class="btn-confirm" [disabled]="bookingForm.invalid || loading">
            Proceed to Payment
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .form-container { max-width: 800px; margin: 4rem auto; padding: 2rem; font-family: 'Inter', sans-serif; }
    .form-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 3rem; border-bottom: 3px solid #000; padding-bottom: 1rem; }
    .form-header h1 { font-weight: 900; text-transform: uppercase; }

    .journey-summary-card {
      background: #000;
      color: #fff;
      padding: 1.5rem 2rem;
      border-radius: 16px;
      margin-bottom: 3rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      box-shadow: 0 10px 30px rgba(0,0,0,0.1);
    }
    .summary-info { display: flex; flex-direction: column; gap: 0.25rem; }
    .route { font-size: 1.25rem; font-weight: 800; }
    .meta { font-size: 0.9rem; color: #aaa; display: flex; align-items: center; gap: 0.5rem; }
    .bus-number-badge { background: rgba(255,255,255,0.2); color: #fff; padding: 0.1rem 0.4rem; border-radius: 4px; font-size: 0.8rem; font-weight: 700; }
    .seat-chips { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .seat-chips .chip { background: rgba(255,255,255,0.1); padding: 0.4rem 0.75rem; border-radius: 30px; font-size: 0.75rem; font-weight: 800; }

    .passenger-card { 
      background: #fff; 
      border: 1px solid #eee; 
      padding: 2rem; 
      margin-bottom: 2rem; 
      box-shadow: 0 10px 30px rgba(0,0,0,0.05);
      border-radius: 16px;
    }
    .card-title { border-bottom: 1px solid #eee; margin-bottom: 1.5rem; padding-bottom: 0.5rem; }
    .card-title h3 { font-weight: 800; font-size: 1.1rem; }

    .inputs-grid { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 1.5rem; }
    
    .input-group { display: flex; flex-direction: column; gap: 0.5rem; }
    .input-group label { font-weight: 700; font-size: 0.8rem; text-transform: uppercase; color: #888; }
    .input-group input, .input-group select { 
      padding: 0.75rem; 
      border: 1px solid #eee; 
      font-weight: 600; 
      outline: none; 
      border-radius: 8px;
      background: #f9f9f9;
    }
    .input-group input:focus, .input-group select:focus { border-color: #000; background: #fff; }
    
    .error { color: #ff5252; font-size: 0.7rem; font-weight: 700; margin-top: 0.25rem; }

    .form-actions { margin-top: 3rem; text-align: right; }
    .btn-confirm { 
      background: #000; 
      color: #fff; 
      border: none; 
      padding: 1.25rem 3rem; 
      font-weight: 900; 
      text-transform: uppercase; 
      letter-spacing: 0.1em; 
      cursor: pointer; 
      border-radius: 40px;
      transition: background 0.2s;
    }
    .btn-confirm:hover:not(:disabled) { background: #222; }
    .btn-confirm:disabled { background: #eee; color: #aaa; cursor: not-allowed; opacity: 0.5; }
  `]
})
export class PassengerFormComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private bookingService = inject(BookingService);
  private locationService = inject(LocationService);
  private cdr = inject(ChangeDetectorRef);

  journeyDetails: any = null;
  journeyId = '';
  selectedSeats: string[] = [];
  pricePerSeat = 0;
  
  bookingForm!: FormGroup;
  loading = false;

  boardingHubs: Hub[] = [];
  droppingHubs: Hub[] = [];

  ngOnInit() {
    const state = this.router.getCurrentNavigation()?.extras.state;
    if (state && state['journeyDetails']) {
      this.journeyDetails = state['journeyDetails'];
    }

    this.route.queryParams.subscribe(params => {
      this.journeyId = params['journeyId'];
      this.selectedSeats = (params['seats'] || '').split(',');
      this.pricePerSeat = +params['price'] || 0;

      this.initForm();

      if (!this.journeyDetails) {
        this.fetchJourneyDetails();
      } else {
        this.loadHubs();
      }
    });
  }

  initForm() {
    const passengerGroups = this.selectedSeats.map(seat => this.fb.group({
      seatNumber: [seat],
      name: ['', [Validators.required, Validators.minLength(3)]],
      age: ['', [Validators.required, Validators.min(1), Validators.max(100)]],
      gender: ['Male', Validators.required]
    }));

    this.bookingForm = this.fb.group({
      boardingPointId: ['', Validators.required],
      droppingPointId: ['', Validators.required],
      passengers: this.fb.array(passengerGroups)
    });
  }

  get passengers() {
    return this.bookingForm.get('passengers') as FormArray;
  }

  loadHubs() {
    if (!this.journeyDetails) return;
    
    // Fetch all cities, match source and destination names, then fetch hubs
    this.locationService.getCities().subscribe(cities => {
      const sourceCity = cities.find(c => c.name.toLowerCase() === this.journeyDetails.source.toLowerCase());
      const destCity = cities.find(c => c.name.toLowerCase() === this.journeyDetails.destination.toLowerCase());

      if (sourceCity) {
        this.locationService.getHubs(sourceCity.id).subscribe(hubs => {
          this.boardingHubs = hubs.filter(h => h.type === 'Boarding' || h.type === 'Both');
          this.cdr.detectChanges();
        });
      }
      if (destCity) {
        this.locationService.getHubs(destCity.id).subscribe(hubs => {
          this.droppingHubs = hubs.filter(h => h.type === 'Dropping' || h.type === 'Both');
          this.cdr.detectChanges();
        });
      }
    });
  }

  fetchJourneyDetails() {
    this.bookingService.getLayout(this.journeyId).subscribe({
      next: (data) => {
        const basePrice = this.selectedSeats.length * this.pricePerSeat;
        const convenienceFee = data.platformFeeType === 'Percentage' 
          ? Math.round(basePrice * (data.platformFeeValue / 100)) 
          : data.platformFeeValue;

        this.journeyDetails = {
          source: data.source,
          destination: data.destination,
          departureTime: data.departureTime,
          busNumber: data.busNumber,
          busName: data.busName,
          basePrice: basePrice,
          convenienceFee: convenienceFee,
          totalPrice: basePrice + convenienceFee
        };
        this.loadHubs();
        this.cdr.detectChanges();
      },
      error: () => {
        console.error('Failed to fetch journey details');
      }
    });
  }

  onSubmit() {
    if (this.bookingForm.invalid) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    const formValue = this.bookingForm.value;
    const boardingPointName = this.boardingHubs.find(h => h.id === formValue.boardingPointId)?.name;
    const droppingPointName = this.droppingHubs.find(h => h.id === formValue.droppingPointId)?.name;

    // Persist to localStorage to prevent data loss on refresh
    const bookingData = {
      passengers: formValue.passengers,
      journeyId: this.journeyId,
      boardingPointId: formValue.boardingPointId,
      droppingPointId: formValue.droppingPointId,
      boardingPointName: boardingPointName,
      droppingPointName: droppingPointName,
      journeyDetails: this.journeyDetails
    };
    localStorage.setItem('pendingBooking', JSON.stringify(bookingData));

    // Lock seats NOW, so the timer only starts on payment page
    this.bookingService.lockSeats({
      journeyId: this.journeyId,
      seatNumbers: this.selectedSeats
    }).subscribe({
      next: (res) => {
        this.router.navigate(['/booking/payment'], {
          queryParams: {
            journeyId: this.journeyId,
            seats: this.selectedSeats.join(','),
            lockId: res.lockId,
            expiresAt: res.expiresAt,
            price: this.pricePerSeat
          },
          state: {
            passengers: formValue.passengers,
            boardingPointId: formValue.boardingPointId,
            droppingPointId: formValue.droppingPointId,
            boardingPointName: boardingPointName,
            droppingPointName: droppingPointName,
            journeyDetails: {
              ...this.journeyDetails,
              basePrice: this.journeyDetails?.basePrice,
              convenienceFee: this.journeyDetails?.convenienceFee,
              totalPrice: this.journeyDetails?.totalPrice
            }
          }
        });
      },
      error: (err) => {
        alert(err.error.message || 'Failed to lock seats. They might have been taken while you were filling details.');
        this.router.navigate(['/booking/seats', this.journeyId], { queryParams: { price: this.pricePerSeat } });
      }
    });
  }
}

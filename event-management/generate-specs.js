const fs = require('fs');
const path = require('path');

const components = [
  'client/src/app/components/home/about-faq/about-faq.ts',
  'client/src/app/components/home/navbar/navbar.ts',
  'client/src/app/components/home/footer/footer.ts',
  'client/src/app/components/home/popular-regions/popular-regions.ts',
  'client/src/app/components/home/events-browsing/events-browsing.ts',
  'client/src/app/components/home/location-modal/location-modal.ts',
  'client/src/app/components/home/hero-carousel/hero-carousel.ts',
  'client/src/app/components/home/home.ts',
  'client/src/app/components/browse-events/browse-events.ts',
  'client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.ts',
  'client/src/app/components/bookings/cancel-booking-modal/cancel-booking-modal.ts',
  'client/src/app/components/bookings/bookings.ts',
  'client/src/app/components/bookings/checkin/checkin.ts',
  'client/src/app/components/booking/stripe-checkout/stripe-checkout.ts',
  'client/src/app/components/booking/checkout/checkout.ts',
  'client/src/app/components/booking/booking.ts',
  'client/src/app/components/account-settings/account-settings.ts',
  'client/src/app/components/organizer/events-list/events-list.ts',
  'client/src/app/components/organizer/create-event/create-event.ts',
  'client/src/app/components/organizer/dashboard/dashboard.ts',
  'client/src/app/components/organizer/event-details-modal/event-details-modal.ts',
  'client/src/app/components/admin/sidebar/sidebar.ts',
  'client/src/app/components/admin/dashboard/dashboard.ts',
  'client/src/app/components/admin/venues/venues.ts',
  'client/src/app/components/admin/password-reset/password-reset.ts',
  'client/src/app/components/admin/helpdesk/helpdesk.ts',
  'client/src/app/components/admin/login/login.ts',
  'client/src/app/components/admin/moderation/moderation.ts',
  'client/src/app/components/shared/report-event-modal/report-event-modal.ts',
  'client/src/app/components/register/register.ts',
  'client/src/app/components/password-reset/password-reset.ts',
  'client/src/app/components/finance/escalations/escalations.ts',
  'client/src/app/components/finance/sidebar/sidebar.ts',
  'client/src/app/components/finance/dashboard/dashboard.ts',
  'client/src/app/components/finance/transactions/transactions.ts',
  'client/src/app/components/finance/login/login.ts',
  'client/src/app/components/finance/payouts/payouts.ts',
  'client/src/app/components/login/login.ts',
  'client/src/app/components/help/help.ts',
  'client/src/app/components/help/my-tickets/my-tickets.ts',
  'client/src/app/components/help/raise-ticket/raise-ticket.ts'
];

components.forEach(file => {
  const fullPath = path.resolve(__dirname, file);
  if (!fs.existsSync(fullPath)) return;
  
  const content = fs.readFileSync(fullPath, 'utf8');
  const classMatch = content.match(/export class (\w+)/);
  if (!classMatch) return;
  
  const className = classMatch[1];
  const specPath = fullPath.replace(/\.ts$/, '.spec.ts');
  const baseName = path.basename(file, '.ts');
  
  const specContent = `import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ${className} } from './${baseName}';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('${className}', () => {
  let component: ${className};
  let fixture: ComponentFixture<${className}>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [${className}],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}), params: of({}) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(${className});
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
`;
  
  fs.writeFileSync(specPath, specContent);
  console.log('Generated', specPath);
});

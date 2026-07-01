import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { BrowseEventsComponent } from './components/browse-events/browse-events';
import { BookingComponent } from './components/booking/booking';
import { BookingsComponent } from './components/bookings/bookings';
import { CheckoutComponent } from './components/booking/checkout/checkout';
import { HelpComponent } from './components/help/help';
import { RaiseTicketComponent } from './components/help/raise-ticket/raise-ticket';
import { MyTicketsComponent } from './components/help/my-tickets/my-tickets';
import { AccountSettingsComponent } from './components/account-settings/account-settings';
import { OrganizerDashboardComponent } from './components/organizer/dashboard/dashboard';
import { OrganizerEventsComponent } from './components/organizer/events-list/events-list';
import { CreateEventComponent } from './components/organizer/create-event/create-event';
import { canDeactivateGuard } from './guards/can-deactivate.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'browse', component: BrowseEventsComponent },
  { path: 'booking', component: BookingComponent },
  { path: 'bookings', component: BookingsComponent },
  { path: 'checkout', component: CheckoutComponent, canDeactivate: [canDeactivateGuard] },
  { path: 'help', component: HelpComponent },
  { path: 'raise-ticket', component: RaiseTicketComponent },
  { path: 'my-tickets', component: MyTicketsComponent },
  { path: 'settings', component: AccountSettingsComponent },
  { path: 'myevents', component: OrganizerDashboardComponent },
  { path: 'myevents/all', component: OrganizerEventsComponent },
  { path: 'myevents/create', component: CreateEventComponent, canDeactivate: [canDeactivateGuard] },
  { path: '**', redirectTo: '' }
];



import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-about-faq',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './about-faq.html',
  styleUrl: './about-faq.css'
})
export class AboutFaqComponent {
  public faqStates = signal<{ [key: number]: boolean }>({
    0: false,
    1: false,
    2: false,
    3: false
  });

  public toggleFaq(index: number): void {
    this.faqStates.update(states => {
      return { ...states, [index]: !states[index] };
    });
  }
}

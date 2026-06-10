import { Pipe, PipeTransform } from '@angular/core';

export const USD_TO_INR_RATE = 83.5;

/**
 * Global conversion function to precisely convert USD to INR.
 */
export function usdToInr(usd: number): number {
  return usd * USD_TO_INR_RATE;
}

/**
 * Global helper function to format a number as INR currency (₹).
 */
export function formatInrValue(usdAmount: number): string {
  const inr = usdToInr(usdAmount);
  return '₹' + inr.toLocaleString('en-IN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

@Pipe({
  name: 'inrCurrency',
  standalone: true
})
export class InrCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || isNaN(value)) {
      return '';
    }
    return formatInrValue(value);
  }
}

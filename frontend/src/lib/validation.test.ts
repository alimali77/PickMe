import { describe, it, expect } from 'vitest';
import {
  registerSchema,
  loginSchema,
  rateReservationSchema,
  createReservationSchema,
} from '@pickme/shared/validation';
import { ServiceType } from '@pickme/shared/constants';

describe('shared validation (frontend)', () => {
  const validReg = {
    firstName: 'Ali',
    lastName: 'Yılmaz',
    email: 'ali@pickme.tr',
    phone: '05551234567',
    password: 'Strong1Pass',
    passwordConfirm: 'Strong1Pass',
    kvkkAccepted: true,
  };

  it('register: valid input passes', () => {
    expect(registerSchema.safeParse(validReg).success).toBe(true);
  });

  it('register: mismatched passwords fail', () => {
    const r = registerSchema.safeParse({ ...validReg, passwordConfirm: 'different' });
    expect(r.success).toBe(false);
  });

  it('register: invalid phone fails', () => {
    const r = registerSchema.safeParse({ ...validReg, phone: '1234567' });
    expect(r.success).toBe(false);
  });

  it('register: weak password fails (no uppercase)', () => {
    const r = registerSchema.safeParse({ ...validReg, password: 'alllowercase1', passwordConfirm: 'alllowercase1' });
    expect(r.success).toBe(false);
  });

  it('register: kvkk must be true', () => {
    const r = registerSchema.safeParse({ ...validReg, kvkkAccepted: false });
    expect(r.success).toBe(false);
  });

  it('login: minimal valid', () => {
    expect(loginSchema.safeParse({ email: 'a@b.tr', password: 'anything' }).success).toBe(true);
  });

  it('rating: 0 out of range', () => {
    expect(rateReservationSchema.safeParse({ score: 0 }).success).toBe(false);
  });

  it('rating: 5 is valid', () => {
    expect(rateReservationSchema.safeParse({ score: 5 }).success).toBe(true);
  });

  it('reservation: time too soon fails', () => {
    const r = createReservationSchema.safeParse({
      serviceType: ServiceType.Driver,
      reservationDateTimeUtc: new Date(Date.now() + 10 * 60_000).toISOString(),
      address: 'Kadıköy, İstanbul',
      lat: 40.99,
      lng: 29.02,
      placeSelectedFromAutocomplete: true,
    });
    expect(r.success).toBe(false);
  });

  it('reservation: lat/lng outside TR fails', () => {
    const r = createReservationSchema.safeParse({
      serviceType: ServiceType.Driver,
      reservationDateTimeUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
      address: 'Somewhere',
      lat: 0,
      lng: 0,
      placeSelectedFromAutocomplete: true,
    });
    expect(r.success).toBe(false);
  });

  it('reservation: not selected from autocomplete fails', () => {
    const r = createReservationSchema.safeParse({
      serviceType: ServiceType.Valet,
      reservationDateTimeUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
      address: 'Kadıköy, İstanbul',
      lat: 40.99,
      lng: 29.02,
      placeSelectedFromAutocomplete: false,
    });
    expect(r.success).toBe(false);
  });
});

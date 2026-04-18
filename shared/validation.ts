import { z } from 'zod';
import rules from './validation-rules.json';
import { ServiceType } from './constants';

const u = rules.user;
const r = rules.reservation;
const rt = rules.rating;
const c = rules.contact;
const m = rules.messages;

const phoneRegex = new RegExp(u.phone.regex);
const emailRegex = new RegExp(u.email.regex);

export const firstNameSchema = z
  .string({ required_error: m.firstNameRequired })
  .trim()
  .min(u.firstName.minLength, m.firstNameLength)
  .max(u.firstName.maxLength, m.firstNameLength);

export const lastNameSchema = z
  .string({ required_error: m.lastNameRequired })
  .trim()
  .min(u.lastName.minLength, m.lastNameLength)
  .max(u.lastName.maxLength, m.lastNameLength);

export const emailSchema = z
  .string({ required_error: m.emailRequired })
  .trim()
  .toLowerCase()
  .max(u.email.maxLength, m.emailFormat)
  .regex(emailRegex, m.emailFormat);

export const phoneSchema = z
  .string({ required_error: m.phoneRequired })
  .trim()
  .regex(phoneRegex, m.phoneFormat);

export const passwordSchema = z
  .string({ required_error: m.passwordRequired })
  .min(u.password.minLength, m.passwordLength)
  .max(u.password.maxLength, m.passwordComplexity)
  .refine((v) => !u.password.requiresUppercase || /[A-Z]/.test(v), m.passwordComplexity)
  .refine((v) => !u.password.requiresLowercase || /[a-z]/.test(v), m.passwordComplexity)
  .refine((v) => !u.password.requiresDigit || /\d/.test(v), m.passwordComplexity);

export const registerSchema = z
  .object({
    firstName: firstNameSchema,
    lastName: lastNameSchema,
    email: emailSchema,
    phone: phoneSchema,
    password: passwordSchema,
    passwordConfirm: z.string(),
    kvkkAccepted: z.boolean().refine((v) => v === true, m.kvkkRequired),
  })
  .refine((data) => data.password === data.passwordConfirm, {
    path: ['passwordConfirm'],
    message: m.passwordConfirmMismatch,
  });
export type RegisterInput = z.infer<typeof registerSchema>;

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, m.passwordRequired),
});
export type LoginInput = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({ email: emailSchema });
export type ForgotPasswordInput = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    token: z.string().min(1),
    password: passwordSchema,
    passwordConfirm: z.string(),
  })
  .refine((d) => d.password === d.passwordConfirm, {
    path: ['passwordConfirm'],
    message: m.passwordConfirmMismatch,
  });
export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, m.passwordRequired),
    newPassword: passwordSchema,
    newPasswordConfirm: z.string(),
  })
  .refine((d) => d.newPassword === d.newPasswordConfirm, {
    path: ['newPasswordConfirm'],
    message: m.passwordConfirmMismatch,
  });
export type ChangePasswordInput = z.infer<typeof changePasswordSchema>;

export const updateProfileSchema = z.object({
  firstName: firstNameSchema,
  lastName: lastNameSchema,
  phone: phoneSchema,
});
export type UpdateProfileInput = z.infer<typeof updateProfileSchema>;

const latSchema = z.number().min(r.lat.min).max(r.lat.max);
const lngSchema = z.number().min(r.lng.min).max(r.lng.max);

export const createReservationSchema = z.object({
  serviceType: z.nativeEnum(ServiceType, { errorMap: () => ({ message: 'Hizmet türü seçiniz.' }) }),
  reservationDateTimeUtc: z
    .string()
    .datetime({ message: 'Geçerli bir tarih-saat giriniz.' })
    .refine((iso) => {
      const diffMs = new Date(iso).getTime() - Date.now();
      return diffMs >= r.minMinutesAhead * 60_000;
    }, m.reservationDateTooSoon),
  address: z
    .string()
    .trim()
    .min(r.address.minLength, m.reservationAddressRequired)
    .max(r.address.maxLength, m.reservationAddressRequired),
  lat: latSchema,
  lng: lngSchema,
  note: z.string().max(r.note.maxLength, m.noteTooLong).optional().or(z.literal('')),
  placeSelectedFromAutocomplete: z.boolean().refine((v) => v === true, m.reservationAddressRequired),
});
export type CreateReservationInput = z.infer<typeof createReservationSchema>;

export const rateReservationSchema = z.object({
  score: z
    .number()
    .int()
    .min(rt.scoreMin, m.ratingScoreRange)
    .max(rt.scoreMax, m.ratingScoreRange),
  comment: z.string().max(rt.commentMaxLength, 'Yorum en fazla 500 karakter olabilir.').optional(),
});
export type RateReservationInput = z.infer<typeof rateReservationSchema>;

export const contactSchema = z.object({
  firstName: firstNameSchema,
  email: emailSchema,
  phone: phoneSchema,
  subject: z.string().trim().min(1, 'Konu boş bırakılamaz.').max(c.subjectMaxLength),
  message: z.string().trim().min(c.messageMinLength, 'Mesaj çok kısa.').max(c.messageMaxLength),
});
export type ContactInput = z.infer<typeof contactSchema>;

export { rules as validationRules };

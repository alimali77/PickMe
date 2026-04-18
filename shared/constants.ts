export const UserRole = {
  Customer: 'Customer',
  Driver: 'Driver',
  Admin: 'Admin',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const ReservationStatus = {
  Pending: 'Pending',
  Assigned: 'Assigned',
  OnTheWay: 'OnTheWay',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
} as const;
export type ReservationStatus = (typeof ReservationStatus)[keyof typeof ReservationStatus];

export const ServiceType = {
  Driver: 'Driver',
  Valet: 'Valet',
} as const;
export type ServiceType = (typeof ServiceType)[keyof typeof ServiceType];

export const DriverStatus = {
  Active: 'Active',
  Inactive: 'Inactive',
} as const;
export type DriverStatus = (typeof DriverStatus)[keyof typeof DriverStatus];

export const CancelledBy = {
  Customer: 'Customer',
  Admin: 'Admin',
  System: 'System',
} as const;
export type CancelledBy = (typeof CancelledBy)[keyof typeof CancelledBy];

export const RESERVATION_LOCAL_LABELS: Record<ReservationStatus, string> = {
  Pending: 'Beklemede',
  Assigned: 'Atandı',
  OnTheWay: 'Yolda',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal Edildi',
};

export const SERVICE_TYPE_LABELS: Record<ServiceType, string> = {
  Driver: 'Şoför Hizmeti',
  Valet: 'Vale Hizmeti',
};

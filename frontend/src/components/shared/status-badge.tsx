import { Badge } from '@/components/ui/badge';
import { ReservationStatus, RESERVATION_LOCAL_LABELS } from '@pickme/shared/constants';

const VARIANT: Record<ReservationStatus, 'default' | 'success' | 'warning' | 'destructive' | 'secondary'> = {
  Pending: 'warning',
  Assigned: 'default',
  OnTheWay: 'default',
  Completed: 'success',
  Cancelled: 'destructive',
};

export function StatusBadge({ status }: { status: ReservationStatus }) {
  return <Badge variant={VARIANT[status]}>{RESERVATION_LOCAL_LABELS[status]}</Badge>;
}

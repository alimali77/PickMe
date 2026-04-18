import { MessageCircle } from 'lucide-react';
import { cn } from '@/lib/utils';

interface WhatsAppFabProps {
  phone?: string;
  message?: string;
  className?: string;
}

export function WhatsAppFab({
  phone = import.meta.env.VITE_WHATSAPP_NUMBER ?? '',
  message = 'Merhaba, rezervasyon hakkında bilgi almak istiyorum.',
  className,
}: WhatsAppFabProps) {
  if (!phone) return null;

  const href = `https://wa.me/${phone.replace(/\D/g, '')}?text=${encodeURIComponent(message)}`;

  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer noopener"
      aria-label="WhatsApp ile iletişim"
      className={cn(
        'fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full',
        'bg-[#25D366] text-white shadow-elevated transition-transform hover:scale-105 active:scale-95',
        'focus-visible:ring-4 focus-visible:ring-[#25D366]/40',
        className,
      )}
    >
      <MessageCircle className="size-7" aria-hidden />
    </a>
  );
}

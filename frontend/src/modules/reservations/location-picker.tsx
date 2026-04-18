import { useState } from 'react';
import { Check, MapPin, Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

export interface SelectedPlace {
  address: string;
  lat: number;
  lng: number;
}

interface LocationPickerProps {
  value: SelectedPlace | null;
  onChange: (place: SelectedPlace | null) => void;
  error?: string;
}

/**
 * Google Places Autocomplete stub'u.
 * GOOGLE_MAPS_API_KEY geldiğinde bu komponent `@react-google-maps/api`
 * `usePlacesAutocomplete` ile değiştirilecek — aynı SelectedPlace interface'i korunur.
 * Brief'teki "Autocomplete'ten seçim yapılmadan submit edilemez" kuralı
 * SelectedPlace null olduğunda frontend'in `placeSelectedFromAutocomplete=false`
 * göndermesiyle hem FE hem BE tarafında enforce edilir.
 */

const PRESETS: SelectedPlace[] = [
  { address: 'İstanbul Havalimanı (IST), Arnavutköy, İstanbul', lat: 41.275278, lng: 28.751944 },
  { address: 'Sabiha Gökçen Havalimanı (SAW), Pendik, İstanbul', lat: 40.898559, lng: 29.309219 },
  { address: 'Taksim Meydanı, Beyoğlu, İstanbul', lat: 41.0369, lng: 28.985 },
  { address: 'Kadıköy İskelesi, Kadıköy, İstanbul', lat: 40.9903, lng: 29.0234 },
  { address: 'Levent Metrosu, Beşiktaş, İstanbul', lat: 41.0827, lng: 29.0175 },
  { address: 'Bağdat Caddesi, Kadıköy, İstanbul', lat: 40.9723, lng: 29.0618 },
  { address: 'Maslak, Sarıyer, İstanbul', lat: 41.1081, lng: 29.0252 },
  { address: 'Nişantaşı, Şişli, İstanbul', lat: 41.049, lng: 28.9883 },
];

export function LocationPicker({ value, onChange, error }: LocationPickerProps) {
  const [query, setQuery] = useState(value?.address ?? '');
  const [open, setOpen] = useState(false);

  const filtered = query
    ? PRESETS.filter((p) => p.address.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr')))
    : PRESETS;

  const handleSelect = (p: SelectedPlace) => {
    setQuery(p.address);
    onChange(p);
    setOpen(false);
  };

  return (
    <div className="relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" aria-hidden />
        <Input
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            if (value) onChange(null);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          placeholder="Adres ara veya listeden seçin..."
          className="pl-9 pr-9"
          aria-invalid={!!error}
          aria-describedby={error ? 'location-err' : undefined}
        />
        {value ? (
          <Check className="absolute right-3 top-1/2 -translate-y-1/2 size-4 text-emerald-500" aria-hidden />
        ) : null}
      </div>

      {open ? (
        <div
          role="listbox"
          className="absolute z-20 mt-1 w-full rounded-md border bg-popover shadow-elevated max-h-72 overflow-auto animate-fade-in"
        >
          {filtered.length === 0 ? (
            <div className="px-3 py-4 text-sm text-muted-foreground">Eşleşen konum yok.</div>
          ) : (
            filtered.map((p) => (
              <button
                key={`${p.lat}-${p.lng}`}
                type="button"
                onClick={() => handleSelect(p)}
                className={cn(
                  'flex w-full items-start gap-3 px-3 py-2.5 text-left text-sm hover:bg-accent transition-colors',
                  value?.address === p.address && 'bg-accent',
                )}
              >
                <MapPin className="size-4 mt-0.5 text-primary shrink-0" aria-hidden />
                <span>{p.address}</span>
              </button>
            ))
          )}
        </div>
      ) : null}

      {open ? (
        <button
          type="button"
          aria-label="Kapat"
          className="fixed inset-0 z-10"
          onClick={() => setOpen(false)}
          tabIndex={-1}
        />
      ) : null}

      <p className="mt-1.5 text-[11px] text-muted-foreground">
        Konum, Google Maps kimliği geldiğinde otomatik tamamlama ile çalışacak. Şimdilik listeden seçin.
      </p>
      {error ? (
        <p id="location-err" role="alert" className="mt-1.5 text-xs font-medium text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}

import * as React from 'react';
import { cn } from '@/lib/utils';
import { Label } from './label';

interface FormFieldProps {
  id?: string;
  label: string;
  description?: string;
  error?: string;
  required?: boolean;
  className?: string;
  children: React.ReactElement;
}

export function FormField({ id, label, description, error, required, className, children }: FormFieldProps) {
  const inputId = id ?? React.useId();
  const descriptionId = description ? `${inputId}-description` : undefined;
  const errorId = error ? `${inputId}-error` : undefined;

  const enhanced = React.cloneElement(children, {
    id: inputId,
    'aria-invalid': !!error,
    'aria-describedby': [descriptionId, errorId].filter(Boolean).join(' ') || undefined,
  } as React.HTMLAttributes<HTMLElement>);

  return (
    <div className={cn('space-y-2', className)}>
      <Label htmlFor={inputId}>
        {label} {required ? <span className="text-destructive">*</span> : null}
      </Label>
      {enhanced}
      {description ? (
        <p id={descriptionId} className="text-xs text-muted-foreground">
          {description}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-xs font-medium text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}

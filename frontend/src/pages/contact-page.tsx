import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { Mail, Phone, MapPin, Clock } from 'lucide-react';
import { contactSchema, type ContactInput } from '@pickme/shared/validation';
import { SEOHead } from '@/components/shared/seo-head';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/ui/form-field';

export function ContactPage() {
  const [submitted, setSubmitted] = useState(false);
  const form = useForm<ContactInput>({
    resolver: zodResolver(contactSchema),
    defaultValues: { firstName: '', email: '', phone: '', subject: '', message: '' },
  });

  const onSubmit = async (values: ContactInput) => {
    // TODO: POST /api/contact — Faz 3'te contact endpoint'i ve rate limit backend tarafında aktif edilecek
    await new Promise((r) => setTimeout(r, 600));
    setSubmitted(true);
    toast.success('Mesajınız alındı! En kısa sürede dönüş yapacağız.');
    form.reset();
    void values;
  };

  return (
    <>
      <SEOHead
        title="İletişim – Pick Me"
        description="Pick Me ile iletişime geçin: e-posta, telefon, adres ve iletişim formu."
        canonicalPath="/iletisim"
      />

      <section className="container py-16 md:py-24">
        <div className="max-w-3xl">
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight">İletişim</h1>
          <p className="mt-4 text-lg text-muted-foreground">
            Sorularınız, özel talepleriniz veya öneriler için bize yazın — en kısa sürede dönüş yapıyoruz.
          </p>
        </div>

        <div className="mt-12 grid gap-10 lg:grid-cols-[1fr_400px]">
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 rounded-xl border bg-card p-6 shadow-soft">
            <div className="grid gap-5 md:grid-cols-2">
              <FormField label="Ad" required error={form.formState.errors.firstName?.message}>
                <Input {...form.register('firstName')} placeholder="Adınız" autoComplete="given-name" />
              </FormField>
              <FormField label="E-posta" required error={form.formState.errors.email?.message}>
                <Input type="email" {...form.register('email')} placeholder="ornek@eposta.com" autoComplete="email" />
              </FormField>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <FormField label="Telefon" required error={form.formState.errors.phone?.message}>
                <Input type="tel" {...form.register('phone')} placeholder="05551234567" autoComplete="tel" />
              </FormField>
              <FormField label="Konu" required error={form.formState.errors.subject?.message}>
                <Input {...form.register('subject')} placeholder="Konu başlığı" />
              </FormField>
            </div>

            <FormField label="Mesajınız" required error={form.formState.errors.message?.message} description="En az 10 karakter">
              <textarea
                {...form.register('message')}
                rows={5}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 aria-[invalid=true]:border-destructive"
                placeholder="Mesajınızı buraya yazın..."
              />
            </FormField>

            <Button type="submit" size="lg" loading={form.formState.isSubmitting} className="w-full sm:w-auto">
              {form.formState.isSubmitting ? 'Gönderiliyor...' : 'Mesaj Gönder'}
            </Button>

            {submitted ? (
              <p className="text-sm text-emerald-600 font-medium animate-fade-in" role="status">
                Teşekkürler! Mesajınız alındı.
              </p>
            ) : null}
          </form>

          <aside className="space-y-4">
            <ContactCard icon={Mail} label="E-posta" value="info@pickme.local" href="mailto:info@pickme.local" />
            <ContactCard icon={Phone} label="Telefon" value="+90 (555) 123 45 67" href="tel:+905551234567" />
            <ContactCard icon={MapPin} label="Adres" value="İstanbul, Türkiye" />
            <ContactCard icon={Clock} label="Çalışma Saatleri" value="7/24 hizmetteyiz" />
          </aside>
        </div>
      </section>
    </>
  );
}

function ContactCard({ icon: Icon, label, value, href }: {
  icon: typeof Mail;
  label: string;
  value: string;
  href?: string;
}) {
  const content = (
    <div className="flex gap-4 rounded-xl border bg-card p-4 shadow-soft">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
        <Icon className="size-5" aria-hidden />
      </div>
      <div>
        <div className="text-xs text-muted-foreground">{label}</div>
        <div className="font-medium">{value}</div>
      </div>
    </div>
  );
  return href ? <a href={href} className="block hover:shadow-elevated transition-shadow">{content}</a> : content;
}

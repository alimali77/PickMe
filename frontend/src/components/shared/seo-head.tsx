import { Helmet } from 'react-helmet-async';

interface SEOHeadProps {
  title: string;
  description?: string;
  canonicalPath?: string;
  ogImage?: string;
  jsonLd?: Record<string, unknown> | Record<string, unknown>[];
}

export function SEOHead({ title, description, canonicalPath, ogImage, jsonLd }: SEOHeadProps) {
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const canonical = canonicalPath ? `${origin}${canonicalPath}` : undefined;

  return (
    <Helmet>
      <title>{title}</title>
      {description && <meta name="description" content={description} />}
      {canonical && <link rel="canonical" href={canonical} />}

      {/* Open Graph */}
      <meta property="og:title" content={title} />
      {description && <meta property="og:description" content={description} />}
      <meta property="og:type" content="website" />
      {canonical && <meta property="og:url" content={canonical} />}
      {ogImage && <meta property="og:image" content={ogImage} />}

      {/* Twitter */}
      <meta name="twitter:card" content="summary_large_image" />
      <meta name="twitter:title" content={title} />
      {description && <meta name="twitter:description" content={description} />}
      {ogImage && <meta name="twitter:image" content={ogImage} />}

      {jsonLd ? (
        <script type="application/ld+json">
          {JSON.stringify(Array.isArray(jsonLd) ? jsonLd : [jsonLd])}
        </script>
      ) : null}
    </Helmet>
  );
}

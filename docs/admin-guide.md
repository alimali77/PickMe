# Pick Me — Admin Paneli Kullanım Kılavuzu

Bu kılavuz, admin paneldeki her bölümün nasıl kullanılacağını açıklar.

## Giriş

1. Tarayıcınızdan `https://pickme.example/giris` (veya lokalde `http://localhost:5173/giris`)
2. Size verilen admin e-posta ve şifreyle giriş yapın.
3. **İlk defa giriyorsanız**: Yönetici hesabınızın şifresini **hemen değiştirin** — Giriş sonrası sağ üst menüden Hesabım → Şifre Değiştir.

---

## 1. Dashboard

Üstte 4 özet kart:
- **Son 10 Rezervasyon**: toplam sistemdeki rezervasyon sayısı
- **Bekleyen Talep**: henüz şoför atanmamış Pending rezervasyonlar
- **Aktif Şoför**: Active durumdaki şoför sayısı
- **Ortalama Puan**: aktif şoförlerin ortalamalarının ortalaması

Altta son 10 rezervasyon listesi — tıklayarak detaya gidebilirsiniz.

---

## 2. Rezervasyonlar

### Listeleme
- **Filtre**: Tümü / Beklemede / Atandı / Yolda / Tamamlandı / İptal Edildi
- **Arama**: adres veya not içinde metin araması
- **Sayfalama**: 20'li sayfalar
- **CSV Dışa Aktar**: filtrelenmiş sonuçları CSV olarak indirir

### Rezervasyon detayı
- **Şoför Ata** / **Yeniden Ata**: aktif şoförleri ortalama puana göre sıralı gösterir. Seçip "Atamayı Onayla" dediğinizde:
  - Müşteriye → "Şoförünüz atandı" maili
  - Şoföre → "Yeni görev atandı" maili
  - Rezervasyon durumu → **Atandı**
- **İptal Et**: sebep zorunludur (min 1 karakter). İptal sonrası:
  - Müşteriye iptal bilgilendirme maili
  - Şoför atandıysa şoföre de mail
  - Status → **İptal Edildi**
- **Zaman çizelgesi**: Oluşturuldu / Atandı / Yola Çıkıldı / Tamamlandı / İptal saatleri.
- **Müşteri puanı**: tamamlanan rezervasyonlarda müşterinin verdiği yıldız görünür.

### Geçerli durum geçişleri
```
Pending → Assigned → OnTheWay → Completed
İptal yalnızca: Pending veya Assigned veya OnTheWay iken (Tamamlandı hariç)
Müşteri yalnızca Pending'de iptal edebilir
```

Geçersiz bir aksiyon denendiğinde (örn. Tamamlandı bir rezervasyonu iptal etmeye çalışmak) **409 Conflict** döner.

---

## 3. Şoförler

### Şoför ekle
- **Yeni Şoför** butonu
- Zorunlu: Ad, Soyad, E-posta (unique), Telefon (Türkiye mobil format)
- **Başlangıç şifresi** — boş bırakırsanız sistem 10-karakterli güçlü şifre üretir
- Oluşturulduğunda şoföre otomatik e-posta gider (giriş bilgileriyle)
- Şoförün `MustChangePassword` flag'i `true` ile başlar → ilk girişte şifre değiştirmesi zorunludur

### Şoförler listesi
Her satırda:
- Ad soyad + Aktif/Pasif badge + "Şifre değiştirmesi gerekiyor" badge'i
- E-posta, telefon
- Yıldız ortalama + toplam yolculuk sayısı
- Üç-nokta menü:
  - **Düzenle**: ad/soyad/telefon güncelle (e-posta değiştirilemez)
  - **Aktifleştir / Pasifleştir**: aktif atanmış görevi varsa **blok**
  - **Şifre Sıfırla**: yeni şifre üretir, mail gönderir, tüm aktif oturumu kapatır, `MustChangePassword=true`
  - **Sil**: soft-delete (aktif görevi varsa blok); kayıt DB'de kalır ama listeden gizlenir

---

## 4. Müşteriler

Sadece-okuma liste:
- Ad soyad, e-posta, telefon
- Aktif/Pasif
- Rezervasyon sayısı
- Kayıt tarihi

Arama: ad/e-posta/telefon.

---

## 5. Değerlendirmeler

### Liste
- **Filtre**: 1+, 2+, 3+, 4+, 5+ (minimum puan)
- Her satırda: yıldız + şoför ← müşteri + yorum + flag durumu + tarih

### Flag'leme
Uygunsuz bir yorum gördüğünüzde "Flag'le" butonuna basıp sebep girin.
- Flag'lenen puan ŞOFÖRÜN ORTALAMASINA DAHİL EDİLMEZ (anlık yeniden hesaplanır)
- Flag'leme sonrası "Flag'i Kaldır" ile geri alınabilir

---

## 6. İletişim Mesajları

İletişim formundan (`/iletisim`) gelen mesajlar burada listelenir.
- Okunmamış mesajlar **bold** + mavi zarf ikonu + "Yeni" badge
- Tıklayınca detay açılır ve otomatik okundu olarak işaretlenir
- Mail ve telefon bilgileri tıklanabilir (mailto / tel:)
- "Sadece Okunmamış" filtresiyle işe odaklanın

---

## 7. SSS (Sıkça Sorulan Sorular)

Kamuya açık `/sss` sayfasında gösterilecek soruları yönetir.
- **Yeni Soru**: soru + cevap + sıralama (küçük rakam önde)
- **Düzenle**: ayrıca Aktif checkbox'ı — pasif SSS'ler public sayfada görünmez
- **Sil**: geri alınamaz

İçerik Schema.org `FAQPage` olarak JSON-LD çıkar → SEO için fayda sağlar.

---

## 8. Yönetici Bildirim E-postaları

**Brief gereği**: yeni rezervasyon açıldığında bu listedeki tüm aktif e-postalara bildirim gider.

- **Yeni E-posta** ekle
- Aktif/Pasif toggle
- Sil
- **Kritik invariant**: son aktif kayıt deaktive veya silinemez. Sistem en az 1 aktif alıcı olmadan yeni rezervasyon kabul etmez.

---

## 9. Yöneticiler

Admin paneline erişim yetkili kullanıcılar.
- **Yeni Yönetici**: Ad soyad + e-posta + başlangıç şifresi (sistem kullanıcıya manuel bildirir)
- **Düzenle**: sadece ad/soyad
- **Sil**:
  - **Kendinizi silemezsiniz** — UI'da disabled, backend'de de reddedilir
  - **Son yöneticiyi silemezsiniz** — sistemde en az 1 yönetici olmalı

---

## 10. Sistem Ayarları

İki grup halinde:
- **İletişim**: WhatsApp numarası, e-posta, telefon, adres, çalışma saatleri — public `/api/settings/{key}` üzerinden frontend'e yansıtılır
- **Entegrasyonlar**: Google Maps API key (hassas — maskelenerek gelir), Google Analytics 4 ID

**Hassas alanlar**: placeholder'da mevcut maskelenmiş değer görünür; boş bırakırsanız **değişmez**. Güncellemek için yeni değeri yazın → Değişiklikleri Kaydet.

---

## 11. Background Jobs (Hangfire)

`/hangfire` adresinden job queue'yi görebilirsiniz (sağ üst menüde link — brief'e göre yalnızca admin).

- **Succeeded**: başarıyla gönderilmiş mailler
- **Failed**: 3 retry sonrası başarısız olanlar — "Requeue" ile yeniden denenebilir
- **Recurring**: scheduled tasks (yoksa boş)

## 12. Günlük rutin

Önerilen günlük akış:
1. Dashboard'da **Bekleyen Talep** sayısını kontrol et.
2. Rezervasyonlar → Beklemede filtresi → her birine şoför ata.
3. İletişim Mesajları → okunmamış varsa yanıtla.
4. Değerlendirmeler → 1-2 puanlıları kontrol et, uygunsuz yorum varsa flag'le.
5. Hangfire Dashboard → Failed sekmesi boş olmalı; değilse sebebi araştır.

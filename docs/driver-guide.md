# Pick Me — Şoför Uygulaması Kullanım Kılavuzu

Şoför paneli mobil telefondan kullanım için tasarlanmıştır. Ama istediğiniz her cihazdan erişebilirsiniz.

---

## 1. İlk kurulum — hesap bilgileri

Yöneticiniz size bir hesap oluşturduğunda e-postanıza **giriş bilgileri** gelir:
- **E-posta** (sistem kullanıcı adınız)
- **Başlangıç şifresi** (geçici)

## 2. İlk giriş

1. Telefonunuzun tarayıcısında `https://pickme.example/giris` açın.
2. Size verilen e-posta ve başlangıç şifresiyle giriş yapın.
3. **İlk girişte güvenliğiniz için şifrenizi değiştirmeniz zorunludur**:
   - "Mevcut (Başlangıç) Şifre": mail ile gelen şifreyi yazın
   - "Yeni Şifre": sadece sizin bildiğiniz güçlü bir şifre (min 8 karakter, büyük+küçük harf ve rakam içersin)
   - "Yeni Şifre (Tekrar)": aynısını tekrar yazın
4. "Şifreyi Güncelle ve Devam Et" → görev listenize yönlendirilirsiniz.

> **Tavsiye**: telefonunuzun ana ekranına `pickme.example/driver` kısayolu ekleyin. Chrome/Safari: Paylaş → "Ana Ekrana Ekle" — uygulamaymış gibi tam ekran açılır.

---

## 3. Görev listesi

Ana sayfa (`/driver`):
- **Atanmış**, **Yolda** ve son **Tamamlanmış** görevleri gösterir.
- Her kartta: durum + hizmet türü + tarih-saat + adres.
- Liste her 60 saniyede bir otomatik yenilenir — yeni atama gelince görürsünüz.

Alt navigasyonda:
- **Görevler** (bu sayfa)
- **Profil**

---

## 4. Görev detayı

Bir görevi tıklayınca detay sayfası açılır:
- **Müşteri adı** ve **telefonu** — telefona basmak direkt arama açar 📞
- **Tarih & saat**
- **Konum** + **"Haritada Aç"** butonu — Google Maps yeni sekmede açılır, navigasyon başlatabilirsiniz
- **Not** — müşterinin bıraktığı özel talep (varsa)

Ekranın altında iki büyük buton (durumunuza göre görünür):

### "Yola Çıktım"
- Yalnızca **Atandı** durumundayken görünür.
- Tıkladığınızda durumunuz **Yolda**'ya geçer.
- Müşteriyle iletişime geçmeden önce arayabilirsiniz.

### "Yolculuğu Tamamla"
- Yalnızca **Yolda** durumundayken görünür.
- Onay diyaloğu çıkar — yanlışlıkla basmaya karşı korur.
- Onaylarsanız durumunuz **Tamamlandı**'ya geçer.
- Müşteriye otomatik olarak **değerlendirme daveti** mail gönderilir.

> **Önemli**: Tamamlandı durumuna geçtikten sonra bu görev için durumu değiştiremezsiniz. Yanlışlıkla tamamladıysanız yöneticinize ulaşın.

---

## 5. İptal durumu

Yönetici bir görevi iptal ederse:
- Görev listenizden kaybolur.
- Yönetici size iptal bilgilendirme maili gönderir.

---

## 6. Profil

- **Şifre değiştirme**: Profil → Şifre Değiştir bölümünden.
- **İstatistikler**: toplam yolculuk, ortalama puan (yöneticiyle birlikte dashboard'da da gösterilir).

---

## 7. Sorun giderme

### "Hesabınız kilitlendi"
Üst üste 5 kere yanlış şifre girerseniz 15 dakika boyunca giriş yapamazsınız. Bekleyin veya yönetici sizi kilitten çıkarsın.

### "Hesabınız devre dışı"
Yönetici hesabınızı pasifleştirmiş. Yöneticiyle iletişime geçin.

### "Şifre değiştir" sayfasında kalıyorum
Yöneticiniz şifrenizi sıfırladı. Mail'deki geçici şifreyle giriş yapıp yeni şifre belirleyin.

### Müşteri telefonu çalmıyor
- Numarayı kontrol edin.
- Müşteriye WhatsApp üzerinden ulaşmayı deneyin (sayfanın sağ altındaki yeşil buton).

### Görevde değişiklik yapamıyorum
- "Yolculuğu Tamamla" butonu yalnızca Yolda durumundayken görünür.
- "Yola Çıktım" yalnızca Atandı durumundayken görünür.
- Durum sırası: Atandı → Yolda → Tamamlandı. Atlanamaz.

---

## 8. Güvenlik ipuçları

- Şifrenizi kimseyle paylaşmayın.
- Ortak bilgisayardan girdiyseniz çıkış yapmayı unutmayın.
- Telefonunuzu kaybedersiniz yöneticinize hemen bildirin — şifreniz sıfırlanıp oturumlarınız kapatılabilir.

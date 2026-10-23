# Fever Dream: Prototip Planı (v0.1)

**Kanıtlanacak şey (GDD):** Parkur ve dövüş akıcı biçimde birleşiyor mu?

Bu prototip yer tutucu (placeholder) geometri ve materyallerle, sanat ve ses olmadan, çekirdek mekanikleri oynanabilir
hâle getirir. Yapı baştan üretim temizliğindedir (bkz. `Docs/PROJECT_STRUCTURE.md`).

## Verilen kararlar

| Konu | Karar | Kaynak |
| --- | --- | --- |
| Kamera | Birinci şahıs | Proje sahibi |
| Parkur | Sprint, kayma, duvarda koşma, duvardan sıçrama, kenara tutunup tırmanma, Zincirli Kama kancası | Proje sahibi |
| Aura | Kombo boyunca birikir, kombo bitince toplu verilir, hasar/boşluk komboyu sıfırlar. Ekranda gösterilir, harcama yok | Proje sahibi + GDD 1.4 |
| Klasör yapısı | Tür bazlı, `_Project/` altında, önekli | Proje sahibi |
| Alter ego | Yalnızca altyazı (metin). Tonu toplam Aura'ya göre alaycı → tedirgin → panik | GDD 1.2 + tek sayfa özet |

## Varsayımlar (onay bekliyor, kolayca değiştirilebilir)

- **Üçüncü düşman tipi:** GDD iki tip sayıyor (alarm saatli ofis çalışanı, klozet kapaklı kalkan). Üçüncüsü palyaço
  korkusu motifinden **turta atan palyaço** (menzilli) olarak eklendi; oyuncuyu kanca ve parkurla mesafe kapatmaya zorlar.
- **Zincirli Kama:** GDD'de Aura ile açılıyor; prototipte baştan açık ve yalnızca parkur kancası olarak çalışıyor.
- **Tsunagi formları:** Prototipte yalnızca Katana formu dövüşüyor. Diğer formlar sonraki adım.
- **Aura ölçümü (GDD açık sorusu):** vuruş başına puan + hamle çeşitliliği çarpanı + havada vuruş, fırlatma, bitirici,
  öldürme ve son an kaçınma bonusları. Tüm değerler `SO_Aura_Default` üzerinden ayarlanır.
- **Kenara tutunma:** Prototipte "tutun ve otomatik tırman" (mantle). Asılı kalma ve kenarda yan yürüme sonraki adım.

## Kontroller (klavye/fare, gamepad)

| Eylem | Klavye/Fare | Gamepad |
| --- | --- | --- |
| Hareket / Bakış | WASD / Fare | Sol çubuk / Sağ çubuk |
| Zıpla, duvardan sıçra | Space | A |
| Sprint | Sol Shift | Sol çubuk bas |
| Kay / Eğil | Sol Ctrl veya C | B |
| Hafif saldırı | Sol tık | X |
| Ağır saldırı (fırlatma, bitirici, havada çakılma) | Sağ tık | Y |
| Kaçınma (dash) | Q | RB |
| Zincirli Kama kancası | E | LB |
| İmleci kilitle/aç | Esc | Start |
| Kontrol listesini aç/kapa | F1 | Select |

## Sistemler

### Parkur (`PlayerMovement`, ayarlar `SO_PlayerMovement_Default`)
- CharacterController tabanlı durum makinesi: Yerde, Havada, Kayma, Duvar koşusu, Tırmanma, Kanca.
- Coyote time ve zıplama tamponu; havada momentum korunur (kayma, sıçrama ve kancadan gelen hız düşmez).
- Kayma: sprint hızında eğil tuşu ile; alçak engellerin altından geçirir. Yavaşken basılı tutmak eğilerek yürümektir.
- Duvar koşusu: havada, ileri basarken, yeterli hızla yan duvara değince. Süre sınırlı, hafif yerçekimi.
  Girişte küçük bir yukarı hız verilir; kenardan zıplamadan çıkılsa bile boşluk geçilir, zıplayınca daha yüksekten gidilir.
- Duvardan sıçrama: duvar koşusunda veya havada bir duvara değerken zıpla.
- Tırmanma: ileri basarken önündeki kenar 0.9-2.4 m yükseklikteyse yerden veya havadan otomatik tırmanır.
- Kanca: görüş alanındaki en uygun kanca noktası işaretlenir, E ile çekilir. Zıpla veya tekrar E ile bırakılır.

### Dövüş (`TsunagiWeapon`, veri `SO_Tsunagi_Katana`)
- Yer: hafif zincir (3 vuruş). Ağır = **Fırlatma** (düşmanı havaya kaldırır). 3 hafiften sonra ağır = **Bitirici**.
- Hava: hafif zincir (3 vuruş, oyuncuyu ve düşmanı havada tutar). Ağır = **Çakılma** (alan hasarı).
- Girdi tamponu, kısa ileri atılma (lunge), vuruş duraklaması (hit stop), kamera sarsıntısı.
- **Kaçınma:** kısa dash, dokunulmazlık penceresi. Saldırı son anda (ilk 0.15 sn) savuşturulursa **son an kaçınma**:
  kısa ağır çekim ve Aura bonusu.

### Aura (`ComboTracker`, `AuraWallet`, ayarlar `SO_Aura_Default`)
- Her başarılı vuruş komboyu uzatır; 2.5 sn vuruş olmazsa kombo biter ve puan çarpanla birlikte verilir.
- Hasar almak veya boşluğa düşmek komboyu bozar, biriken puan kaybolur.
- Çarpan, kullanılan farklı hamle sayısıyla artar ("nasıl öldürdüğün önemli").

### Düşmanlar (`EnemyBase` ve alt sınıflar, veriler `Data/Enemies/`)
| Düşman | Davranış | Zayıf nokta |
| --- | --- | --- |
| Alarm Saatli Ofis Çalışanı | Yakın dövüş, saldırmadan önce "çalar" (kırmızı uyarı) | Çalarken vurulursa uzun süre sersemler |
| Klozet Kapaklı Kalkan | Önden gelen hafif vuruşları kapakla engeller | Ağır saldırı, fırlatma veya bitirici kalkanı kırar; arkadan ve havadan vuruşlar geçer |
| Turta Atan Palyaço | Mesafeyi korur, yan adımlar, turta fırlatır | Az canlı; turtalar kılıçla kesilebilir |

- **Saldırı sırası:** Aynı anda en fazla 2 düşman saldırı hazırlığında olabilir; diğerleri yaklaşıp bekler.
  Böylece oyuncu kalabalıkta da kombo kurabilir.
- Düşmanlar kenardan düşmez (önlerinde zemin yoksa durur), arenadan çıkamaz.

### Alter ego (`AlterEgoCommentator`, replikler `SO_AlterEgo_Pigeon`)
- Olaylara altyazıyla tepki: bölüm başı, küçük/büyük/dev kombo, kombo bozulması, boşluğa düşme, ölüm, son an kaçınma,
  kontrol noktası, bölüm sonu. Replikler toplam Aura arttıkça alaycıdan paniğe kayar.

### HUD (`HudView`, `PF_HUD`)
- Can çubuğu, nişangâh, kanca işaretçisi, kombo sayacı ve süre çubuğu, bekleyen Aura ve çarpan, toplam Aura,
  alter ego altyazısı, kontrol listesi (F1), bölüm sonu mesajı.

## Test seviyesi: `L_Proto_Dream`

Tek küçük seviye, her mekaniği sırayla öğretir:

1. **Başlangıç:** devasa buzdolabı ve çamaşır makineleri olan oda.
2. **Parkur 1:** kanepenin altından kayma → boşluk üstünde sağ duvarda koşma → 2.2 m'lik kenara tırmanma.
3. **Arena A:** 3 alarm saatli ofis çalışanı + 1 klozet kapaklı kalkan.
4. **Parkur 2:** boşluk üstünde iki kanca noktası → iniş platformu → duvardan sıçrama bacası ve tırmanma.
5. **Arena B:** 2 ofis çalışanı, 1 kalkan, kasaların üstünde 2 palyaço; arenanın üstünde kanca noktası.
6. **Hedef:** rüyadaki tuvalet. Bölüm sonu mesajı ve toplam Aura.

Her parkur bölümünden sonra kontrol noktası var; boşluğa düşen oyuncu son kontrol noktasına döner.

## Adımlar

1. Belgeler: GDD kopyası, yapı belgesi, bu plan.
2. Klasör yapısı, katmanlar, Input Actions, şablon artıklarının temizlenmesi.
3. Oyuncu: girdi, kamera, hareket ve parkur.
4. Dövüş ve Aura: Tsunagi, kaçınma, kombo sayacı, Aura cüzdanı.
5. Düşmanlar, alter ego ve HUD.
6. Yer tutucu içerik üretimi ve test seviyesi; derleme ve oynanış kontrolü.

## Kapsam dışı (sonraki adımlar)

Tsunagi'nin diğer formları ve form geçiş bonusu, Aura harcama (kontrol noktası dükkânı), animasyonlu karakter ve
düşman modelleri, ses ve seslendirme, gerçek seviye tasarımı, menüler, kaydetme, tuvalet arayışının hikâye akışı.

## Test durumu (v0.1)

Editörde Play modunda, girdi simülasyonu ve doğrudan çağrılarla doğrulananlar:

- Sprint → boşluk üstünde duvar koşusu → karşı zemine iniş → 2.2 m kenara tırmanma → Arena A'ya iniş; kontrol noktaları kaydediliyor.
- Kayma (sprint + Ctrl), Zincirli Kama ile ilk kanca noktasına çekilme ve bırakma.
- Boşluğa düşünce son kontrol noktasına dönüş, ölünce yeniden doğma.
- Tsunagi: hafif vuruş, Çakılma, fırlatma ve öldürme; vuruş komboya işleniyor.
- Kombo sonu Aura ödemesi (5 farklı hamle: 161 puan × 1.8 = 290 Aura), alter ego tepkisi.
- Son an kaçınma: hasar iptal, ağır çekim, +40 bonus, alter ego tepkisi.
- Düşman yapay zekası saldırıyor, saldırı sırası en fazla 2 saldırgana izin veriyor.

Henüz elle oynanarak denenmeyenler: fare ile nişan ve vuruş hissi, duvardan sıçrama bacası, iki kanca noktasının
zincirlenmesi, palyaço turtaları, kalkanlı düşmanın blok/kırılma davranışı, bölüm sonu, genel denge ve zorluk.

## Oynanış testi kontrol listesi

- [ ] Kanepenin altından kayarak geçilebiliyor.
- [ ] Boşluk, sağ duvarda koşarak geçilebiliyor; düşünce kontrol noktasına dönülüyor.
- [ ] 2.2 m'lik kenara tırmanılabiliyor.
- [ ] Arena A'da hafif zincir, fırlatma, havada kombo ve bitirici çalışıyor.
- [ ] Kalkanlı düşman önden hafif vuruşu engelliyor, ağır vuruşla kalkanı kırılıyor.
- [ ] İki kanca noktasıyla boşluk geçilebiliyor.
- [ ] Bacada duvardan sıçrayıp tırmanarak Arena B'ye çıkılabiliyor.
- [ ] Palyaço turtaları kaçınma ile savuşturulabiliyor veya kesilebiliyor; son an kaçınma ağır çekim veriyor.
- [ ] Kombo bitince Aura toplu ekleniyor, hasar alınca kombo bozuluyor.
- [ ] Alter ego olaylara tepki veriyor, Aura arttıkça tonu değişiyor.
- [ ] Tuvalete ulaşınca bölüm sonu mesajı çıkıyor.

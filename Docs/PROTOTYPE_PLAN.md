# Fever Dream: Prototip Planı (v0.2)

**Kanıtlanacak şey (GDD):** Parkur ve dövüş akıcı biçimde birleşiyor mu?

Bu prototip çekirdek mekanikleri oynanabilir hâle getirir. v0.1 yer tutucu geometriyle başladı; v0.2'de düşmanlar,
Katana, kontrol noktası, hedef tuvalet ve dekor için ilk sanat kiti (rüyadaki dev apartman) geldi ve bir ana menü ile
duraklatma menüsü eklendi. Ses yok. Yapı baştan üretim temizliğindedir (bkz. `Docs/PROJECT_STRUCTURE.md`).

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
| Duraklat / menü | Esc | Start |
| Kontrol listesi (duraklatır) | F1 | Select |

## Sistemler

### Parkur (`PlayerMovement`, ayarlar `SO_PlayerMovement_Default`)
- CharacterController tabanlı durum makinesi: Yerde, Havada, Kayma, Duvar koşusu, Tırmanma, Kanca.
- Coyote time ve zıplama tamponu; havada momentum korunur (kayma, sıçrama ve kancadan gelen hız düşmez).
  Duvara çarpınca duvara doğru olan hız sıfırlanır (köşelere yapışma ve yerinde duvar koşusu olmaz).
- Kayma: sprint hızında eğil tuşu ile; alçak engellerin altından geçirir. İnişten hemen önce basılan veya inişe
  basılı tutularak gelinen kayma da çalışır. Yavaşken basılı tutmak eğilerek yürümektir.
- Duvar koşusu: havada, ileri basarken, yeterli hızla yan duvara değince. Süre sınırlı, hafif yerçekimi.
  Girişte küçük bir yukarı hız verilir; kenardan zıplamadan çıkılsa bile boşluk geçilir. Kenardan biraz geç
  (coyote süresi içinde) zıplanırsa oyuncu duvardan atılmaz, zamanında zıplamış gibi aynı yükselişi alır.
  Duvar koşusu bir iç köşeye girerse önde tırmanılacak kenar varsa tırmanır, yoksa duvarı bırakır (köşeye yapışmaz).
- Duvardan sıçrama: duvar koşusunda veya havada bir duvara değerken zıpla. Koşulan veya sıçranan duvar, yere
  basılana kadar tekrar kullanılamaz. Aynı yöne bakan duvarlar (yan yana bloklar) aynı duvar sayılır; bacada
  karşılıklı iki duvar arasında sıçranır.
- Kayma hızlandırması üst sınırlıdır (`slideMaxSpeed`); kayıp zıplayarak sınırsız hız kazanılmaz.
- Tırmanma: ileri basarken önündeki kenar 0.9-2.4 m yükseklikteyse yerden veya havadan otomatik tırmanır.
- Kanca: görüş alanındaki en uygun kanca noktası işaretlenir, E ile çekilir. Çekilirken başka bir noktaya bakıp
  tekrar E'ye basmak doğrudan o noktaya zincirler; başka nokta yokken E veya zıpla bırakır.
- Ölüm ve boşluk: seviyenin altındaki ölüm alanı (y≈-15) son kontrol noktasına döndürür. Ölünce 1.2 sn kontrol yoktur.

### Dövüş (`TsunagiWeapon`, veri `SO_Tsunagi_Katana`)
- Yer: hafif zincir (3 vuruş). Ağır = **Fırlatma** (düşmanı havaya kaldırır). 3 hafiften sonra ağır = **Bitirici**.
- Hava: hafif zincir (3 vuruş). Yalnızca **isabet eden** hava vuruşları oyuncuyu ve düşmanı havada tutar; boşa
  sallamak süzülme sağlamaz. Ağır = **Çakılma** (alan hasarı).
- Girdi tamponu, yerde kısa ileri atılma (lunge, metre cinsinden; havada yok), vuruş duraklaması (hit stop), kamera sarsıntısı.
- Vuruş yönü her zaman oyuncudan hedefe doğrudur (kalkanın önden bloğu ve geri itme gerçek konuma göre çalışır).
- **Kaçınma:** kısa dash, dokunulmazlık penceresi. Saldırı son anda (ilk 0.15 sn) savuşturulursa **son an kaçınma**:
  kısa ağır çekim ve Aura bonusu. Kaçınma ve hasar ayarları `SO_PlayerCombat_Default`, kamera ayarları `SO_PlayerCamera_Default`.

### Aura (`ComboTracker`, `AuraWallet`, ayarlar `SO_Aura_Default`)
- Her başarılı vuruş komboyu uzatır; 2.5 sn vuruş olmazsa kombo biter ve puan çarpanla birlikte verilir.
- En az 2 vuruş gerekir; içinde son an kaçınma olan kombo vuruşsuz da ödenir.
- Bonuslar: havada vuruş, fırlatma, bitirici, çakılma, öldürme ve son an kaçınma.
- Hasar almak veya boşluğa düşmek komboyu bozar, biriken puan kaybolur. Hedefe varınca açık kombo hemen ödenir.
- HUD, en az vuruşa ulaşmamış komboda Aura vaat etmez ("en az 2 vuruş" yazar).
- Kombo sürerken boşluğa düşürülen düşman da öldürme bonusu verir.
- Çarpan, kullanılan farklı hamle sayısıyla artar ("nasıl öldürdüğün önemli").
- Turta kesmek oyuncuyu korur ama kombo vuruşu sayılmaz.

### Düşmanlar (`EnemyBase` ve alt sınıflar, veriler `Data/Enemies/`)
| Düşman | Davranış | Zayıf nokta |
| --- | --- | --- |
| Alarm Saatli Ofis Çalışanı | Yakın dövüş, saldırmadan önce "çalar" (kırmızı uyarı) | Çalarken vurulursa uzun süre sersemler |
| Klozet Kapaklı Kalkan | Önden gelen hafif vuruşları kapakla engeller | Önden ağır saldırı (fırlatma, bitirici, çakılma) kalkanı kırar; arkadan ve havadan hafif vuruşlar geçer |
| Turta Atan Palyaço | Mesafeyi korur, yan adımlar, turta fırlatır | Az canlı; turtalar kılıçla kesilebilir |

- **Saldırı sırası:** Bir arenada aynı anda en fazla 2 düşman saldırı hazırlığında olabilir (`EnemyArena`
  üzerinde ayarlanır); diğerleri yaklaşıp bekler. Böylece oyuncu kalabalıkta da kombo kurabilir.
- **Arena:** Her düşman grubu (`Arena_A`, `Arena_B`) bir `EnemyArena` kutusudur. Düşmanlar oyuncu kutuya girince
  savaşa başlar, oyuncu kutunun 4 m yakınında kaldıkça savaşmayı sürdürür (kapı ağzından bedava vuruş yok), kutunun
  dışına yürümez. Vuruşla kutudan dışarı itilen düşman geri yürür.
- Düşmanlar kenardan düşmez (önlerinde zemin yoksa durur). Boşluğa düşen düşman yenilmiş sayılır.
- Düşmana özgü ayarlar (alarm saatinin sersemleme çarpanı, kalkanın kırılma süresi ve ön açısı, palyaçonun yan adım
  ve nişan öngörüsü) `EnemySettings` içindedir.

### Alter ego (`AlterEgoCommentator`, replikler `SO_AlterEgo_Pigeon`)
- Olaylara altyazıyla tepki: bölüm başı, küçük/büyük/dev kombo, kombo bozulması, boşluğa düşme, ölüm, son an kaçınma,
  düşman yenilmesi (boşluğa düşme dahil), kontrol noktası, bölüm sonu. Replikler toplam Aura arttıkça alaycıdan
  paniğe kayar. Bölüm bitince kombo replikleri bölüm sonu repliğinin üstüne konuşmaz.

### HUD (`HudView`, `PF_HUD`)
- Can çubuğu, nişangâh, kanca işaretçisi, kombo sayacı ve süre çubuğu, bekleyen Aura ve çarpan, toplam Aura,
  alter ego altyazısı, bölüm sonu mesajı. Kontrol listesi oyun ekranında yazmaz; menülerdedir.
- Görünüm manga/anime aksiyon stilindedir (bkz. `PROJECT_STRUCTURE.md`, "Arayüz stili"):
  - Can: atan bir kalp ve eğik çerçevede 10 bölme. Hasar alınca çubuk sarsılır, can %30'un altındayken bölmeler
    kırmızıya yanıp söner.
  - Aura: eğik plaka, "AURA" etiketi, toplam değişince dönen yıldız ve temiz çizgili sayı.
  - Kombo sayacı bir patlamanın üstünde her vuruşta zıplar; alter ego konuşma balonunda isim etiketiyle konuşur.
  - Nişangâh ve kanca işaretçisi çizilmiş ikonlardır; kanca işaretçisi hedefin üstünde döner.
- Oyun duraklayınca HUD gizlenir; menünün arkasından görünmez.

### Menüler (`MainMenuView`, `PauseMenuView`, `ControlsPanel`, `GamePause`, `SceneFlow`)
- **Ana menü (`MENU_Main`):** Oyna, Kontroller, Çıkış. Arkada kitten kurulmuş bir rüya odası: ortada dev tuvalet, sağ
  duvarda apartman kapıları. Build listesinde ilk sahnedir.
- **Duraklatma (`PF_PauseMenu`):** Esc / Start açar. Devam Et, Kontroller, Yeniden Başla, Ana Menü, Çıkış. Açıkken zaman
  durur (`TimeEffects` üzerinden, vuruş duraksamasıyla çakışmaz), imleç serbest kalır ve oyun girdisi yok sayılır. Devam
  ettiren tıklama veya gamepad A, aynı karede saldırı ya da zıplama olarak okunmaz.
- **Kontroller (`PF_ControlsPanel`):** iki menünün de gösterdiği tek kontrol listesi. Oyunda F1 menüyü doğrudan bu
  sayfada açar; oradan Geri, Esc ya da gamepad B oyuna döndürür. Menüden açıldıysa menüye döndürür.
- **Bölüm sonu:** tuvalete ulaştıktan 4 sn sonra aynı menü "Bölüm tamamlandı" başlığı ve toplam Aura ile, Devam Et
  olmadan açılır. Kendi açıldığı için ilk 0.5 sn girdi almaz: o anda yapılan saldırı tıklaması bir düğmeye basmaz.

## Test seviyesi: `L_Proto_Dream`

Tek seviye, her mekaniği sırayla öğretir. v0.2'de rüyadaki dev bir apartmana dönüştü: oyuncu fare boyunda, eşyalar
4-5 kat büyük. Seviye artık elle düzenlenir (kodla üretilmez).

1. **Yatak odası:** Jack dev yatağın üstünde uyanır. Pencerede perdeler.
2. **Parkur 1:** kanepe engelinin altından kayma → boşluk üstünde sağ duvarda koşma → 2.2 m'lik kenara tırmanma.
3. **Arena A (mutfak ve çamaşır köşesi):** 3 alarm saatli ofis çalışanı + 1 klozet kapaklı kalkan. Tezgâh, çamaşır
   makineleri, buzdolabı.
4. **Parkur 2:** çamaşır ipleri altındaki boşlukta iki kanca noktası → iniş platformu → duvardan sıçrama bacası ve
   tırmanma. Arena B, platformdan 6 m yukarıdadır: arka duvardan tek sıçrama yetmez, en az bir yan duvar sıçraması gerekir.
5. **Arena B (oturma odası):** 2 ofis çalışanı, 1 kalkan, çamaşır makinelerinin üstünde 2 palyaço; dev kanepe, posta
   kutuları; arenanın üstünde kanca noktası.
6. **Merdiven holü (yeni):** apartman kapıları, dev merdivenle 8.55 m yukarıdaki kata çıkış, sahanlıkta kontrol noktası.
7. **Banyo (yeni):** ışıklı banyo kapısı, ayna, duş perdesi ve hedef: rüyadaki dev tuvalet. Bölüm sonu mesajı, toplam
   Aura ve bölüm sonu menüsü.

Her parkur bölümünden sonra kontrol noktası var; boşluğa düşen oyuncu son kontrol noktasına döner.

## Adımlar

1. Belgeler: GDD kopyası, yapı belgesi, bu plan.
2. Klasör yapısı, katmanlar, Input Actions, şablon artıklarının temizlenmesi.
3. Oyuncu: girdi, kamera, hareket ve parkur.
4. Dövüş ve Aura: Tsunagi, kaçınma, kombo sayacı, Aura cüzdanı.
5. Düşmanlar, alter ego ve HUD.
6. Yer tutucu içerik üretimi ve test seviyesi; derleme ve oynanış kontrolü.

## Kapsam dışı (sonraki adımlar)

Tsunagi'nin diğer formları ve form geçiş bonusu (modelleri `Art/Models/Weapons/` altında hazır), Aura harcama (kontrol
noktası dükkânı), düşman animasyonları (modeller parçalı ve iskelet hiyerarşili, animasyon eklenebilir), ses ve
seslendirme, ayarlar menüsü, kaydetme, tuvalet arayışının hikâye akışı.

## Test durumu (v0.1)

Editörde Play modunda, girdi simülasyonu ve doğrudan çağrılarla doğrulananlar:

- Sprint → boşluk üstünde duvar koşusu → karşı zemine iniş → 2.2 m kenara tırmanma → Arena A'ya iniş; kontrol noktaları kaydediliyor.
- Kayma (sprint + Ctrl), Zincirli Kama ile ilk kanca noktasına çekilme ve bırakma.
- Boşluğa düşünce son kontrol noktasına dönüş, ölünce yeniden doğma.
- Tsunagi: hafif vuruş, Çakılma, fırlatma ve öldürme; vuruş komboya işleniyor.
- Kombo sonu Aura ödemesi (5 farklı hamle: 161 puan × 1.8 = 290 Aura), alter ego tepkisi.
- Son an kaçınma: hasar iptal, ağır çekim, +40 bonus, alter ego tepkisi.
- Düşman yapay zekası saldırıyor, saldırı sırası en fazla 2 saldırgana izin veriyor.

PR #1 incelemesinden sonra (v0.1.1) ayrıca doğrulananlar:

- Kenarda coyote içinde geç zıplama duvar koşusunu yükseltiyor, oyuncuyu duvardan atmıyor; normal duvar zıplaması
  çalışıyor ve aynı duvarı yere basılana kadar kilitliyor.
- İnişe Ctrl basılı tutularak gelindiğinde kayma başlıyor.
- Boşa sallanan hava saldırısı süzdürmüyor ve ileri atmıyor; yerde atılma uygulanıyor ve uzun karelerde mesafeyi aşmıyor.
- Kalkan: önden hafif vuruş bloklanıyor, arkadan geçiyor, Çakılma kalkanı kırıyor, kırıktan sonra önden vuruş işliyor.
- Arena: oyuncu dışarı çıkınca düşmanlar duruyor ve arenada kalıyor, saldırı sıraları boşalıyor.
- Ölüyken kontrol kapalı ve hedef tetiklenmiyor; yeniden doğunca kontrol geri geliyor.
- Hedefte açık kombo ödeniyor, bölüm sonu mesajı son toplamla kalıyor ve sonradan bozulmuyor.
- Yalnızca son an kaçınma içeren kombo ödeniyor (40 Aura); turta kesmek kombo vuruşu sayılmıyor.

Doğrulama turundan sonra (v0.1.2) ayrıca doğrulananlar:

- Bitişik ve aynı yöne bakan duvarlar tek duvar sayılıyor; bacadaki karşılıklı duvarlar ayrı sayılıyor.
- Kayma hızı 13.5'te sınırlanıyor; coyote içinde geç zıplama zamanında zıplamayla aynı yükselişi veriyor.
- Köşeye giren duvar koşusu, tırmanılacak kenar yoksa duvarı bırakıyor (yapışmıyor).
- Yerde atılma 60, 30 ve 5 FPS'te yazılan 0.4 m'yi veriyor.
- Arenadan itilen düşman geri yürüyor; kapı ağzındaki oyuncuyla düşmanlar savaşmayı sürdürüyor.
- Tek vuruşlu kombo HUD'da Aura vaat etmiyor ("en az 2 vuruş").

Henüz elle oynanarak denenmeyenler: fare ile nişan ve vuruş hissi, duvardan sıçrama bacası (yeni 6 m yükseklikte),
köşedeki duvar koşusundan kenara tırmanma, iki kanca noktasının zincirlenmesi, palyaço turtalarının gerçek isabeti,
genel denge ve zorluk.

## Test durumu (v0.2)

Play modunda doğrulananlar:

- Oyuncu yatağın üstünde başlıyor, yatağın ayak ucundan yere iniyor.
- Karakter denetleyicisi merdiveni sonuna kadar çıkıyor (8.55 m), sahanlıktan üst hole ve ışıklı kapıdan banyoya geçiyor.
- Duraklatma: zaman duruyor, imleç serbest kalıyor, ilk düğme seçili; Devam Et ile her şey geri geliyor ve devam
  ettiren kare oyun girdisi olarak okunmuyor.
- Tuvalete ulaşınca 4 sn sonra "Bölüm tamamlandı" menüsü açılıyor; Yeniden Başla seviyeyi temiz başlatıyor.
- Ana menü ↔ seviye geçişleri: zaman ve imleç her sahnede doğru durumda, kontroller sayfası açılıp kapanıyor.
- Yeni düşman modelleri: vuruş parlaması bütün malzeme yuvalarında çalışıp temizleniyor, kalkan kırılınca yana
  açılıyor, palyaçolar çamaşır makinelerinin üstünde kalıp turta atıyor, düşmanlar kovalıyor ve vuruyor.
- Arayüz gerçek oyun ekranından alınan görüntülerle kontrol edildi: HUD, duraklatma, kontroller, bölüm sonu, ana menü.
- Menü akışı: Esc açıp kapatıyor; F1 doğrudan kontrol listesini açıyor ve Geri oyuna döndürüyor; menüden açılan kontrol
  listesinden Geri/Esc menüye döndürüyor. Esc hem Duraklat hem UI Cancel olarak geldiğinde kare başına bir kez işleniyor.
- Menü açıldığında duran imleç seçimi değiştirmiyor, fare oynayınca altındaki düğme seçiliyor. Bölüm sonu menüsü kendi
  açıldığında 0.5 sn girdi almıyor.
- Oyun duraklayınca HUD gizleniyor; can çubuğu %25'te 2.5 bölme gösterip yanıp sönüyor.
- Konsol temiz (hata ve uyarı yok).

Henüz elle oynanarak denenmeyenler: yeni dekorun parkur hissi (kanepe, tezgâh ve makinelerin üstüne çıkış), merdiven
holü ve banyo bölümlerinin akışı, menülerin gerçek klavye, fare ve gamepad ile gezilmesi (menü akışı kod üzerinden
tetiklenerek denendi).

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

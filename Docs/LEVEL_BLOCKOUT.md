# Fever Dream: Blockout Rehberi

Yeni bir bölümün blockout'unu (gri kutu taslağını) yapan tasarımcı içindir. Klasör ve adlandırma için
`PROJECT_STRUCTURE.md`, git akışı için kökteki `CONTRIBUTING.md` geçerlidir. Burada yalnızca bölüm tasarımına özgü
kurallar ve oyuncunun gerçek ölçüleri var.

## Başlamadan önce

- **Kendi bölümünü kendi sahnende yap.** `L_Proto_Dream` örnek ve test seviyesidir; ölçüler için açıp bakabilirsin.
- Yeni bölüm `Assets/_Project/Scenes/Levels/` altında `L_<BolumAdi>` sahnesidir (PascalCase, boşluksuz, Türkçe
  karaktersiz; ör. `L_Dream_Kitchen`). Denemeler `Scenes/Sandbox/` altında `SB_<Ad>` olur; bunlar build'e girmez.
- En hızlı başlangıç: Project penceresinde `L_Proto_Dream`'i seç, **Ctrl+D** ile çoğalt ve yeniden adlandır (F2).
  Işık, sis, oyuncu, alter ego, HUD, duraklatma menüsü ve EventSystem hazır gelir. Boş bir sahneden başlarsan bunları
  da ekle: menüler `PF_PauseMenu` ve tıklanabilmesi için bir `EventSystem` ister. Sonra:
  - `--- ENVIRONMENT ---` ve `--- ENEMIES ---` altını kendi bölümüne göre değiştir.
  - `--- GAMEPLAY ---` altında oyuncuyu (`Player`), kontrol noktalarını, kanca noktalarını, hedefi ve
    `KillVolume_Void`'u yeni yerleşime taşı. Oyuncu ilk kez başladığı yerde yeniden doğar: altında zemin yoksa
    sonsuza kadar düşüp doğar. Ölüm alanının üstü y = -10'dadır; bundan alçak zemin öldürür.
- Sahneyi açmadan önce ekibe haber ver: aynı sahneyi aynı anda iki kişi düzenlemez. Kendi dalında çalış
  (`feature/<kısa-ad>` veya `art/<kısa-ad>`).

## Geometri kuralları

- **Yürünen, koşulan, tırmanılan ve arkasına saklanılan her şey `Environment` (8) katmanında olmalı.** ProBuilder
  yeni şekilleri `Default` katmanında oluşturur; katmanı Inspector'dan değiştir. Oyuncu `Default` bir yüzeyin üstünde
  yine durur, bu yüzden hata kolay gözden kaçar. Ama orada:
  - duvar koşusu, duvardan sıçrama (dolayısıyla baca) ve tırmanma çalışmaz,
  - alçak engelin altında oyuncu ayağa kalkabilir,
  - kanca görüşü, palyaçonun görüşü ve turtaları duvardan geçer,
  - düşmanlar o zeminde yerinden kıpırdamaz.
- Her parçanın çarpıştırıcısı olmalı; yalnızca renk şeritleri (`Decal_`) çarpıştırıcısızdır. ProBuilder yeni şekle
  Mesh Collider ekler.
- Duvarlar dik olmalı (dikeyden en fazla ~17° sapma). Üstünde durulacak yüzeyler en fazla 45° eğimli olmalı.
- Köşede duvarlar üst üste binmeli: iki duvar yalnızca kenarından değerse köşedeki sütun boş kalır, ay ışığı oradan
  içeri sızar ve zeminde ince bir ışık çizgisi görünür. Duvarlardan birini köşeyi kapatacak kadar uzat.
- Parçalar `--- ENVIRONMENT ---` altında bölüm gruplarına girer (`Section_01_Bedroom`, `Section_02_Parkour` ...). Grup
  transform'ları sıfırdır. Adlar açıklayıcı olur: `Floor_`, `Wall_`, `WallRun_`, `Ledge_`, `Obstacle_SlideUnder_`,
  `Prop_`, `Decal_` ve gerekirse numara (`Wall_Corridor_Left`, `Prop_GiantFridge_01`).
- Renk dili (`Art/Materials/Prototype/`). Mekanik renge değil geometriye bağlıdır; renk, oyuncu yüzeyin ne işe
  yaradığını bir bakışta anlasın diye vardır:

| Materyal | Anlamı |
| --- | --- |
| `M_Proto_Floor` | Zemin |
| `M_Proto_Wall` | Normal duvar |
| `M_Proto_WallRun` (mavi) | Üstünde koşulan veya sıçranan duvar |
| `M_Proto_Ledge` (sarı) | Tırmanılacak kenarın üstüne ince şerit (`Decal_`, çarpıştırıcısız) |
| `M_Proto_SlideUnder` (turuncu) | Altından kayılacak engel |

- Blockout oynanır hale gelince zemin, duvar ve duvar koşusu yüzeyleri `Art/Materials/World/` altındaki
  `M_World_Floor`, `M_World_Wall` ve `M_World_WallRun` ile değiştirilir (`L_Proto_Dream` böyledir; istersen baştan
  bunlarla da çalışabilirsin). Desenleri dünya konumundan hesaplanır: UV açmak gerekmez, şekli ölçeklemek deseni
  bozmaz. Parlayan camgöbeği çizgiler "bu duvarda koşulur" demektir. Sarı ve turuncu işaretler `M_Proto_` olarak kalır.
- Ayı (`--- SETUP ---/Light_Moon`) döndürürsen veya gökyüzünü değiştirirsen **Fever Dream > Setup > Bake Sky
  Reflection** komutunu çalıştırıp sahneyi kaydet; ayna ve metal yüzeyler gökyüzünü bu dokudan yansıtır.

## Oyuncunun ölçüleri

Değerler `Data/Player/SO_PlayerMovement_Default`'tan ve `PF_Player`'daki CharacterController'dan (yarıçap, basamak)
gelir; bunlardan biri değişirse bu bölüm de güncellenir. Mesafeler kapsülün merkezine göredir. Zıplama, duvar koşusu
ve kanca yayları kare hızından bağımsızdır (30, 60 ve 144 FPS'te aynı). Yalnızca her karede bir kez yapılan kontroller
(kenara tutunma, duvardan sıçrama) 30 FPS'te bir kare geç tetiklenip ~0.2 m'ye kadar kısa kalabilir; aşağıdaki
değerler bunu hesaba katar.
**"Rahat" değerlerle tasarla**, "en fazla" değerleri yalnızca bilerek zor yapılan geçişlerde kullan.

| | Değer |
| --- | --- |
| Boy / genişlik | 1.8 m / 0.7 m. Ayakta geçmek için ~1.9 m yükseklik ve ~0.8 m genişlik gerekir; çömelip kayarak 1.1 m |
| Basamak | 0.35 m'ye kadar yürüyerek çıkılır, daha yükseği zıplanarak. Çömelirken ve kayarken yalnızca 0.05 m |
| Hız | Yürüme 6.5 m/s, koşu (Shift) 10.5 m/s, çömelme 3.5 m/s |
| Zıplama | 1.4 m |
| Aynı yükseklikteki boşluk | Yürüyerek rahat 3 m (en fazla ~4 m), koşarak rahat 5 m (en fazla ~6.4 m), kayıp zıplayarak rahat 6.5 m (en fazla ~8.2 m) |
| 1 m yukarıdaki platforma | Koşarak en fazla ~4.9 m |

Bunlar platformun **üstüne inme** mesafesidir. Karşı tarafın dik ve dolu bir ön yüzü varsa kısa kalan oyuncu havada
kenara tutunup tırmanır: aynı yükseklikte koşarak ~9 m, kayıp zıplayarak ~11 m, 1 m yukarıya koşarak ~8 m geçilir. Bir
boşluğun yalnızca duvar koşusu veya kancayla geçilmesini istiyorsan onu bundan uzun yap.

### Kenara tırmanma

- İleri basarken önündeki kenarın üstü ayaklarının 0.9-2.4 m yukarısındaysa oyuncu kendiliğinden tırmanır, yerden de
  havadan da. Zıplayarak rahat 3.5 m'ye (en fazla ~3.9 m) kadar kenarlara çıkılır.
- Kenarın ön yüzü dik ve dolu olmalı: tırmanma yüzü ayakların ~0.7 m üstünde arar. Havada duran ince platformlara
  tırmanmak zordur (1 m kalınlıkta iyi çalışır), ~0.2 m'den incelere hiç tırmanılamaz, üstlerine ancak zıplayıp inilir.
- Kenarın üstünde en az 1 m derinlik ve ~1.9 m boş yükseklik olmalı.
- Örnek: `L_Proto_Dream`'deki 2.2 m'lik `Ledge_Step_01`.

### Kayma ve alçak geçitler

- Koşarken (en az 7.5 m/s) Ctrl veya C **basılı tutularak** kayılır: en fazla ~1.1 sn'de ~10 m. Tuş erken bırakılırsa
  kayma ~3.5 m'de biter.
- Altından kayılacak engelin alt kenarı yerden 1.1-1.7 m yukarıda olsun; ayakta geçilemez. Örnek: `L_Proto_Dream`'deki
  kanepe, 1.1 m.
- Alçak geçidi 8 m'den uzun yapma: kayma bitince oyuncu kalanını çömelerek, yavaş yürür.
- Alçak geçidin zemini düz olsun: çömelmiş ve kayan oyuncu 5 cm'den yüksek eşiği aşamaz
  (`SO_PlayerMovement_Default` > `crouchStepOffset`). Bu değer bilerek küçüktür: kontrolcü her adımda basamak payı
  kadar yükselir, ayaktaki 0.35 m ile 1.1 m'lik boşluğun kenarına takılır ve geçemez.

### Duvar koşusu

- Oyuncu havadayken, ileri basarken ve en az 5 m/s hızla bir duvarı **sağına veya soluna** alınca başlar. Duvar,
  gövdesinden en fazla ~0.8 m uzakta olmalı; kalkış yolunu duvara bu kadar yakın çiz (örnekteki mavi şerit duvardan
  0.8 m uzakta). Hareket yönüyle duvar arasındaki açı ~50°'den küçük olmalı; dümdüz duvara koşmak duvar koşusu değil,
  sıçrama veya tırmanmadır. Yerden en az 0.9 m yüksekte olmak gerekir, yani önce zıplanır.
- 11 m/s ile en fazla 1.8 sn sürer: duvar boyunca ~20 m. Koşu yolu bundan uzunsa oyuncu yolun sonunda düşer.
- Koşu sırasında oyuncu girişe göre ~1.25 m'ye (kenardan geç zıplayınca ~1.4 m'ye) kadar yükselir, sona doğru ~0.5 m
  alçalabilir. Duvar, kalkış zemininin altından başlayıp en az 4 m yukarı çıkmalı. Örnek: `WallRun_Right_01`, 18 m
  uzunluk, 16 m'lik boşluğun üstünde, zeminin 6 m altından 6 m üstüne.
- Az önce koşulan veya sıçranan duvar, ve ona aynı yöne bakan her duvar (yan yana bloklar), bir sonraki koşu ya da
  sıçrama için kullanılamaz. Kilidi başka yöne bakan (en az ~26° farklı) bir duvarı kullanmak, yere basmak veya kanca
  atmak kaldırır. Art arda koşular için duvarlar karşılıklı olmalı: sağ, sol, sağ.
- Koşu bir iç köşeye girerse önde tırmanılacak kenar varsa oyuncu tırmanır, yoksa düşer.

### Duvardan sıçrama ve bacalar

- Duvar koşusunda veya havada bir duvara değerken zıplamak oyuncuyu ~1.7 m yukarı ve duvardan uzağa atar. Duvar,
  oyuncunun önünde, arkasında ya da yanında ~0.8 m içinde olmalı.
- Baca: karşılıklı iki duvar arasında sırayla sıçrayarak tırmanılır. İç genişlik 3-4 m olsun. 5-6 m'de yalnızca
  duvara varmadan basılan sıçramalarla yükselinir, zordur. Genişlik tırmanmayı engellemez: karşı duvara dönüp ileri
  basan oyuncu 7 m'lik bacayı da çıkar. Tırmanılmasını istemediğin yerde karşılıklı dik duvar bırakma.
- Örnek: `L_Proto_Dream` bacası, 4 m genişlik, 6 m tırmanış.

### Kanca (Zincirli Kama)

- `PF_GrapplePoint`'i `--- GAMEPLAY --- / GrapplePoints` altına koy. Çarpıştırıcısı yoktur.
- Nokta, oyuncunun gözünden 3-28 m uzaktaysa, ekranın ortasına ~14° yakınsa ve arada `Environment` geometrisi yoksa
  hedeflenir. Oyuncuyla nokta arasındaki düz yol da boş olmalı: çekilirken bir duvara sürtünen oyuncu kancayı bırakır.
- Oyuncu 21 m/s ile çekilir, noktaya 2.2 m kala bırakır ve ~13 m/s ile ileri-yukarı fırlar. İniş yeri noktanın en az
  2 m altında olsun: 2 m aşağıda ~3-5 m, 6.5 m aşağıda ~8-10 m öteye düşülür. İniş yerini bu aralığı kapsayacak kadar
  uzun yap.
- Çekilirken başka bir noktaya bakıp E'ye basmak doğrudan ona zincirler. Zincir zorunlu olsun istiyorsan ikinci nokta
  her kalkış yerinden 28 m'den uzak ya da görüş dışında olmalı.
- Örnek: 6 m ve 7.5 m yükseklikte, 10 m arayla iki nokta 22 m'lik boşluğu geçirir; iniş platformu ikinci noktanın 7 m
  ötesinde ve 6.5 m aşağısında. Burada ikinci nokta köprüden de görünür, yani zincir isteğe bağlıdır.

## Oyun nesneleri

Hepsi sahnede `--- GAMEPLAY ---` altına girer. `PF_Player` `Prefabs/Characters/`, diğerleri `Prefabs/Gameplay/`
altındadır.

- **Başlangıç:** oyuncu `PF_Player`'ın durduğu yerden ve baktığı yönden başlar.
- **`PF_Checkpoint`** (6×3×2 m tetikleyici, `Checkpoints` alt grubunda): oyuncu içinden geçince yeniden doğma noktası
  olur. Oyuncu onun konumunda ve mavi okunun (Z ekseni) yönüne bakarak doğar. Her zor geçişten sonra ve her arenadan
  önce bir tane koy. Geçit 6 m'den genişse ölçeğini büyüt ki yanından geçilmesin.
- **`PF_KillVolume`** (`KillVolumes` alt grubunda): bölümün tamamının altında tek büyük tetikleyici. Düşen oyuncuyu son
  kontrol noktasına döndürür. Düşülebilecek her yerin altını kaplamalı. Örnek: y = -15, ölçek 300×10×420.
- **`PF_Goal_DreamToilet`**: bölüm sonu. Tuvaletin kendisi katıdır; 3.6×3×2.6 m'lik tetikleyici tuvaletin önündedir
  (modelin -Z tarafı). Prefab'ı tuvaletin önü oyuncunun geldiği yöne bakacak şekilde döndür.

## Düşman arenaları

- Her arena `--- ENEMIES ---` altında `Arena_<Harf>` grubudur: transform sıfırda, katman `Ignore Raycast`, üstünde
  tetikleyici bir `BoxCollider` ve `EnemyArena` bileşeni. Kutunun `Center` ve `Size` değerleri arenanın zeminini ve
  üstündeki boşluğu (kasa gibi yükseltiler dahil) kaplar. Düşmanlar bu grubun altına konur. En kolayı, çoğalttığın
  sahnedeki bir arena grubunu kopyalayıp kutusunu yeniden boyutlandırmaktır.
- Düşmanlar kutudan çıkmaz. Oyuncu kutuya girince ve 22 m içindeyse (palyaço 26 m) savaşa başlarlar. Oyuncu kutudan
  4 m'den fazla ya da o düşmandan yatayda 22 m'den (palyaço 26 m) fazla uzaklaşınca bırakırlar. Aynı anda en fazla
  2 düşman saldırır. Saldırgan sınırı ve 4 m payı `EnemyArena` üzerinden, algılama mesafesi `Data/Enemies/` altındaki
  dosyalardan ayarlanır.
- Düşmanlar yol bulma kullanmaz: oyuncuya düz yürür ve ~1 m'den derin düşüşlerin kenarında durur. Arenanın ortasını
  açık bırak, büyük engelleri kenarlara koy. Basamak çıkmaları da muhtemelen takılır (oyunda denenmedi): arena zeminini
  tek seviyede tut, gerekirse rampa kullan.
- Palyaçolar oyuncuyu görmeden turta atmaz. Yüksek bir yerde iyi çalışırlar (örnek: Arena B'deki 4.1 m'lik çamaşır makineleri).
- Denenmiş boyutlar: 24×32 m (3 işçi ve 1 kalkan) ve 26×40 m (2 işçi, 1 kalkan, 2 palyaço).

## ProBuilder

ProBuilder 6'da ayrı bir ProBuilder penceresi yoktur; araçlar Scene görünümündedir.

- Yeni şekil: Scene görünümündeki **Tools** overlay'inden ProBuilder şekil aracını seç (ipucu "Create Cube"; basılı
  tutunca diğer şekiller çıkar) ya da **Tools > ProBuilder > Editors > Create Shape > Cube** (Ctrl+Shift+K). Sonra
  Scene'de tabanı çizip yüksekliği ver. Hızlı yol: **GameObject > ProBuilder > Cube** sahneye 1×1×1 m'lik bir küp koyar.
- Yüz, kenar ve köşe düzenlemek için şekli seç ve **Tools** overlay'inin üstündeki araç bağlamını (Tool Context)
  ProBuilder'a çevir; köşe, kenar ve yüz modu **Tool Settings** overlay'indedir, işlemler sağ tık menüsündedir.
- Ölçüleri yuvarlak tut: Scene görünümünün üst çubuğundaki ızgara yakalamayı aç (0.5 m veya 1 m adım).
- Her yeni şekilde: katmanı `Environment` yap, uygun `M_Proto_` materyalini ver, anlamlı bir ad ver ve doğru
  `Section_` grubuna taşı.
- Çok tekrar eden bir parça prefab olur: `Prefabs/Environment/` altında `PF_` önekiyle.

## Hazır dekor (rüyadaki apartman kiti)

`Prefabs/Environment/PF_Prop_...` prefab'ları sahneye sürüklenip kullanılır. Eşyalar gerçeğin 4-5 katı büyüklüğündedir;
oyuncu yanlarında fare boyunda kalır. Çarpıştırıcıları hazırdır ve `Environment` katmanındadır. Modeller -Z'ye bakar:
önü sana dönsün istiyorsan sahnede Y'de döndür. Oyuncunun üstüne çıkabileceği yükseklikler:

| Prefab | Üst yüzey | Not |
| --- | --- | --- |
| `PF_Prop_BedroomSet` | Yatak 3.1 m, komodin 2.8 m | Zıplayıp tırmanılır |
| `PF_Prop_GiantCouch` | Oturak 3.5 m, kolçak 3.6 m, sırt 4.3 m | Oturağa zıplayıp tırmanılır |
| `PF_Prop_WashingMachine` | 4.1 m | Zıplayarak tırmanmaya yetmez (en fazla ~3.9 m) |
| `PF_Prop_MailboxRow` | 4.8 m | Engel veya kanca ile |
| `PF_Prop_KitchenCounter` | Tezgâh 5.5 m, üst dolaplar 7.5-11.2 m | Engel veya kanca ile |
| `PF_Prop_GiantFridge` | 9 m | Engel, uzaktan görünen işaret |
| `PF_Prop_StairRailing` | 10 basamakla 8.5 m, sahanlık | Rampa gibi yürünür (ilk basamakta 40°, sonra 31°); çarpıştırıcı ilk basamağın önüne taşmaz |
| `PF_Prop_ApartmentDoor` | 27 m uzunluk, 14 m yükseklik | Düz bir duvar yüzeyi; zemin olan yerde duvar koşusuna uygun. Paspaslar zemine yatık durur, boşluk üstünde havada kalır. Duvarın önüne koy: panelin yüzü duvar yüzünün 3 cm önünde olsun, uçları yandaki duvarların içine girsin |
| `PF_Prop_BathroomDoor` | Kasa 6 m, açıklık 4.5 m | Kapı kanadı menteşe tarafında (-X) içeri (+Z) açık; düz geçiş ~2.7 m. Bir duvar boşluğuna yerleştir |

`PF_Prop_BathroomMirror`, `PF_Prop_CurtainRail` ve `PF_Prop_Clothesline` yalnızca görseldir, çarpıştırıcıları yoktur.
Perde ve çamaşır ipinin pivotu tepededir; asıldıkları yükseklikten aşağı sarkarlar.

## Deneme ve teslim

- Play'e bas ve bölümü baştan sona oyna. Kontrol listesini F1 (gamepad: Select) açar; Esc duraklatma menüsüdür.
  Konsolda hata olmamalı.
- Sahneyi ve `.meta` dosyasını birlikte commit et; Bake Sky Reflection çalıştırdıysan `Art/Textures/T_Env_<Sahne>.exr`
  ve `.meta` dosyasını da ekle. Dalını push edip `main`'e PR aç.
- Bölüm oynanabilir hale gelince build profile'ın sahne listesine eklenmesi için haber ver. Menüler sahneleri adıyla
  yükler; listede olmayan bir sahneye geçilemez.

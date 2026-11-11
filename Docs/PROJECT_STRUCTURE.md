# Fever Dream: Proje Yapısı ve Adlandırma

Bu belge proje yapısının **tek doğru kaynağıdır**. Bir şey oluşturmadan, taşımadan veya yeniden adlandırmadan önce
buraya bakın. Yapı değişirse bu belge aynı değişiklikte güncellenir. Kurallar `CLAUDE.md`'deki altın kurala dayanır:
editörü açan herkes aradığını kolayca bulabilmelidir.

## Klasörler

Projeye ait her şey `Assets/_Project/` altındadır. Dış paketler ve hazır asset'ler `Assets/ThirdParty/` altına girer.
`Assets/Settings/` Unity'nin URP ayarlarıdır ve yerinde kalır.

```
Assets/
  _Project/
    Art/
      Materials/            M_
        Palette/            modellerin ortak renk paleti (M_FD_...)
        Prototype/          blockout ve oynanış renk dili (M_Proto_...), modeli henüz olmayanların yer tutucuları
        World/              dünya yüzeyleri ve gökyüzü (M_World_..., M_Sky_...)
        VFX/                efekt materyalleri (M_FX_...)
      Models/               SM_ (statik), SK_ (iskeletli)
        Characters/  Props/  Weapons/
      Textures/             T_ (T_FX_... efekt dokusu, T_Env_<Sahne> o sahnenin gökyüzü yansıması)
      Animations/           A_ (klip), AC_ (animator controller)
      VFX/                  FX_ (parçacık prefab'ları)
      PostProcessing/       VP_ (volume profile)
      Shaders/              SH_
    Audio/
      Music/                MUS_
      SFX/                  SFX_
      VO/                   VO_
    Data/                   SO_ (ScriptableObject verileri)
      Player/  Combat/  Aura/  Enemies/  AlterEgo/
    Input/                  IA_ (Input Actions)
    Prefabs/                PF_
      Characters/  Enemies/  Weapons/  Environment/  Gameplay/  UI/
    Scenes/
      Levels/               L_  (oynanabilir bölümler)
      Menus/                MENU_ (ana menü)
      Sandbox/              SB_ (deneme sahneleri, build'e girmez)
    Scripts/
      Runtime/              FeverDream.Runtime assembly
        Core/  Player/  Combat/  Aura/  Enemies/  AlterEgo/  UI/  VFX/
      Editor/               FeverDream.Editor assembly
        Import/  Setup/
    UI/                     UI sprite ve fontları (UI_)
      Fonts/  Sprites/
  ThirdParty/
  Settings/                 URP (Unity'nin kendi ayarları)
Docs/                       GDD, yapı, plan ve blockout belgeleri (Assets dışında)
```

**Kurallar**
- `Assets/` kökünde ve `_Project/` kökünde dağınık dosya olmaz.
- `New Folder`, `Untitled`, `_copy`, `_old`, `Test2` gibi adlar bırakılmaz.
- Asset'ler her zaman Unity içinden (veya Unity MCP asset araçlarıyla) taşınır; `.meta` dosyası asset'iyle birlikte commit edilir.

## Asset adlandırma

`Önek_Alan_Ad_Varyant`, PascalCase, boşluksuz, Türkçe karaktersiz. Örnek: `PF_Enemy_ToiletLidShield`,
`M_Proto_WallRun`, `SO_Enemy_ClownThrower`, `L_Proto_Dream`.

| Tür | Önek | Tür | Önek |
| --- | --- | --- | --- |
| Prefab | `PF_` | ScriptableObject verisi | `SO_` |
| Materyal | `M_` | Input Actions | `IA_` |
| Doku | `T_` | Volume profile | `VP_` |
| Statik model | `SM_` | İskeletli model | `SK_` |
| Animasyon klibi | `A_` | Animator controller | `AC_` |
| VFX | `FX_` | UI sprite | `UI_` |
| Ses efekti | `SFX_` | Seslendirme | `VO_` |
| Müzik | `MUS_` | Shader | `SH_` |
| Bölüm sahnesi | `L_` | Sandbox sahnesi | `SB_` |
| Menü sahnesi | `MENU_` | | |

Kod dosyaları önek almaz: dosya adı = sınıf adı (`PlayerMovement.cs`).

## Sahne hiyerarşisi

Her sahnede kök seviyede yalnızca şu grup nesneleri bulunur (transform'ları sıfırlanmış boş nesneler):

```
--- SETUP ---         ışık, global volume, sahne ayarları; oyuncusu olmayan sahnelerde (menü) kamera
--- ENVIRONMENT ---   bölüm geometrisi, bölüm bölüm alt gruplar (Section_01_Bedroom, ...)
--- GAMEPLAY ---      oyuncu, alter ego, kontrol noktaları, kanca noktaları, ölüm alanları, hedef
--- ENEMIES ---       arenalara göre gruplanmış düşmanlar (Arena_A, Arena_B, ...)
--- UI ---            HUD, duraklatma menüsü (PauseMenu) ve EventSystem
--- RUNTIME ---       yalnızca oyun sırasında kod tarafından oluşturulur (mermiler vb.)
```

- Bir bölüme ait ışıklar (`Light_Kitchen` gibi) o bölümün grubunda durur; `--- SETUP ---` yalnızca sahne geneli ışıkları
  (`Light_Moon`, `Light_Fill`), volume'u ve gerekirse kamerayı tutar.

- Sahne nesneleri önek almaz, açıklayıcı PascalCase ad ve gerekirse numara alır: `Wall_Corridor_Left`,
  `Prop_GiantFridge_01`, `Enemy_AlarmClockWorker_02`, `Light_Moon`, `Volume_Global`.
- `GameObject (1)`, `Cube (3)`, `Directional Light` gibi boşluklu varsayılan adlar bırakılmaz.
- `--- ENEMIES ---` altındaki her arena grubu bir `EnemyArena` bileşeni ve tetikleyici kutu taşır. Grubun transform'u
  orijinde kalır, kutunun `center` ve `size` değerleri arenayı kaplar. Düşmanlar bu kutudan çıkmaz ve yalnızca oyuncu
  içerideyken savaşır.
- Tekrar kullanılan her şey prefab'dır. Büyük bölümler ileride ekip paralel çalışabilsin diye additive alt sahnelere bölünür.

## Kod

- Namespace klasörle eşleşir: `FeverDream.Core`, `FeverDream.Player`, `FeverDream.Combat`, `FeverDream.Aura`,
  `FeverDream.Enemies`, `FeverDream.AlterEgo`, `FeverDream.UI`, `FeverDream.VFX`, editör araçları `FeverDream.EditorTools.*`
  (`FeverDream.Editor` kullanılmaz: Unity'nin `UnityEditor.Editor` türünü gölgeler).
- Dosya başına bir üst seviye tür; dosya adı = tür adı.
- Ayarlanabilir oynanış değerleri ScriptableObject'lerde (`Data/` altında) tutulur ki tasarımcılar koda dokunmadan ayar
  yapabilsin: `SO_PlayerMovement_Default`, `SO_PlayerCamera_Default`,
  `SO_PlayerCombat_Default` (can, kaçınma, hasar, yeniden doğma), `SO_Aura_Default`, `SO_Tsunagi_Katana`,
  `SO_Enemy_*` (palyaço turtasının uçuşu dahil), `SO_AlterEgo_Pigeon`.
  Prefab bileşenlerinde yalnızca referanslar ve görsel ayarlar (renkler, pozlar) kalır. Sahneye özgü ayarlar
  (`EnemyArena` üzerindeki saldırgan sınırı ve savaşı bırakma payı) sahnede durur.
- Bir ayar dosyası atanmamışsa bileşen konsola hata yazar ve kendini kapatır (sessiz `NullReference` yerine).
- Veri olarak kaydedilen enum'lara (ör. `AlterEgoEvent`) sabit sayı verilir; yeni değer yeni sayı alır, mevcutlar değişmez.
- Sahneler arası bağımlılık için `FindObjectOfType` yerine bilinen erişim noktaları kullanılır (`PlayerContext.Current`).

## Katmanlar (Layers)

| No | Ad | Kullanım |
| --- | --- | --- |
| 6 | Player | Oyuncu kapsülü |
| 7 | Enemy | Düşman gövdeleri (Tsunagi'nin vurduğu katman) |
| 8 | Environment | Yürünebilir ve parkur yapılabilir geometri (duvar koşusu, tutunma, görüş hattı) |
| 9 | GrapplePoint | Zincirli Kama kanca noktaları. Çarpıştırıcıları yoktur; hedefleme `GrapplePoint` listesiyle yapılır. Katman, ileride eklenecek tetikleyiciler için ayrılmıştır ve hiçbir katmanla çarpışmaz |
| 10 | Projectile | Düşman mermileri (yalnızca Player ve Environment ile çarpışır) |

Düşman arenalarının tetikleyici kutuları `Ignore Raycast` (2) katmanındadır. Katman adları ve çarpışma kuralları
**Fever Dream > Setup > Create Missing Data** tarafından da ayarlanır; araç her çalıştığında 9 ve 10 numaralı katmanların
çarpışma kurallarını yeniden yazar. Değiştirirseniz bu tabloyu ve `ProjectDataSetup.ConfigureLayers`'ı birlikte güncelleyin.

## Sanat içe aktarma

- Modeller `Art/Models/` altında türüne göre (`Characters/`, `Props/`, `Weapons/`) `SM_` önekiyle durur. Dosya adı
  oyundaki adı taşır (`SM_Enemy_AlarmClockWorker`), kaynak dosyanın adını değil.
- `Scripts/Editor/Import/ModelImportRules.cs`, `Art/Models/` altına ilk kez giren her modeli aynı ayarlarla içe aktarır:
  kamera ve ışık yok, animasyon yok, birim metre, eksen dönüşümü kalıcı, çarpıştırıcı yok. Dosyadaki `FD_X` adlı
  materyaller `Art/Materials/Palette/M_FD_X` ile değiştirilir; kitin bütün modelleri bu tek paleti paylaşır. Ayarlar
  yalnızca ilk içe aktarmada yazılır, sonra Inspector'da yapılan değişiklikler korunur.
- Kit modelleri -Z yönüne bakar. Karakter prefab'larında model 180° döndürülür (düşmanlar +Z'ye bakar); modelin kendi
  kök dönüşü korunur, üstüne eklenir.
- Dekor prefab'ları `Prefabs/Environment/PF_Prop_...` adını taşır: kök `Environment` katmanındadır, model görsel olarak
  durur, oynanışa giren yüzeyler `Collider_...` adlı kutu çarpıştırıcılardadır (çıkıntılar duvar koşusunu bozmasın diye).
  Çarpıştırıcı modelin görünen sınırını aşmaz; oyuncu görmediği bir şeye takılmamalıdır. Eğik bir kutunun ucu
  eğimine dik olduğu için ileri taşar: eğik kutu modelin içinde başlatılır, başlangıcı dik bir kutuyla kapatılır
  (`PF_Prop_StairRailing`'deki `Collider_RailPost` ve `Collider_RailStart` gibi). Modeldeki küçük bir boşluğu kapatan
  görsel parça `Patch_...` adını alır (`PF_Prop_BedroomSet` > `Patch_HeadboardGap`; `PF_Prop_StairRailing` >
  `Patch_RunnerEdges`: halı basamak kenarının üstünden kesilmeden geçsin diye her basamağa bir parça).
- `PF_Prop_StairRailing`'de `StairRailing_Posts` ve `StairRailing_Rail` 10 cm öne alınmıştır: ilk basamağın kenar
  çıtası baş direğin önünden geçmez, direğin içinde biter.
- `PF_Prop_BathroomDoor`'da kitin ışık parçası (`BathroomDoor_Light`) kapalıdır: opaktı ve kapının 1.4 m arkasında
  duruyordu. Yerine kapı hizasında `Glow_Doorway` ve zeminde `Glow_DoorwayFloor` vardır; ikisi de
  `Art/Materials/VFX/M_FX_DoorGlow` (saydam, toplamalı) kullanır: kapı parlar ama içerisi görünür.
- Duvar paneli olan dekor (`PF_Prop_ApartmentDoor`) blockout duvarının içine değil önüne konur: panelin yüzü duvar
  yüzünün birkaç santim önünde durur, yoksa kapılar duvarın içinde kalır. Panelin ucu başka bir duvarın yüzeyiyle aynı
  düzlemde bırakılmaz (iki yüzey titreşir); uç, o duvarın içine sokulur.
- Sanatçıya not: Blender'dan FBX alırken "Apply Transform" kapalı olsun. Açıkken bazı alt parçaların dönüşü iki kez
  uygulanıyor (`SM_Prop_ApartmentDoor`'daki kapılar, `SM_Prop_BathroomDoor`'daki havlu askısı); bu parçalar şimdilik
  prefab'da düzeltildi.
- Sanatçıya not: aynı düzlemde üst üste binen yüzeyler titreşir (z-fighting). `SM_Prop_BedroomSet`'in yatak başlığında
  bulut biçimli parçalarla arkalarındaki tahta aynı düzlemdeydi ve aralarında tırtıklı bir bant görünüyordu. Şimdilik bu
  modelin normalleri Unity'de yeniden hesaplanıyor (Model sekmesi: Normals = Calculate, Smoothing Angle = 45, Smoothing
  Source = From Angle): iki yüzey aynı ışığı aldığı için bant görünmüyor. Kalıcı çözüm modeldedir: parçalar tahtayla
  birleştirilmeli ya da birkaç milimetre öne alınmalı. Yeni bir modelde aynı bant görülürse önce modele bakılır.
  Aynı başlıkta ortadaki iki bulut parçasının arasında 11×14 cm'lik bir boşluk da var (arkası görünüyordu); prefab'da
  `Patch_HeadboardGap` ile kapatıldı, modelde de kapatılmalı.

## Dünya görseli

Rüya gecesi: mor-mavi tonlar, pembe ufuk, hilal ay. Yeni bir sahne aynı parçaları kullanır; en kolayı `L_Proto_Dream`'i
çoğaltmaktır.

- **Gökyüzü:** `Art/Materials/World/M_Sky_Dream` (`SH_Sky_Dream`, "FeverDream/Dream Sky"); Lighting > Environment >
  Skybox Material'a atanır. Renk geçişi, yıldızlar, bulutsu ve hilal ay shader'da üretilir, doku yoktur. Ay, sahnenin ana
  yönlü ışığının geldiği yönde çizilir: ayı taşımak için `Light_Moon` döndürülür, gölgeler kendiliğinden uyar.
- **Yüzeyler:** blockout küpleri `SH_WorldSurface` ("FeverDream/World Surface") kullanan materyalleri alır:
  `M_World_Floor` (dama zemin, hafif parlaklık), `M_World_Wall` (çizgili duvar kâğıdı), `M_World_WallRun` (parlayan
  camgöbeği çizgiler: üstünde koşulan duvar). Desen dünya konumundan hesaplanır: UV gerekmez, küp ölçeklenince desen
  uzamaz, ProBuilder şekillerinde de çalışır. Yüzey, en çok baktığı dünya eksenine göre izdüşürülür (kutu eşlemesi):
  eksenlere dik yüzeylerde ölçü tamdır, 45° döndürülmüş bir duvarda desen en fazla 1.4 kat uzar, eğri şekillerde
  izdüşüm ekseninin değiştiği yerde dikiş görünür. Aynı shader'dan yeni materyal türetilebilir (desen: yok, ızgara,
  dama, çizgi; boyut metre, açı derece). Shader yalnızca dünya uzayını kullanır: statik batching nesne uzayını yok
  ettiği için nesnenin kendi konumuna bağlı efekt eklenmez.
- **Işık:** `--- SETUP ---` altında `Light_Moon` (soğuk lavanta, yumuşak gölge) ve `Light_Fill` (pembe, gölgesiz dolgu,
  ayın karşı tarafından). Ortam ışığı Trilight (mavi-mor), sis üsteldir ve rengi ufka yakın mor-pembedir. Bölüm ışıkları
  (`Light_Kitchen` gibi) gölgesiz nokta ışıktır.
- **Yansımalar:** parlak yüzeyler (ayna, metal) gökyüzünü `Art/Textures/T_Env_<Sahne>.exr` dokusundan yansıtır
  (Lighting > Environment > Environment Reflections: Custom). Dokuyu **Fever Dream > Setup > Bake Sky Reflection** açık
  sahne için üretir ve sahneye atar. Gökyüzü materyali ya da `Light_Moon`'un yönü değişince yeniden çalıştırılır.
  `M_Sky_Dream` sahneler arasında ortaktır, doku ise sahneye özeldir: materyal değişince komut onu kullanan her
  sahnede (`L_Proto_Dream`, `MENU_Main`, çoğaltılan bölümler) ayrı ayrı çalıştırılır. Üretilen `T_Env_<Sahne>.exr` ve
  `.meta` dosyası sahneyle aynı commit'e girer. Çoğaltılan bir sahne, kendi dokusu üretilene kadar kaynağının dokusunu
  kullanır; sıfırdan kurulan bir sahnede komut çalıştırılmazsa yansımalarda Unity'nin varsayılan gündüz gökyüzü görünür.
- **Post-processing:** `Art/PostProcessing/VP_Dream` (bloom, vinyet, renk, split toning, hafif renk sapması, ACES).
  Değerler değişirse `ProjectDataSetup.CreateVolumeProfile` içindeki ilk değerler de güncellenir.
- **Zerrecikler:** `Art/VFX/FX_DreamMotes` (materyal `M_FX_Mote`, doku `T_FX_SoftDot`) havada süzülen ışık noktalarıdır.
  `PF_Player`'ın içindedir ve dünya uzayında simüle edilir (zerrecikler doğdukları yerde kalır). Doğma kutusu oyuncunun
  çevresinde 80 m'lik bir karedir: oyuncu nereye dönerse dönsün ve koşarken de çevresi dolu kalır. `AmbientParticles`
  bileşeni ışınlanmadan (yeniden doğma) sonra bulutu yeni yerde yeniden doldurur. Oyuncusu olmayan sahnelere (menü)
  elle konur.

## Arayüz stili

Arayüz manga/anime aksiyon stilindedir: eğik paneller, kalın koyu dış çizgi, sert ofset gölge, ekran tonu noktaları,
kombo için patlama, alter ego için konuşma balonu. Yeni bir ekran eklerken aynı parçalar kullanılır.

- **Yazı tipi:** başlık, düğme ve sayılar `UI/Fonts/UI_Font_Bangers` (Google Fonts, SIL OFL 1.1; lisans dosyası yanında).
  Türkçe harflerin hepsi var, ama yalnızca büyük harf çizer: Türkçe metin elle büyük harfle yazılır (`BİR`, `BEKLİYOR`),
  koddan gelen metin `HudView.TurkishUpper` gibi i→İ, ı→I dönüşümünden geçer. Uzun cümleler (alter ego repliği)
  Unity'nin yerleşik yazı tipiyle kalır. Bangers satırları yerleşik yazı tipinden uzundur; metinlerde dikey taşma açıktır.
- **Yazı efekti:** Bangers metinleri `UiStickerOutline` kullanır (8-12 yöne eşit dış çizgi ve çizgili şeklin sert
  gölgesi). Unity'nin `Outline` bileşeni yalnızca dört köşegen kopya çizer ve kalın çizgide tırtıklı görünür; metinde
  kullanılmaz. Bileşen sırası: `UiGradient`, `UiSkew`, `UiStickerOutline`.
- **Sprite'lar (`UI/Sprites/`):**
  - Beyaz dolgu ve gömülü koyu çizgi; Image rengi yalnızca dolguyu boyar: `UI_Panel_Sticker` (panel ve düğme, 9-slice),
    `UI_SpeechBubble` (kuyruk sol altta, 9-slice), `UI_Burst` (kombo patlaması), `UI_Burst_Wide` (Aura plakasının
    arkasından taşan geniş patlama; büyük ödemede görünür), `UI_Crosshair`, `UI_GrappleMarker`.
  - Çizgisiz, boyanır: `UI_Bar_Chip` (can bölmesi, üstünde parlaklık bandı), `UI_Halftone` (soldan sağa sönen ekran tonu),
    `UI_Halftone_Corner` (sol alt köşeden dışa doğru sönen ekran tonu).
  - Renkleri gömülü ikonlar, Image rengi beyaz kalır: `UI_Icon_Heart`, `UI_Icon_Sparkle`.
- **Ekran tonu kuralı:** `UI_Halftone` yalnızca bir panelin içinde, panelle aynı eğimde ve kenarlarından içeride
  kullanılır. Ekran köşelerinde `UI_Halftone_Corner` kullanılır; kenarlara varmadan söner, dikdörtgen izi bırakmaz. Başka
  köşeler için ölçek -1 ile çevrilir (pivot ortada kalmalı, yoksa görüntü panelin dışına taşar). Bir kartın köşesinde
  kullanılacaksa kartın içine sığmayan kısmı kesilmelidir: ton, kartla aynı sprite ve eğimde, grafiği gizli bir `Mask`
  taşıyan `ToneClip` nesnesinin altına konur (`PF_ControlsPanel`'deki gibi). Kesilmeyen ton kartın dışında keskin bir
  kenarla biter.
- **Efekt bileşenleri (`Scripts/Runtime/UI/`):** `UiSkew` (eğim), `UiGradient` (dikey renk geçişi),
  `UiPunchOnChange` (izlediği metin değişince zıplatır), `UiIdleFloat` (yavaş süzülme; LayoutGroup içindeki öğelerde
  kullanılmaz, düzeni bozar), `UiPulse` (nabız ve dönme), `UiSegmentedBar` (bölmeli çubuk: değer düşünce sarsılır, azken
  yanıp söner), `UiMenuButton` (fareyle üstüne gelmek düğmeyi seçer; seçili düğme büyür ve işaretçisini gösterir).
  `UiPulse` ile `UiPunchOnChange` aynı nesnede birlikte kullanılmaz; ikisi de ölçeği yazar.
  `UiRollingCounter` (hane hane dönen sayaç: dönerken yalnızca değişen haneler parlar ve zıplar; haneler dönerken sabit
  ızgarada, dururken yazı tipinin kendi aralığında durur), `UiFlyInLabel` (yerinde belirip kısa bir an bekleyen, sonra
  hedefine uçup kaybolan etiket), `UiImpact` (tek seferlik vuruş: `Play` ile başlangıç pozuna atlar ve prefab'daki
  pozuna oturur; `CanvasGroup` verilirse çalınana kadar gizlidir, bekler ve söner; güç değeri vuruşu küçültür).
  `UiImpact` ölçeği ve dönüşü yazar; `UiPulse` ve `UiPunchOnChange` ile aynı nesnede kullanılmaz. `UiRollingCounter`
  hanelerinin, `UiFlyInLabel` kendi nesnesinin konumunu ve ölçeğini yazar; bu nesnelere başka hareket efekti konmaz.
  Üçü de tuvali (Canvas) kapalıyken bekler: oyun duraklayınca HUD gizlenir, yarım kalan animasyon menüden dönünce
  kaldığı yerden sürer. Uzun süren tek bir kare (takılma) animasyonu yutmaz; adım 0.1 sn ile sınırlıdır.
- **Aura sayacı (`PF_HUD` > `Aura`):** toplam tek bir metin değildir; `Total` altındaki `Digit_01` (birler) …
  `Digit_06` hanelerinde durur. Her hane tek karakterlik Bangers metnidir (`UiSkew`, `UiStickerOutline`); soldaki hane
  hiyerarşide önce gelir. Kombo ödenince "+N" plakanın altında belirir ve sayıya uçar, sayı ödemenin büyüklüğüne göre
  0.35-0.8 sn döner. Sayı oturduğu anda `HudView` iniş efektlerini ödemenin gücüyle çalar: sayı ve yıldız zıplar, ekran
  tonu pembe parlar (`ToneHeat`), yaklaşık 95 Aura'dan büyük ödemede plakanın arkasında patlama (`Burst`) görünür.
  Toplam yeni bir alter ego kademesine (600, 1500) ulaşınca "AURA" etiketinin üstüne pembe bir damga basılır
  (`TierStamp`; yazıları `HudView` üzerindeki "Aura Tier Callouts" listesindedir). Boştayken sayaçta hiçbir şey
  kıpırdamaz. Gizli duran efekt nesnelerinin (`Burst`, `ToneHeat`, `TierStamp`) `CanvasGroup` alfası prefab'da 1'dir;
  `UiImpact` oyun başlarken gizler.
- **Çift sert gölge:** Aura plakasında macenta gölgenin arkasında camgöbeği ikinci bir `Shadow` vardır. İkinci `Shadow`
  birincisiyle aynı mesafede (7, -7) olmalıdır: `Shadow` sprite'ın gömülü koyu çizgisini de kopyalar, daha kısa
  mesafede camgöbeği bu çizginin altında kalır ve görünmez.
- **Menü düğmeleri:** her zaman tek bir düğme vurguludur. Fare, klavye ve gamepad aynı seçimi paylaşır (`UiMenuButton`):
  - Fare bir düğmenin üstünde oynayınca o düğme seçilir. Menü duran imlecin altında açılırsa seçim değişmez.
  - Düğmelerde Highlighted rengi Normal ile aynıdır; vurguyu yalnızca seçim gösterir.
  - Sahnedeki `EventSystem`'de "Deselect On Background Click" kapalıdır; boşluğa tıklamak seçimi silmez.
  - UI Cancel (Esc, gamepad B) yalnızca seçili nesneye gider; `UiMenuButton` bunu üstündeki menüye iletir.
  - Bir sayfanın düğmesi, yerine geçtiği sayfadaki bir düğmeyle aynı yere konmaz: ikinci tıklama alttakine gider.
- **Kontrol listesi:** `Prefabs/UI/PF_ControlsPanel` tek kaynaktır; ana menü ve duraklatma menüsü aynı prefab'ı gösterir.
  Satırlar prefab'da elle düzenlenir ve `IA_Player` ile uyumlu tutulur. Oyun ekranında (HUD) kontrol listesi yoktur.
- **Renkler:** mürekkep `#1F1238` (panel), erik `#5C339E` (düğme), pembe `#FF6BAD` (vurgu, can, seçili düğme), macenta
  `#D12485` (sert gölge), camgöbeği `#4DEBFF` (Aura, ikinci gölge), krem `#FAF2FF` (yazı), koyu `#120821` (dış çizgi).

## Editör araçları

- **Fever Dream > Setup > Create Missing Data** (`Scripts/Editor/Setup/ProjectDataSetup.cs`): katman adlarını ve çarpışma
  kurallarını, eksik `SO_` veri dosyalarını ilk değerleriyle ve `VP_Dream` volume profile'ını oluşturur. Var olan hiçbir
  asset'e, prefab'a veya sahneye dokunmaz. Katman adları yalnızca boşsa yazılır; başka bir ad kullanılıyorsa uyarır.
- **Fever Dream > Setup > Bake Sky Reflection** (`Scripts/Editor/Setup/SkyReflectionBaker.cs`): açık sahnenin
  gökyüzünü `Art/Textures/T_Env_<Sahne>.exr` küp dokusuna çizer ve sahnenin ortam yansıması yapar. Yalnızca gökyüzü
  çizilir; ışık haritası ya da başka bir şey üretilmez. Sonrasında sahne kaydedilir.
- Prefab'lar ve sahneler elle düzenlenir; artık kodla üretilmezler.
- Yeni bir sahne oynanabilir olunca build profile'ın sahne listesine eklenir. `MENU_Main` listede ilk sahnedir; menüler
  sahneleri adlarıyla yükler (`SceneFlow`).

Bölüm blockout'u ProBuilder ile yapılır. Kurallar ve oyuncunun ölçüleri `Docs/LEVEL_BLOCKOUT.md`'dedir.

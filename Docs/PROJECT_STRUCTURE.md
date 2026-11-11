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
      Models/               SM_ (statik), SK_ (iskeletli)
        Characters/  Props/  Weapons/
      Textures/             T_
      Animations/           A_ (klip), AC_ (animator controller)
      VFX/                  FX_
      PostProcessing/       VP_ (volume profile)
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
        Core/  Player/  Combat/  Aura/  Enemies/  AlterEgo/  UI/
      Editor/               FeverDream.Editor assembly
        Import/  Setup/
    UI/                     UI sprite ve fontları (UI_)
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

- Bir bölüme ait ışıklar (`Light_Kitchen` gibi) o bölümün grubunda durur; `--- SETUP ---` yalnızca sahne geneli ışığı,
  volume'u ve gerekirse kamerayı tutar.

- Sahne nesneleri önek almaz, açıklayıcı PascalCase ad ve gerekirse numara alır: `Wall_Corridor_Left`,
  `Prop_GiantFridge_01`, `Enemy_AlarmClockWorker_02`, `Light_Sun`, `Volume_Global`.
- `GameObject (1)`, `Cube (3)`, `Directional Light` gibi boşluklu varsayılan adlar bırakılmaz.
- `--- ENEMIES ---` altındaki her arena grubu bir `EnemyArena` bileşeni ve tetikleyici kutu taşır. Grubun transform'u
  orijinde kalır, kutunun `center` ve `size` değerleri arenayı kaplar. Düşmanlar bu kutudan çıkmaz ve yalnızca oyuncu
  içerideyken savaşır.
- Tekrar kullanılan her şey prefab'dır. Büyük bölümler ileride ekip paralel çalışabilsin diye additive alt sahnelere bölünür.

## Kod

- Namespace klasörle eşleşir: `FeverDream.Core`, `FeverDream.Player`, `FeverDream.Combat`, `FeverDream.Aura`,
  `FeverDream.Enemies`, `FeverDream.AlterEgo`, `FeverDream.UI`, editör araçları `FeverDream.EditorTools.*`
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
- Sanatçıya not: Blender'dan FBX alırken "Apply Transform" kapalı olsun. Açıkken bazı alt parçaların dönüşü iki kez
  uygulanıyor (`SM_Prop_ApartmentDoor`'daki kapılar, `SM_Prop_BathroomDoor`'daki havlu askısı); bu parçalar şimdilik
  prefab'da düzeltildi.

## Editör araçları

- **Fever Dream > Setup > Create Missing Data** (`Scripts/Editor/Setup/ProjectDataSetup.cs`): katman adlarını ve çarpışma
  kurallarını, eksik `SO_` veri dosyalarını ilk değerleriyle ve `VP_Dream` volume profile'ını oluşturur. Var olan hiçbir
  asset'e, prefab'a veya sahneye dokunmaz. Katman adları yalnızca boşsa yazılır; başka bir ad kullanılıyorsa uyarır.
- Prefab'lar ve sahneler elle düzenlenir; artık kodla üretilmezler.
- Yeni bir sahne oynanabilir olunca build profile'ın sahne listesine eklenir. `MENU_Main` listede ilk sahnedir; menüler
  sahneleri adlarıyla yükler (`SceneFlow`).

Bölüm blockout'u ProBuilder ile yapılır. Kurallar ve oyuncunun ölçüleri `Docs/LEVEL_BLOCKOUT.md`'dedir.

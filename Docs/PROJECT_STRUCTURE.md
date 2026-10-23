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
      Materials/            M_  (prototip materyalleri: Materials/Prototype/)
      Models/               SM_ (statik), SK_ (iskeletli)
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
      Sandbox/              SB_ (deneme sahneleri, build'e girmez)
    Scripts/
      Runtime/              FeverDream.Runtime assembly
        Core/  Player/  Combat/  Aura/  Enemies/  AlterEgo/  UI/
      Editor/               FeverDream.Editor assembly
    UI/                     UI sprite ve fontları (UI_)
  ThirdParty/
  Settings/                 URP (Unity'nin kendi ayarları)
Docs/                       GDD, yapı ve plan belgeleri (Assets dışında)
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

Kod dosyaları önek almaz: dosya adı = sınıf adı (`PlayerMovement.cs`).

## Sahne hiyerarşisi

Her sahnede kök seviyede yalnızca şu grup nesneleri bulunur (transform'ları sıfırlanmış boş nesneler):

```
--- SETUP ---         ışık, global volume, sahne ayarları
--- ENVIRONMENT ---   bölüm geometrisi, bölüm bölüm alt gruplar (Section_01_Start, ...)
--- GAMEPLAY ---      oyuncu, alter ego, kontrol noktaları, kanca noktaları, ölüm alanları, hedef
--- ENEMIES ---       arenalara göre gruplanmış düşmanlar (Arena_A, Arena_B, ...)
--- UI ---            HUD ve menüler
--- RUNTIME ---       yalnızca oyun sırasında kod tarafından oluşturulur (mermiler vb.)
```

- Sahne nesneleri önek almaz, açıklayıcı PascalCase ad ve gerekirse numara alır: `Wall_Corridor_Left`,
  `Prop_GiantFridge_01`, `Enemy_AlarmClockWorker_02`.
- `GameObject (1)`, `Cube (3)` gibi varsayılan adlar bırakılmaz.
- Tekrar kullanılan her şey prefab'dır. Büyük bölümler ileride ekip paralel çalışabilsin diye additive alt sahnelere bölünür.

## Kod

- Namespace klasörle eşleşir: `FeverDream.Core`, `FeverDream.Player`, `FeverDream.Combat`, `FeverDream.Aura`,
  `FeverDream.Enemies`, `FeverDream.AlterEgo`, `FeverDream.UI`, editör araçları `FeverDream.Editor.*`.
- Dosya başına bir üst seviye tür; dosya adı = tür adı.
- Ayarlanabilir değerler ScriptableObject'lerde (`Data/` altında) tutulur ki tasarımcılar koda dokunmadan ayar yapabilsin.
- Sahneler arası bağımlılık için `FindObjectOfType` yerine bilinen erişim noktaları kullanılır (`PlayerContext.Current`).

## Katmanlar (Layers)

| No | Ad | Kullanım |
| --- | --- | --- |
| 6 | Player | Oyuncu kapsülü |
| 7 | Enemy | Düşman gövdeleri (Tsunagi'nin vurduğu katman) |
| 8 | Environment | Yürünebilir ve parkur yapılabilir geometri (duvar koşusu, tutunma, görüş hattı) |
| 9 | GrapplePoint | Zincirli Kama kanca noktaları (tetikleyici) |
| 10 | Projectile | Düşman mermileri (yalnızca Player ve Environment ile çarpışır) |

## Prototip araçları

`Scripts/Editor/Prototype/PrototypeContentBuilder.cs`, prototipin yer tutucu materyal, veri, prefab ve test sahnesini
üretir (menü: **Fever Dream > Prototype**). "Build Missing" yalnızca eksikleri oluşturur, ayarlarınızı ezmez.
"Rebuild" prefab'ları ve `L_Proto_Dream` sahnesini baştan yazar; elle yapılan sahne düzenlemeleri kaybolur.

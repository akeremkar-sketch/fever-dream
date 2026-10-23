using FeverDream.AlterEgo;

namespace FeverDream.EditorTools.Prototype
{
    /// <summary>
    /// First pass of the pigeon's lines, written into SO_AlterEgo_Pigeon when it is created.
    /// The writers own the asset from then on; edit the lines there, not here.
    /// </summary>
    public static class PrototypeAlterEgoLines
    {
        public static void Fill(AlterEgoLines lines)
        {
            lines.speakerName = "Güvercin";
            lines.lineDuration = 3.5f;
            lines.cooldown = 2.5f;
            lines.bigComboPoints = 150;
            lines.hugeComboPoints = 400;
            lines.lineSets = new[]
            {
                Set(AlterEgoEvent.LevelStart, 1f, true,
                    new[] { "Yine mi rüya? Tuvaleti bulana kadar seni izleyeceğim. Zevkle.", "Jack, mesanen bir anime finalinden daha gergin. Koş." },
                    null, null),
                Set(AlterEgoEvent.ComboSmall, 0.6f, false,
                    new[] { "Buna kombo mu diyorsun? Kumrular bile daha iyi kanat çırpar.", "Vay. Birkaç vuruş. Tarihe geçtin.", "Aura mı kazandın, yoksa sadece terledin mi?" },
                    new[] { "Hmm. Fena değil. Yani... fena.", "Bu Aura işi biraz fazla oluyor ama, değil mi?" },
                    new[] { "Dur. Her vuruşta biraz daha siliniyorum gibi.", "Jack, ben buradayım, unutma beni!" }),
                Set(AlterEgoEvent.ComboBig, 1f, false,
                    new[] { "Tamam, kabul. Bu biraz havalıydı. Biraz.", "Kafanda anime açılış şarkısı mı çalıyor? Buradan duyabiliyorum." },
                    new[] { "Bu kadar Aura sağlığa zararlı. Magnezyum takviyesi gibi: doktor kontrolünde!", "Sesim neden kısılıyor? Jack?" },
                    new[] { "Kanatlarım şeffaflaşıyor! Bu normal mi?!", "Lütfen biraz sıradan ol. Bir kerecik." }),
                Set(AlterEgoEvent.ComboHuge, 1f, true,
                    new[] { "TAMAM! Sen bir anime kahramanısın. Mutlu musun?", "Bu kombo uçan diskimin motorunu ısıttı." },
                    new[] { "Bu kadar parlarsan beni kim hatırlayacak?", "Aura göstergesi patladı. Ben de patlamak üzereyim." },
                    new[] { "JACK! Ben senin iç sesinim! Beni silme!", "Disk... düşüyor... Aura çok fazla!" }),
                Set(AlterEgoEvent.ComboBroken, 0.8f, false,
                    new[] { "Ve kombo pencereden uçtu. Ben uçarım, kombo uçmaz.", "Darbe aldın, Aura gitti. Klasik Jack." },
                    new[] { "Kaybettin. Bir yanım rahatladı, nedenini bilmiyorum.", "Bu acıttı mı? Beni biraz rahatlattı." },
                    new[] { "Güzel! Yani... kötü! Ama bana iyi geldi!", "Bir nefes aldım. Teşekkürler, düşman." }),
                Set(AlterEgoEvent.FellIntoVoid, 1f, true,
                    new[] { "Boşluğa düştün. Tuvalet orada değil, emin ol.", "Yerçekimi bir, Jack sıfır." },
                    new[] { "Hiçliğe bakınca ben de geri baktım. Pek hoş değildi.", "Düşerken bağırmadın. Gelişme var." },
                    new[] { "Boşluk bana her seferinde daha yakın geliyor...", "Aşağıda biri vardı. Sanırım bendim." }),
                Set(AlterEgoEvent.PlayerDied, 1f, true,
                    new[] { "Öldün. Rüyada. Tuvaletsiz. Şiirsel.", "Bir ofis çalışanından dayak yedin. Gerçek hayat gibi." },
                    new[] { "Kalk hadi. Bensiz ölemezsin, değil mi?" },
                    new[] { "Ölünce bir an eski hâlime döndüm. Güzeldi." }),
                Set(AlterEgoEvent.PerfectDodge, 1f, true,
                    new[] { "Ooo, son anda! Neredeyse etkilendim.", "Matrix izledin, değil mi?" },
                    new[] { "Bu refleksler nereden çıktı? Beni korkutuyorsun." },
                    new[] { "Çok hızlısın, sana yetişemiyorum!" }),
                Set(AlterEgoEvent.EnemyKilled, 0.25f, false,
                    new[] { "Bir mesai daha bitti.", "Alarm sustu. Huzur." },
                    new[] { "Bir tane daha. Sayıyorum, bilmiş ol." },
                    new[] { "Onlar gidince ben de gidiyorum sanki." }),
                Set(AlterEgoEvent.CheckpointReached, 0.7f, false,
                    new[] { "Kontrol noktası. Burada biraz dur, ben de nefesleneyim.", "Kaydedildi. Utancın da." },
                    null, null),
                Set(AlterEgoEvent.LevelComplete, 1f, true,
                    new[] { "Tuvalet! Gerçekten bulduk! ...Bu bir rüya, değil mi?" },
                    new[] { "Buldun. Ama ben neden bu kadar soluk görünüyorum?" },
                    new[] { "Tuvaleti buldun ve ben neredeyse yokum. Bir sonraki bölümde görüşürüz. Belki." }),
            };
        }

        private static AlterEgoLineSet Set(AlterEgoEvent trigger, float chance, bool interrupt, string[] calm, string[] uneasy, string[] panic)
        {
            return new AlterEgoLineSet
            {
                trigger = trigger,
                chance = chance,
                interrupt = interrupt,
                calm = calm ?? new string[0],
                uneasy = uneasy ?? new string[0],
                panic = panic ?? new string[0],
            };
        }
    }
}

namespace FeverDream.UI
{
    /// <summary>The control list shown in the HUD help panel and in the main menu. Keep it in step with IA_Player.</summary>
    public static class ControlsText
    {
        public const string Lines =
            "WASD  Hareket      Fare  Bakış\n" +
            "Space  Zıpla, duvardan sıçra\n" +
            "Shift  Sprint      Ctrl / C  Kay, eğil\n" +
            "Sol tık  Hafif saldırı (3'lü zincir)\n" +
            "Sağ tık  Fırlatma, 3 hafiften sonra Bitirici\n" +
            "Havada  Sol tık hava kombosu, Sağ tık Çakılma\n" +
            "Q  Kaçınma (son anda: ağır çekim)\n" +
            "E  Zincirli Kama (camgöbeği nokta)\n" +
            "Esc  Duraklat / menü";

        public const string HudHelp = "KONTROLLER   (F1 gizle / göster)\n" + Lines;
    }
}

using FeverDream.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FeverDream.Editor.Prototype
{
    public static partial class PrototypeContentBuilder
    {
        private const string ControlsHelp =
            "KONTROLLER   (F1 gizle / göster)\n" +
            "WASD  Hareket      Fare  Bakış\n" +
            "Space  Zıpla, duvardan sıçra\n" +
            "Shift  Sprint      Ctrl / C  Kay, eğil\n" +
            "Sol tık  Hafif saldırı (3'lü zincir)\n" +
            "Sağ tık  Fırlatma, 3 hafiften sonra Bitirici\n" +
            "Havada  Sol tık hava kombosu, Sağ tık Çakılma\n" +
            "Q  Kaçınma (son anda: ağır çekim)\n" +
            "E  Zincirli Kama (camgöbeği nokta)\n" +
            "Esc  İmleci kilitle / aç";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color AuraPurple = new Color(0.75f, 0.55f, 1f);
        private static readonly Color AuraCyan = new Color(0.45f, 0.95f, 1f);

        private static GameObject BuildHud()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("PF_HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = root.AddComponent<HudView>();
            var rootRect = root.transform;

            UiText(rootRect, "Crosshair", "+", font, 30, TextAnchor.MiddleCenter, Color.white, Center, Vector2.zero, new Vector2(40f, 40f));
            var grappleMarker = UiImage(rootRect, "GrappleMarker", new Color(0.3f, 1f, 1f, 0.9f), Center, Vector2.zero, new Vector2(26f, 26f));
            grappleMarker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Health, bottom left.
            var healthBar = UiImage(rootRect, "HealthBar", new Color(0f, 0f, 0f, 0.55f), Vector2.zero, new Vector2(40f, 40f), new Vector2(360f, 22f));
            var healthFill = UiImage(healthBar.rectTransform, "Fill", new Color(1f, 0.3f, 0.45f), Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(healthFill.rectTransform, 3f, new Vector2(0f, 0.5f));
            UiText(rootRect, "HealthLabel", "CAN", font, 18, TextAnchor.LowerLeft, Color.white, Vector2.zero, new Vector2(40f, 66f), new Vector2(200f, 24f));

            // Aura, top right.
            var topRight = new Vector2(1f, 1f);
            UiText(rootRect, "AuraLabel", "AURA", font, 22, TextAnchor.UpperRight, AuraPurple, topRight, new Vector2(-40f, -30f), new Vector2(300f, 30f));
            var auraTotal = UiText(rootRect, "AuraTotal", "0", font, 48, TextAnchor.UpperRight, Color.white, topRight, new Vector2(-40f, -58f), new Vector2(400f, 60f));
            var auraChange = UiText(rootRect, "AuraChange", string.Empty, font, 24, TextAnchor.UpperRight, AuraCyan, topRight, new Vector2(-40f, -120f), new Vector2(600f, 32f));

            // Combo, right middle.
            var comboPanel = UiRect(rootRect, "ComboPanel", new Vector2(1f, 0.5f), new Vector2(-40f, 40f), new Vector2(420f, 150f));
            var comboGroup = comboPanel.gameObject.AddComponent<CanvasGroup>();
            var comboHits = UiText(comboPanel, "Hits", string.Empty, font, 56, TextAnchor.UpperRight, Color.white, topRight, Vector2.zero, new Vector2(420f, 64f));
            var comboPoints = UiText(comboPanel, "Points", string.Empty, font, 24, TextAnchor.UpperRight, AuraCyan, topRight, new Vector2(0f, -68f), new Vector2(420f, 30f));
            var timerBar = UiImage(comboPanel, "TimerBar", new Color(0f, 0f, 0f, 0.5f), topRight, new Vector2(0f, -108f), new Vector2(260f, 8f));
            var timerFill = UiImage(timerBar.rectTransform, "Fill", AuraPurple, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(timerFill.rectTransform, 1f, new Vector2(1f, 0.5f));

            // Alter ego subtitles, bottom center.
            var alterEgoPanel = UiImage(rootRect, "AlterEgoPanel", new Color(0.08f, 0.05f, 0.14f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1100f, 110f));
            var alterEgoGroup = alterEgoPanel.gameObject.AddComponent<CanvasGroup>();
            var speaker = UiText(alterEgoPanel.rectTransform, "Speaker", string.Empty, font, 20, TextAnchor.UpperLeft, AuraPurple, new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(800f, 26f));
            var line = UiText(alterEgoPanel.rectTransform, "Line", string.Empty, font, 28, TextAnchor.MiddleCenter, Color.white, Center, new Vector2(0f, -10f), new Vector2(1060f, 70f));
            line.horizontalOverflow = HorizontalWrapMode.Wrap;

            var centerMessage = UiText(rootRect, "CenterMessage", string.Empty, font, 48, TextAnchor.MiddleCenter, Color.white, Center, new Vector2(0f, 160f), new Vector2(1400f, 160f));

            // Controls, top left.
            var helpPanel = UiImage(rootRect, "HelpPanel", new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(560f, 330f));
            var help = UiText(helpPanel.rectTransform, "Controls", ControlsHelp, font, 19, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(530f, 310f));
            help.verticalOverflow = VerticalWrapMode.Overflow;

            SetReference(hud, "healthFill", healthFill.rectTransform);
            SetReference(hud, "comboGroup", comboGroup);
            SetReference(hud, "comboHitsText", comboHits);
            SetReference(hud, "comboPointsText", comboPoints);
            SetReference(hud, "comboTimerFill", timerFill.rectTransform);
            SetReference(hud, "auraTotalText", auraTotal);
            SetReference(hud, "auraChangeText", auraChange);
            SetReference(hud, "alterEgoGroup", alterEgoGroup);
            SetReference(hud, "alterEgoNameText", speaker);
            SetReference(hud, "alterEgoLineText", line);
            SetReference(hud, "grappleMarker", grappleMarker.rectTransform);
            SetReference(hud, "centerMessageText", centerMessage);
            SetReference(hud, "helpPanel", helpPanel.gameObject);
            return root;
        }

        /// <summary>Rect anchored at a point. The pivot defaults to the anchor so positions read as offsets from that corner.</summary>
        private static RectTransform UiRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image UiImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = UiRect(parent, name, anchor, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text UiText(Transform parent, string name, string content, Font font, int size, TextAnchor alignment, Color color,
            Vector2 anchor, Vector2 position, Vector2 rectSize)
        {
            var text = UiRect(parent, name, anchor, position, rectSize).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }

        private static void Stretch(RectTransform rect, float inset, Vector2 pivot)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = pivot;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}

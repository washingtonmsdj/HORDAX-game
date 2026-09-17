using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HORDAX.Core;
using HORDAX.Data;
using HORDAX.Prototype;

namespace HORDAX.UI
{
    public sealed class MetaMenuController : MonoBehaviour
    {
        [SerializeField] private CampaignDefinition campaignDefinition;
        [SerializeField] private string gameplaySceneName = "Prototype";

        private CampaignDefinition campaign;
        private ProgressionService progression;
        private Font font;
        private Text walletText;
        private readonly List<Button> levelButtons = new List<Button>();
        private readonly List<Text> levelLabels = new List<Text>();
        private readonly List<Button> upgradeButtons = new List<Button>();
        private readonly List<Text> upgradeLabels = new List<Text>();

        private void Start()
        {
            progression = ProgressionService.GetOrCreate();
            campaign = campaignDefinition != null ? campaignDefinition : PrototypeCampaignFactory.CreateCampaign();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildEventSystem();
            BuildUi();
            Refresh();
        }

        private void BuildEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            gameObject.AddComponent<GraphicRaycaster>();

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.045f, 0.065f, 1f);

            Text title = CreateText("Title", transform, "HORDAX", 86, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -45f);
            title.rectTransform.sizeDelta = new Vector2(1000f, 110f);

            walletText = CreateText("Wallet", transform, string.Empty, 36, TextAnchor.UpperRight);
            walletText.rectTransform.anchorMin = new Vector2(1f, 1f);
            walletText.rectTransform.anchorMax = new Vector2(1f, 1f);
            walletText.rectTransform.pivot = new Vector2(1f, 1f);
            walletText.rectTransform.anchoredPosition = new Vector2(-50f, -55f);
            walletText.rectTransform.sizeDelta = new Vector2(520f, 60f);

            Text levelsTitle = CreateText("Levels Title", transform, "CAMPAIGN", 40, TextAnchor.MiddleCenter);
            Position(levelsTitle.rectTransform, new Vector2(520f, 840f), new Vector2(650f, 70f));

            for (int i = 0; i < campaign.LevelCount; i++)
            {
                int index = i;
                Button button = CreateButton(
                    "Level " + (i + 1),
                    transform,
                    new Vector2(520f, 735f - i * 118f),
                    new Vector2(650f, 92f),
                    () => PlayLevel(index));

                levelButtons.Add(button);
                levelLabels.Add(button.GetComponentInChildren<Text>());
            }

            Text upgradesTitle = CreateText("Upgrades Title", transform, "PERMANENT UPGRADES", 40, TextAnchor.MiddleCenter);
            Position(upgradesTitle.rectTransform, new Vector2(1400f, 840f), new Vector2(700f, 70f));

            IReadOnlyList<PermanentUpgradeDefinition> upgrades = PrototypeUpgradeCatalog.All;
            for (int i = 0; i < upgrades.Count; i++)
            {
                PermanentUpgradeDefinition definition = upgrades[i];
                Button button = CreateButton(
                    "Upgrade " + definition.UpgradeId,
                    transform,
                    new Vector2(1400f, 710f - i * 150f),
                    new Vector2(700f, 112f),
                    () => Purchase(definition));

                upgradeButtons.Add(button);
                upgradeLabels.Add(button.GetComponentInChildren<Text>());
            }

            Text hint = CreateText(
                "Hint",
                transform,
                "BLOCKOUT MENU - arte final entra depois sem mudar a progressao",
                28,
                TextAnchor.LowerCenter);
            hint.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 32f);
            hint.rectTransform.sizeDelta = new Vector2(1300f, 55f);
        }

        private void Refresh()
        {
            walletText.text = $"COINS {progression.WalletCoins}";

            for (int i = 0; i < levelButtons.Count; i++)
            {
                LevelDefinition level = campaign.GetLevel(i);
                bool unlocked = progression.IsLevelUnlocked(campaign, i);
                bool completed = level != null && progression.IsLevelCompleted(level.LevelId);

                levelButtons[i].interactable = unlocked;
                levelLabels[i].text =
                    level == null
                        ? "INVALID LEVEL"
                        : $"{i + 1:00}  {level.DisplayName}   {(completed ? "COMPLETE" : unlocked ? "PLAY" : "LOCKED")}";
            }

            IReadOnlyList<PermanentUpgradeDefinition> upgrades = PrototypeUpgradeCatalog.All;
            for (int i = 0; i < upgradeButtons.Count && i < upgrades.Count; i++)
            {
                PermanentUpgradeDefinition definition = upgrades[i];
                int level = progression.GetUpgradeLevel(definition.UpgradeId);
                bool max = level >= definition.MaxLevel;
                int cost = progression.GetUpgradeCost(definition);

                upgradeButtons[i].interactable = !max && progression.WalletCoins >= cost;
                upgradeLabels[i].text =
                    max
                        ? $"{definition.DisplayName}   LV {level}/{definition.MaxLevel}   MAX"
                        : $"{definition.DisplayName}   LV {level}/{definition.MaxLevel}   COST {cost}";
            }
        }

        private void PlayLevel(int index)
        {
            if (!progression.IsLevelUnlocked(campaign, index)) return;
            if (!GameSession.SelectLevel(campaign, index)) return;

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void Purchase(PermanentUpgradeDefinition definition)
        {
            progression.TryPurchaseUpgrade(definition);
            Refresh();
        }

        private Button CreateButton(
            string name,
            Transform parent,
            Vector2 center,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            Position(rect, center, size);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.13f, 0.17f, 0.24f, 0.96f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text text = CreateText("Label", go.transform, string.Empty, 30, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(18f, 8f);
            text.rectTransform.offsetMax = new Vector2(-18f, -8f);

            return button;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static void Position(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Data;
using HORDAX.Enemies;
using HORDAX.Player;

namespace HORDAX.UI
{
    public sealed class HudController : MonoBehaviour
    {
        private const string PrototypeRevision = "TWO-LANE CORE / PASS 1";

        private RunnerController runner;
        private PlayerHealth health;
        private WeaponController weapon;
        private Text statusText;
        private Text statsText;
        private Text encounterText;
        private Text revisionText;
        private Image healthFill;
        private Image progressFill;
        private Image bossHealthFill;
        private GameObject bossHealthRoot;
        private GameObject resultControls;
        private Button nextButton;
        private GameState lastState = GameState.Booting;
        private float restartAllowedAt;
        private float damageFlashTimer;

        public void Initialize(RunnerController player)
        {
            runner = player;
            health = player.GetComponent<PlayerHealth>();
            weapon = player.GetComponent<WeaponController>();
            BuildUi();

            if (health != null)
                health.Damaged += OnPlayerDamaged;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnPlayerDamaged;
        }

        private void OnPlayerDamaged(float amount)
        {
            damageFlashTimer = Mathf.Max(damageFlashTimer, 0.18f);
        }

        private void Update()
        {
            if (runner == null || health == null || weapon == null || GameManager.Instance == null) return;

            GameState state = GameManager.Instance.State;
            if (state != lastState)
            {
                lastState = state;
                if (state == GameState.Won || state == GameState.Lost)
                    restartAllowedAt = Time.unscaledTime + 0.45f;
            }

            if (damageFlashTimer > 0f)
                damageFlashTimer = Mathf.Max(0f, damageFlashTimer - Time.unscaledDeltaTime);

            healthFill.fillAmount = health.Normalized;
            healthFill.color = damageFlashTimer > 0f
                ? new Color(1f, 0.20f, 0.16f, 1f)
                : new Color(0.24f, 0.92f, 0.46f, 1f);
            float finish = Mathf.Max(1f, GameManager.Instance.FinishZ);
            progressFill.fillAmount = Mathf.Clamp01(runner.transform.position.z / finish);

            statsText.text =
                $"HP {Mathf.CeilToInt(health.CurrentHealth)}   {weapon.DisplayName} [{weapon.Rarity}] LV {weapon.UpgradeLevel}   " +
                $"DMG {weapon.Damage:0.#}   ROF {weapon.FireRate:0.#}   x{weapon.ProjectilesPerShot}\n" +
                $"KILLS {GameManager.Instance.EnemyKills}   LEAKS {GameManager.Instance.EnemyBreaches}   " +
                $"BOSS {GameManager.Instance.BossKills}/{GameManager.Instance.RequiredBossKills}   " +
                $"COINS {GameManager.Instance.RunCoins}   SCORE {GameManager.Instance.Score}";

            bool bossEncounter = GameManager.Instance.TryGetActiveBossBarrier(out _);
            encounterText.text = bossEncounter ? "BOSS FIGHT  -  DEFEAT THE BOSS TO ADVANCE" : string.Empty;
            encounterText.gameObject.SetActive(bossEncounter);

            EnemyAgent boss = EnemyAgent.ActiveBoss;
            bool showBossHealth = bossEncounter && boss != null;
            if (bossHealthRoot != null && bossHealthRoot.activeSelf != showBossHealth)
                bossHealthRoot.SetActive(showBossHealth);
            if (showBossHealth && bossHealthFill != null)
                bossHealthFill.fillAmount = boss.HealthNormalized;

            bool ended = state == GameState.Won || state == GameState.Lost;
            if (resultControls != null && resultControls.activeSelf != ended)
                resultControls.SetActive(ended);

            switch (state)
            {
                case GameState.Won:
                    statusText.text =
                        $"HORDAX\nFASE CONCLUÍDA\nSTARS {GameManager.Instance.EarnedStars}/3   " +
                        $"+{GameManager.Instance.RunCoins} COINS   SCORE {GameManager.Instance.Score}\n" +
                        GameManager.Instance.GetObjectiveResultSummary();
                    RefreshNextButton();
                    break;

                case GameState.Lost:
                    statusText.text =
                        $"HORDAX\nDERROTA\nKILLS {GameManager.Instance.EnemyKills}   SCORE {GameManager.Instance.Score}";
                    if (nextButton != null) nextButton.gameObject.SetActive(false);
                    break;

                default:
                    statusText.text = string.Empty;
                    break;
            }

            if (ended && Time.unscaledTime >= restartAllowedAt)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    Restart();

                if (Input.GetKeyDown(KeyCode.M))
                    BackToMenu();

                if (state == GameState.Won && Input.GetKeyDown(KeyCode.N))
                    NextLevel();
            }
        }

        private void Restart()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Restart();
        }

        private void BackToMenu()
        {
            SceneManager.LoadScene("FrontEnd");
        }

        private void NextLevel()
        {
            CampaignDefinition campaign = GameSession.ActiveCampaign;
            int nextIndex = GameSession.SelectedLevelIndex + 1;
            if (campaign == null || nextIndex < 0 || nextIndex >= campaign.LevelCount) return;

            ProgressionService progression = ProgressionService.GetOrCreate();
            if (!progression.IsLevelUnlocked(campaign, nextIndex)) return;
            if (!GameSession.SelectLevel(campaign, nextIndex)) return;

            SceneManager.LoadScene("Prototype");
        }

        private void RefreshNextButton()
        {
            if (nextButton == null) return;

            CampaignDefinition campaign = GameSession.ActiveCampaign;
            int nextIndex = GameSession.SelectedLevelIndex + 1;
            bool available =
                campaign != null &&
                nextIndex >= 0 &&
                nextIndex < campaign.LevelCount &&
                ProgressionService.GetOrCreate().IsLevelUnlocked(campaign, nextIndex);

            nextButton.gameObject.SetActive(available);
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject topPanel = new GameObject("Top HUD Panel", typeof(RectTransform), typeof(Image));
            topPanel.transform.SetParent(transform, false);
            RectTransform topPanelRect = topPanel.GetComponent<RectTransform>();
            topPanelRect.anchorMin = new Vector2(0f, 1f);
            topPanelRect.anchorMax = new Vector2(1f, 1f);
            topPanelRect.pivot = new Vector2(0.5f, 1f);
            topPanelRect.anchoredPosition = Vector2.zero;
            topPanelRect.sizeDelta = new Vector2(0f, 148f);
            topPanel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.055f, 0.82f);

            statsText = CreateText("Stats", topPanel.transform, font, 27, TextAnchor.UpperLeft);
            RectTransform statsRect = statsText.rectTransform;
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(0f, 1f);
            statsRect.pivot = new Vector2(0f, 1f);
            statsRect.anchoredPosition = new Vector2(40f, -22f);
            statsRect.sizeDelta = new Vector2(1460f, 92f);
            statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;

            revisionText = CreateText("Prototype Revision", topPanel.transform, font, 22, TextAnchor.UpperRight);
            RectTransform revisionRect = revisionText.rectTransform;
            revisionRect.anchorMin = new Vector2(1f, 1f);
            revisionRect.anchorMax = new Vector2(1f, 1f);
            revisionRect.pivot = new Vector2(1f, 1f);
            revisionRect.anchoredPosition = new Vector2(-40f, -24f);
            revisionRect.sizeDelta = new Vector2(420f, 46f);
            revisionText.color = new Color(0.38f, 0.86f, 1f, 1f);
            revisionText.text = PrototypeRevision;

            encounterText = CreateText("Encounter Status", transform, font, 34, TextAnchor.MiddleCenter);
            RectTransform encounterRect = encounterText.rectTransform;
            encounterRect.anchorMin = new Vector2(0.5f, 1f);
            encounterRect.anchorMax = new Vector2(0.5f, 1f);
            encounterRect.pivot = new Vector2(0.5f, 1f);
            encounterRect.anchoredPosition = new Vector2(0f, -165f);
            encounterRect.sizeDelta = new Vector2(1100f, 52f);
            encounterText.color = new Color(1f, 0.45f, 0.18f, 1f);
            encounterText.gameObject.SetActive(false);

            bossHealthRoot = new GameObject("Boss Health", typeof(RectTransform));
            bossHealthRoot.transform.SetParent(transform, false);
            RectTransform bossRootRect = bossHealthRoot.GetComponent<RectTransform>();
            bossRootRect.anchorMin = new Vector2(0.5f, 1f);
            bossRootRect.anchorMax = new Vector2(0.5f, 1f);
            bossRootRect.pivot = new Vector2(0.5f, 1f);
            bossRootRect.anchoredPosition = new Vector2(0f, -220f);
            bossRootRect.sizeDelta = new Vector2(760f, 52f);

            Text bossLabel = CreateText("Boss Label", bossHealthRoot.transform, font, 24, TextAnchor.MiddleCenter);
            bossLabel.text = "BOSS HP";
            bossLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            bossLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            bossLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            bossLabel.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            bossLabel.rectTransform.sizeDelta = new Vector2(0f, 28f);

            GameObject bossBarBg = new GameObject("Boss Bar Background", typeof(RectTransform), typeof(Image));
            bossBarBg.transform.SetParent(bossHealthRoot.transform, false);
            RectTransform bossBarBgRect = bossBarBg.GetComponent<RectTransform>();
            bossBarBgRect.anchorMin = new Vector2(0f, 0f);
            bossBarBgRect.anchorMax = new Vector2(1f, 0f);
            bossBarBgRect.pivot = new Vector2(0.5f, 0f);
            bossBarBgRect.anchoredPosition = Vector2.zero;
            bossBarBgRect.sizeDelta = new Vector2(0f, 18f);
            bossBarBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject bossFillObject = new GameObject("Boss Bar Fill", typeof(RectTransform), typeof(Image));
            bossFillObject.transform.SetParent(bossBarBg.transform, false);
            bossHealthFill = bossFillObject.GetComponent<Image>();
            bossHealthFill.color = new Color(0.95f, 0.18f, 0.16f, 1f);
            bossHealthFill.type = Image.Type.Filled;
            bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
            bossHealthFill.fillOrigin = 0;
            bossHealthFill.fillAmount = 1f;
            RectTransform bossFillRect = bossFillObject.GetComponent<RectTransform>();
            bossFillRect.anchorMin = Vector2.zero;
            bossFillRect.anchorMax = Vector2.one;
            bossFillRect.offsetMin = new Vector2(3f, 3f);
            bossFillRect.offsetMax = new Vector2(-3f, -3f);
            bossHealthRoot.SetActive(false);

            statusText = CreateText("Status", transform, font, 62, TextAnchor.MiddleCenter);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(0f, 140f);
            statusRect.offsetMax = Vector2.zero;

            healthFill = CreateBar("Health", new Vector2(40f, -112f), new Vector2(520f, 24f));
            healthFill.color = new Color(0.24f, 0.92f, 0.46f, 1f);

            progressFill = CreateBar("Progress", new Vector2(0f, 28f), new Vector2(760f, 18f), true);
            progressFill.color = new Color(0.20f, 0.78f, 1f, 1f);

            GameObject resultBackdrop = new GameObject("Result Backdrop", typeof(RectTransform), typeof(Image));
            resultBackdrop.transform.SetParent(transform, false);
            RectTransform backdropRect = resultBackdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = new Vector2(0.5f, 0.5f);
            backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
            backdropRect.pivot = new Vector2(0.5f, 0.5f);
            backdropRect.anchoredPosition = new Vector2(0f, 20f);
            backdropRect.sizeDelta = new Vector2(1120f, 470f);
            resultBackdrop.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.90f);

            statusText.transform.SetParent(resultBackdrop.transform, false);
            RectTransform resultStatusRect = statusText.rectTransform;
            resultStatusRect.anchorMin = Vector2.zero;
            resultStatusRect.anchorMax = Vector2.one;
            resultStatusRect.offsetMin = new Vector2(40f, 185f);
            resultStatusRect.offsetMax = new Vector2(-40f, -30f);

            resultControls = new GameObject("Result Controls", typeof(RectTransform));
            resultControls.transform.SetParent(resultBackdrop.transform, false);
            RectTransform resultRect = resultControls.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = new Vector2(0f, -150f);
            resultRect.sizeDelta = new Vector2(1000f, 180f);

            CreateButton("Restart", resultControls.transform, new Vector2(-300f, 0f), "RESTART", Restart);
            CreateButton("Menu", resultControls.transform, Vector2.zero, "MENU", BackToMenu);
            nextButton = CreateButton("Next", resultControls.transform, new Vector2(300f, 0f), "NEXT", NextLevel);

            resultBackdrop.SetActive(false);
            resultControls = resultBackdrop;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            Vector2 position,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250f, 90f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.10f, 0.15f, 0.22f, 0.96f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text text = CreateText("Label", go.transform, font, 30, TextAnchor.MiddleCenter);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private Text CreateText(string objectName, Transform parent, Font font, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            return text;
        }

        private Image CreateBar(string objectName, Vector2 position, Vector2 size, bool bottomCentered = false)
        {
            GameObject background = new GameObject(objectName + " Background", typeof(RectTransform));
            background.transform.SetParent(transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform bgRect = background.GetComponent<RectTransform>();
            if (bottomCentered)
            {
                bgRect.anchorMin = new Vector2(0.5f, 0f);
                bgRect.anchorMax = new Vector2(0.5f, 0f);
                bgRect.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                bgRect.anchorMin = new Vector2(0f, 1f);
                bgRect.anchorMax = new Vector2(0f, 1f);
                bgRect.pivot = new Vector2(0f, 1f);
            }

            bgRect.anchoredPosition = position;
            bgRect.sizeDelta = size;

            GameObject fill = new GameObject(objectName + " Fill", typeof(RectTransform));
            fill.transform.SetParent(background.transform, false);
            Image image = fill.AddComponent<Image>();
            image.color = Color.white;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            return image;
        }
    }
}

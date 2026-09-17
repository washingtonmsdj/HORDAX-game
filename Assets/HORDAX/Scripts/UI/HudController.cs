using UnityEngine;
using UnityEngine.UI;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.UI
{
    public sealed class HudController : MonoBehaviour
    {
        private RunnerController runner;
        private PlayerHealth health;
        private WeaponController weapon;
        private Text statusText;
        private Text statsText;
        private Image healthFill;
        private Image progressFill;
        private GameState lastState = GameState.Booting;
        private float restartAllowedAt;

        public void Initialize(RunnerController player)
        {
            runner = player;
            health = player.GetComponent<PlayerHealth>();
            weapon = player.GetComponent<WeaponController>();
            BuildUi();
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

            healthFill.fillAmount = health.Normalized;
            float finish = Mathf.Max(1f, GameManager.Instance.FinishZ);
            progressFill.fillAmount = Mathf.Clamp01(runner.transform.position.z / finish);

            statsText.text = $"HP {Mathf.CeilToInt(health.CurrentHealth)}   WPN LV {weapon.UpgradeLevel}   DMG {weapon.Damage:0.#}   ROF {weapon.FireRate:0.#}   KILLS {GameManager.Instance.EnemyKills}";

            switch (state)
            {
                case GameState.Won:
                    statusText.text = "HORDAX\nFASE CONCLUÍDA\n\nTOQUE / CLIQUE / R PARA REINICIAR";
                    break;
                case GameState.Lost:
                    statusText.text = "HORDAX\nDERROTA\n\nTOQUE / CLIQUE / R PARA REINICIAR";
                    break;
                default:
                    statusText.text = string.Empty;
                    break;
            }

            if ((state == GameState.Won || state == GameState.Lost) && Time.unscaledTime >= restartAllowedAt)
            {
                bool restartPressed = Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0);
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                    restartPressed = true;

                if (restartPressed)
                    GameManager.Instance.Restart();
            }
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            statsText = CreateText("Stats", transform, font, 34, TextAnchor.UpperLeft);
            RectTransform statsRect = statsText.rectTransform;
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(0f, 1f);
            statsRect.pivot = new Vector2(0f, 1f);
            statsRect.anchoredPosition = new Vector2(40f, -35f);
            statsRect.sizeDelta = new Vector2(1500f, 60f);

            statusText = CreateText("Status", transform, font, 62, TextAnchor.MiddleCenter);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            healthFill = CreateBar("Health", new Vector2(40f, -95f), new Vector2(520f, 30f));
            progressFill = CreateBar("Progress", new Vector2(0f, 28f), new Vector2(760f, 18f), true);
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

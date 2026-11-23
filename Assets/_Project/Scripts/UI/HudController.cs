using AirDefense.Core;
using AirDefense.Missions;
using AirDefense.Threats;
using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// Builds and drives the M1 greybox HUD (UI_UX_FLOW.md §2): radar, health,
    /// weapon tiles, ammo/heat, FIRE/ZOOM, contextual prompts, briefing, pause
    /// and the after-action screen. Layout is in 1920x1080 reference units and
    /// follows the device safe area.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        public MissionDirector director;
        public ManualWeaponController player;
        public PlayerInputState input;
        public ThreatManager threats;
        public Camera cam;

        private Canvas _canvas;
        private RectTransform _safe;
        private Rect _lastSafe;

        private Image _healthFill;
        private Text _healthText;
        private Image _ammoFill;
        private Image _heatFill;
        private Text _ammoText;
        private Text _statusText;
        private Text _banner;
        private float _bannerUntil;
        private Text _prompt;
        private Image _fireImage;

        private GameObject _briefing;
        private GameObject _pause;
        private GameObject _end;
        private Text _endTitle;
        private Text _endBody;
        private Text _sensText;
        private Text _invertText;
        private Text _assistText;

        // Onboarding (GDD §9): each prompt shows once, in context.
        private bool _dragged, _fired, _hintedLead, _hintedReload;
        private float _promptUntil;

        private void Start()
        {
            BuildCanvas();
            BuildHud();
            BuildBriefing();
            BuildPause();
            BuildEnd();
            player.InputEnabled = false;
            GameEvents.MissionEnded += OnMissionEnded;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.ThreatImpacted += OnImpacted;
        }

        private void OnDestroy()
        {
            GameEvents.MissionEnded -= OnMissionEnded;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.ThreatImpacted -= OnImpacted;
            Time.timeScale = 1f;
        }

        // ---------------------------------------------------------------- build

        private void BuildCanvas()
        {
            var go = new GameObject("HUD Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            _safe = UiFactory.Rect("SafeArea", go.transform);
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            Rect s = Screen.safeArea;
            _lastSafe = s;
            _safe.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            _safe.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
        }

        private void BuildHud()
        {
            // Aim area first so buttons draw (and receive touches) above it.
            var aim = UiFactory.Image("AimArea", _safe, new Color(0f, 0f, 0f, 0f), raycast: true);
            aim.rectTransform.Stretch(new Vector2(0.4f, 0f), Vector2.one);
            var aimPanel = aim.gameObject.AddComponent<AimPanel>();
            aimPanel.input = input;
            aimPanel.FirstDrag = () => _dragged = true;

            ThreatMarkers.Build(_safe, _canvas, threats, cam, player.mount);

            // Crosshair
            var ch = UiFactory.Image("Crosshair", _safe, new Color(0.7f, 1f, 0.7f, 0.8f), UiFactory.Ring);
            ch.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            var dot = UiFactory.Image("Dot", ch.transform, new Color(0.7f, 1f, 0.7f, 0.9f), UiFactory.Circle);
            dot.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));

            // Radar (top-left) and installation health below it.
            var radar = RadarView.Build(_safe, threats, player.mount, threats.installationCenter);
            radar.GetComponent<RectTransform>().Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(280f, 280f));

            var hpBg = UiFactory.Image("HealthBar", _safe, UiFactory.Panel, UiFactory.Rounded);
            hpBg.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -316f), new Vector2(280f, 30f));
            _healthFill = UiFactory.Image("Fill", hpBg.transform, UiFactory.Accent, UiFactory.White);
            _healthFill.rectTransform.Stretch(new Vector2(0.02f, 0.2f), new Vector2(0.98f, 0.8f));
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthText = UiFactory.Text("Label", hpBg.transform, "INSTALLATION 100%", 18, TextAnchor.MiddleCenter, Color.white);
            _healthText.rectTransform.Stretch(Vector2.zero, Vector2.one);
            var hpOutline = _healthText.gameObject.AddComponent<Outline>();
            hpOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            hpOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Top-centre banner + placeholder notice.
            _banner = UiFactory.Text("Banner", _safe, "", 40, TextAnchor.UpperCenter, UiFactory.Warn);
            _banner.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 60f));
            var ph = UiFactory.Text("PlaceholderNotice", _safe, "PLACEHOLDER GREYBOX ART  -  M1 PROTOTYPE", 16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.45f));
            ph.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(900f, 24f));

            // Pause (top-right).
            var pauseBtn = UiFactory.Button("Pause", _safe, "II", 40, () => SetPaused(true));
            pauseBtn.GetComponent<RectTransform>().Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(100f, 100f));

            // Weapon tiles (bottom-left). Only ZU-23-2 is available (C-05); others show unlock cost.
            Tile("ZU-23-2", "MANUAL", new Vector2(24f, 24f), true);
            Tile("C-RAM", "LOCKED\n4,000 cr", new Vector2(194f, 24f), false);
            Tile("TUNGUSKA", "LOCKED\n10,000 cr", new Vector2(364f, 24f), false);

            // Ammo / heat (bottom-centre). Tap to reload early.
            var ammoBtn = UiFactory.Button("Ammo", _safe, "", 10, () => { input.ReloadRequested = true; _hintedReload = true; });
            ammoBtn.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(440f, 110f));
            _ammoText = UiFactory.Text("AmmoText", ammoBtn.transform, "", 26, TextAnchor.UpperLeft);
            _ammoText.rectTransform.Stretch(new Vector2(0.05f, 0.55f), new Vector2(0.6f, 0.95f));
            _statusText = UiFactory.Text("Status", ammoBtn.transform, "", 24, TextAnchor.UpperRight, UiFactory.Warn);
            _statusText.rectTransform.Stretch(new Vector2(0.4f, 0.55f), new Vector2(0.95f, 0.95f));
            _ammoFill = Bar(ammoBtn.transform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.45f), UiFactory.Accent);
            _heatFill = Bar(ammoBtn.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.22f), UiFactory.Warn);

            // FIRE (bottom-right) and ZOOM above it.
            var fire = UiFactory.Image("Fire", _safe, new Color(0.75f, 0.18f, 0.15f, 0.85f), UiFactory.Circle, raycast: true);
            fire.rectTransform.Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(240f, 240f));
            var fireLabel = UiFactory.Text("Label", fire.transform, "FIRE", 44);
            fireLabel.rectTransform.Stretch(Vector2.zero, Vector2.one);
            var hold = fire.gameObject.AddComponent<HoldButton>();
            hold.Changed = held => { input.FireHeld = held; if (held) _fired = true; };
            _fireImage = fire;

            var zoom = UiFactory.Button("Zoom", _safe, "ZOOM", 30, () => input.ZoomToggleRequested = true);
            zoom.GetComponent<RectTransform>().Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-85f, 310f), new Vector2(150f, 84f));

            // Contextual prompt.
            _prompt = UiFactory.Text("Prompt", _safe, "", 34, TextAnchor.MiddleCenter, Color.white);
            _prompt.rectTransform.Anchor(new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 60f));
            var outline = _prompt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void Tile(string name, string state, Vector2 pos, bool selected)
        {
            var bg = UiFactory.Image("Tile_" + name, _safe, selected ? new Color(0.12f, 0.3f, 0.14f, 0.9f) : new Color(0.1f, 0.1f, 0.1f, 0.7f), UiFactory.Rounded);
            bg.rectTransform.Anchor(Vector2.zero, Vector2.zero, pos, new Vector2(160f, 120f));
            if (selected)
            {
                var border = bg.gameObject.AddComponent<Outline>();
                border.effectColor = UiFactory.Accent;
                border.effectDistance = new Vector2(3f, -3f);
            }
            var title = UiFactory.Text("Name", bg.transform, name, 24, TextAnchor.UpperCenter, selected ? Color.white : new Color(1f, 1f, 1f, 0.5f));
            title.rectTransform.Stretch(new Vector2(0f, 0.5f), new Vector2(1f, 0.92f));
            var st = UiFactory.Text("State", bg.transform, state, 18, TextAnchor.MiddleCenter, selected ? UiFactory.Accent : new Color(1f, 1f, 1f, 0.45f));
            st.rectTransform.Stretch(new Vector2(0f, 0.05f), new Vector2(1f, 0.5f));
        }

        private static Image Bar(Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var bg = UiFactory.Image("BarBg", parent, new Color(1f, 1f, 1f, 0.12f));
            bg.rectTransform.Stretch(min, max);
            var fill = UiFactory.Image("Fill", bg.transform, color, UiFactory.White);
            fill.rectTransform.Stretch(Vector2.zero, Vector2.one);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        private GameObject Overlay(string name)
        {
            var bg = UiFactory.Image(name, _safe, new Color(0f, 0f, 0f, 0.7f), raycast: true);
            bg.rectTransform.Stretch(Vector2.zero, Vector2.one);
            var card = UiFactory.Image("Card", bg.transform, UiFactory.Panel, UiFactory.Rounded);
            card.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
            return bg.gameObject;
        }

        private void BuildBriefing()
        {
            _briefing = Overlay("Briefing");
            var card = _briefing.transform.Find("Card");
            var t = UiFactory.Text("Title", card, director.mission.title.ToUpperInvariant(), 52, TextAnchor.UpperCenter, UiFactory.Accent);
            t.rectTransform.Stretch(new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
            var b = UiFactory.Text("Body", card, director.mission.briefing, 30, TextAnchor.UpperLeft);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            b.rectTransform.Stretch(new Vector2(0.07f, 0.3f), new Vector2(0.93f, 0.78f));
            var start = UiFactory.Button("Start", card, "START", 44, StartMission, new Color(0.15f, 0.45f, 0.2f, 1f));
            start.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(380f, 120f));

            // CC-BY-4.0 attribution (docs/ASSET_REGISTER.md §8). Moves to a Credits screen in M2.
            var credit = UiFactory.Text("Credit", card,
                "ZU-23-2 model: \"ZU - 23\" by LuckySkink9661 (sketchfab.com), CC BY 4.0, modified (reduced and rigged). Other art: placeholder.",
                16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.5f));
            credit.horizontalOverflow = HorizontalWrapMode.Wrap;
            credit.rectTransform.Stretch(new Vector2(0.04f, 0.005f), new Vector2(0.96f, 0.06f));
        }

        private void BuildPause()
        {
            _pause = Overlay("Pause");
            var card = _pause.transform.Find("Card");
            var t = UiFactory.Text("Title", card, "PAUSED", 52, TextAnchor.UpperCenter, UiFactory.Accent);
            t.rectTransform.Stretch(new Vector2(0f, 0.86f), new Vector2(1f, 0.96f));

            _sensText = Row(card, 0.70f, "Aim sensitivity", () => ChangeSensitivity(-0.1f), () => ChangeSensitivity(0.1f));
            _invertText = Row(card, 0.55f, "Invert vertical", () => { GameSettings.InvertY = !GameSettings.InvertY; RefreshSettings(); }, null);
            _assistText = Row(card, 0.40f, "Aim assist (lead ring)", () =>
            {
                GameSettings.Assist = GameSettings.Assist == AimAssist.Off ? AimAssist.Light : AimAssist.Off;
                RefreshSettings();
            }, null);

            var resume = UiFactory.Button("Resume", card, "RESUME", 40, () => SetPaused(false), new Color(0.15f, 0.45f, 0.2f, 1f));
            resume.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 50f), new Vector2(340f, 110f));
            var restart = UiFactory.Button("Restart", card, "RESTART", 40, Restart);
            restart.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 50f), new Vector2(340f, 110f));

            if (Debug.isDebugBuild)
            {
                var stress = UiFactory.Button("Stress", card, "DEV: +20 THREATS", 24, () => { SetPaused(false); director.DebugSpawnStress(20); });
                stress.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(340f, 70f));
            }

            RefreshSettings();
            _pause.SetActive(false);
        }

        private Text Row(Transform card, float y, string label, System.Action minusOrToggle, System.Action plus)
        {
            var l = UiFactory.Text("Label", card, label, 32, TextAnchor.MiddleLeft);
            l.rectTransform.Stretch(new Vector2(0.08f, y - 0.05f), new Vector2(0.5f, y + 0.05f));
            var v = UiFactory.Text("Value", card, "", 32, TextAnchor.MiddleCenter, UiFactory.Accent);
            if (plus != null)
            {
                var m = UiFactory.Button("Minus", card, "-", 44, minusOrToggle);
                m.GetComponent<RectTransform>().Stretch(new Vector2(0.55f, y - 0.05f), new Vector2(0.63f, y + 0.05f));
                v.rectTransform.Stretch(new Vector2(0.64f, y - 0.05f), new Vector2(0.82f, y + 0.05f));
                var p = UiFactory.Button("Plus", card, "+", 44, plus);
                p.GetComponent<RectTransform>().Stretch(new Vector2(0.83f, y - 0.05f), new Vector2(0.91f, y + 0.05f));
            }
            else
            {
                var tg = UiFactory.Button("Toggle", card, "", 10, minusOrToggle);
                tg.GetComponent<RectTransform>().Stretch(new Vector2(0.6f, y - 0.05f), new Vector2(0.91f, y + 0.05f));
                v.transform.SetParent(tg.transform, false);
                v.rectTransform.Stretch(Vector2.zero, Vector2.one);
            }
            return v;
        }

        private void BuildEnd()
        {
            _end = Overlay("AfterAction");
            var card = _end.transform.Find("Card");
            _endTitle = UiFactory.Text("Title", card, "", 56, TextAnchor.UpperCenter);
            _endTitle.rectTransform.Stretch(new Vector2(0f, 0.84f), new Vector2(1f, 0.96f));
            _endBody = UiFactory.Text("Body", card, "", 30, TextAnchor.UpperLeft);
            _endBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endBody.rectTransform.Stretch(new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.8f));
            var retry = UiFactory.Button("Retry", card, "PLAY AGAIN", 40, Restart, new Color(0.15f, 0.45f, 0.2f, 1f));
            retry.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(400f, 110f));
            _end.SetActive(false);
        }

        // --------------------------------------------------------------- update

        private void Update()
        {
            if (Screen.safeArea != _lastSafe) ApplySafeArea();

            // Android back gesture arrives as Escape in the Input System.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && director.Phase == MissionPhase.Running)
                SetPaused(!_pause.activeSelf);

            _healthFill.fillAmount = director.Health01;
            _healthFill.color = director.Health01 > 0.5f ? UiFactory.Accent : director.Health01 > 0.25f ? UiFactory.Warn : UiFactory.Danger;
            _healthText.text = $"INSTALLATION {director.Health01 * 100f:0}%";

            WeaponState s = player.State;
            _ammoText.text = $"ZU-23-2   {s.Ammo}/{s.Def.magazineSize}";
            _ammoFill.fillAmount = s.Reloading ? s.ReloadProgress01 : (float)s.Ammo / s.Def.magazineSize;
            _heatFill.fillAmount = s.Heat01;
            _heatFill.color = s.Overheated ? UiFactory.Danger : UiFactory.Warn;
            _statusText.text = s.Reloading ? "RELOADING" : s.Overheated ? "OVERHEAT" : "";
            _fireImage.color = input.FireHeld ? new Color(1f, 0.3f, 0.2f, 0.95f) : new Color(0.75f, 0.18f, 0.15f, 0.85f);

            if (_banner.text.Length > 0 && Time.unscaledTime > _bannerUntil) _banner.text = "";

            UpdatePrompts();
        }

        private void UpdatePrompts()
        {
            if (director.Phase != MissionPhase.Running) { _prompt.text = ""; return; }

            string text = "";
            if (!_dragged)
            {
                text = "Drag on the right side of the screen to aim";
            }
            else if (!_fired && threats.Active.Count > 0)
            {
                text = "Hold FIRE to shoot - follow the brackets and edge arrows";
            }
            else if (!_hintedLead && player.State.RoundsFired >= 60 && director.Destroyed == 0)
            {
                _hintedLead = true;
                Flash(GameSettings.Assist == AimAssist.Light
                    ? "Aim at the yellow ring - it shows where to lead moving targets"
                    : "Aim ahead of moving targets", 5f);
            }
            else if (!_hintedReload && player.State.Reloading)
            {
                _hintedReload = true;
                Flash("Tip: tap the ammo panel to reload early", 4f);
            }

            if (text.Length > 0) _prompt.text = text;
            else if (Time.unscaledTime > _promptUntil) _prompt.text = "";
        }

        private void Flash(string text, float seconds)
        {
            _prompt.text = text;
            _promptUntil = Time.unscaledTime + seconds;
        }

        private void Banner(string text, float seconds, Color color)
        {
            _banner.text = text;
            _banner.color = color;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        // --------------------------------------------------------------- events

        private void StartMission()
        {
            _briefing.SetActive(false);
            director.Begin();
            Banner("STAND BY - RADAR ACTIVE", 3f, UiFactory.Accent);
        }

        private void OnWaveStarted(int index, string label) =>
            Banner($"{label.ToUpperInvariant()}  -  CONTACTS INBOUND", 3.5f, UiFactory.Warn);

        private void OnImpacted(ThreatAgent t) => Banner("IMPACT ON INSTALLATION", 2f, UiFactory.Danger);

        private void OnMissionEnded(MissionResult r)
        {
            input.FireHeld = false;
            _end.SetActive(true);
            _endTitle.text = r.success ? "INSTALLATION HELD" : "INSTALLATION LOST";
            _endTitle.color = r.success ? UiFactory.Accent : UiFactory.Danger;

            float score = r.healthRemaining01 * 0.5f
                          + (r.threatsTotal > 0 ? (float)r.threatsDestroyed / r.threatsTotal : 0f) * 0.35f
                          + Mathf.Clamp01(r.Accuracy * 4f) * 0.15f;
            string grade = !r.success ? "D" : score >= 0.9f ? "S" : score >= 0.8f ? "A" : score >= 0.65f ? "B" : "C";

            string tip;
            if (r.threatsLeaked > 0)
                tip = $"{r.threatsLeaked} threat(s) reached the base. Engage earlier: watch the radar and the edge arrows, and turn toward contacts before they close in.";
            else if (r.Accuracy < 0.08f)
                tip = "Accuracy was low. Fire short bursts and aim at the yellow lead ring instead of directly at the target.";
            else
                tip = "Clean defence. Next: try with aim assist off, or the DEV stress test from the pause menu.";

            _endBody.text =
                $"Grade: {grade}\n" +
                $"Time: {r.duration:0} s\n" +
                $"Installation: {r.healthRemaining01 * 100f:0}%\n" +
                $"Threats destroyed: {r.threatsDestroyed} / {r.threatsTotal}   (reached base: {r.threatsLeaked})\n" +
                $"Accuracy: {r.Accuracy * 100f:0.0}%  ({r.roundsHit} hits / {r.roundsFired} rounds)\n\n" +
                tip;
        }

        private void SetPaused(bool paused)
        {
            if (director.Phase == MissionPhase.Ended) return;
            _pause.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            input.FireHeld = false;
            if (director.Phase == MissionPhase.Running) player.InputEnabled = !paused;
            RefreshSettings();
        }

        private void ChangeSensitivity(float delta)
        {
            GameSettings.Sensitivity = Mathf.Round((GameSettings.Sensitivity + delta) * 10f) / 10f;
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            if (_sensText == null) return;
            _sensText.text = GameSettings.Sensitivity.ToString("0.0");
            _invertText.text = GameSettings.InvertY ? "ON" : "OFF";
            _assistText.text = GameSettings.Assist == AimAssist.Light ? "ON" : "OFF";
        }

        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnApplicationPause(bool paused)
        {
            // Backgrounding mid-mission pauses the game (MILESTONES §3.2).
            if (paused && director != null && director.Phase == MissionPhase.Running) SetPaused(true);
        }
    }
}

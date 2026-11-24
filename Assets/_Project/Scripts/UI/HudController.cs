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
    [DefaultExecutionOrder(200)] // after ManualWeaponController (100) moves the camera
    public sealed class HudController : MonoBehaviour
    {
        public MissionDirector director;
        public ManualWeaponController player;
        public PlayerInputState input;
        public ThreatManager threats;
        public Camera cam;
        public Progression.WeaponRoster roster;
        [Tooltip("Copy of CREDITS.md, made by the scene builder.")]
        public TextAsset credits;
        private GameObject _credits;

        /// <summary>One weapon tile (HUD_UI_SPECIFICATION.md §5): icon, name, live state, one key value, a thin bar.</summary>
        private sealed class WeaponTile
        {
            public Data.WeaponDefinition def;
            public Image bg, border, accent, icon, bar, barBg;
            public Text name, state, value;
        }
        private readonly System.Collections.Generic.List<WeaponTile> _tiles = new System.Collections.Generic.List<WeaponTile>();
        private Text _ammoName;
        private Text _healthValue;
        private Image _fireRing;
        private Text _fireLabel;
        private RectTransform _promptCard;
        private RectTransform _bannerCard;
        private ThreatMarkers _markers;
        private RadarView _radar;
        private GameObject _unlockConfirm;
        private Text _unlockConfirmText;
        private Data.WeaponDefinition _pendingUnlock;
        private GameObject _endUnlockBtn;
        private Text _qualityText;
        private Text _shakeText;
        private Text _endRewards;
        private Image _unlockFill;
        private Text _unlockText;
        private Text _briefCredits;

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
        private RectTransform _crosshair;

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
            BuildUnlockConfirm();
            player.InputEnabled = false;
            if (Cinematic.CinematicDirector.Playing)
            {
                // The opening cinematic hands over to Mission 1 directly: no briefing card, HUD hidden
                // until the staged reveal (CINEMATIC_OPENING.md shot 6).
                _briefing.SetActive(false);
                SetHudStageAlpha(0, 0f); SetHudStageAlpha(1, 0f); SetHudStageAlpha(2, 0f); SetHudStageAlpha(3, 0f);
            }
            GameEvents.MissionEnded += OnMissionEnded;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.ThreatImpacted += OnImpacted;
            GameEvents.ManualWeaponChanged += OnManualWeaponChanged;
        }

        private void OnDestroy()
        {
            GameEvents.MissionEnded -= OnMissionEnded;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.ThreatImpacted -= OnImpacted;
            GameEvents.ManualWeaponChanged -= OnManualWeaponChanged;
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
            // Keep controls clear of the gesture bar at the bottom edge (VS-1): ~2% of the screen height
            // (≈ 20 reference px), on top of whatever the system safe area already reserves.
            float gesture = Screen.height * 0.02f;
            if (s.yMin < gesture) { s.height -= gesture - s.yMin; s.y = gesture; }
            _safe.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            _safe.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
        }

        private void BuildHud()
        {
            // Aim area first so buttons draw (and receive touches) above it.
            var aim = UiFactory.Image("AimArea", _safe, new Color(0f, 0f, 0f, 0f), raycast: true);
            // Left 60% (user request 2026-11-23): left thumb aims, right thumb holds FIRE.
            aim.rectTransform.Stretch(Vector2.zero, new Vector2(0.6f, 1f));
            var aimPanel = aim.gameObject.AddComponent<AimPanel>();
            aimPanel.input = input;
            aimPanel.FirstDrag = () => _dragged = true;

            _markers = ThreatMarkers.Build(_safe, _canvas, threats, cam, player.mount);

            // Crosshair: drawn where the gun's bore actually points (third-person camera is tilted, see LateUpdate).
            var ch = UiFactory.Image("Crosshair", _safe, UiTheme.System.WithAlpha(0.85f), UiFactory.Ring);
            ch.rectTransform.Anchor(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            _crosshair = ch.rectTransform;
            var dot = UiFactory.Image("Dot", ch.transform, UiTheme.System, UiFactory.Circle);
            dot.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));

            // Radar (top-left) and installation status below it.
            var radar = RadarView.Build(_safe, threats, player.mount, threats.installationCenter);
            _radar = radar;
            radar.GetComponent<RectTransform>().Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(280f, 280f));

            var hp = UiFactory.PanelImage("HealthBar", _safe);
            hp.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -316f), new Vector2(280f, 52f));
            var hpLabel = UiFactory.Text("Label", hp.transform, "INSTALLATION", UiTheme.Caption, TextAnchor.UpperLeft, UiTheme.InkDim, UiTheme.Weight.SemiBold);
            hpLabel.rectTransform.Stretch(new Vector2(0.05f, 0.4f), new Vector2(0.7f, 0.92f));
            _healthValue = UiFactory.Text("Value", hp.transform, "100%", UiTheme.Label, TextAnchor.UpperRight, UiTheme.Ink, UiTheme.Weight.SemiBold);
            _healthValue.rectTransform.Stretch(new Vector2(0.5f, 0.38f), new Vector2(0.95f, 0.95f));
            _healthFill = ThinBar(hp.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.28f), UiTheme.System);
            _healthText = hpLabel; // kept for older references

            // Top-centre banner: a framed card, hidden when empty.
            var bannerImg = UiFactory.PanelImage("Banner", _safe);
            _bannerCard = bannerImg.rectTransform;
            _bannerCard.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(760f, 58f));
            _banner = UiFactory.Text("Text", bannerImg.transform, "", UiTheme.Banner - 6, TextAnchor.MiddleCenter, UiTheme.Caution, UiTheme.Weight.SemiBold);
            _banner.rectTransform.Stretch(Vector2.zero, Vector2.one);
            _bannerCard.gameObject.SetActive(false);

            // Pause (top-right).
            var pauseBtn = UiFactory.Button("Pause", _safe, "II", 34, () => SetPaused(true));
            pauseBtn.GetComponent<RectTransform>().Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(96f, 96f));

            // Weapon tiles (bottom-left), from the roster (C-04, C-05): tap an unlocked weapon to operate it
            // (the others defend on their own), or the next locked one to unlock it with credits.
            for (int i = 0; i < roster.weapons.Count; i++)
                Tile(roster.weapons[i], new Vector2(24f + i * 188f, 24f));
            RefreshTiles();

            // Ammo / heat (bottom-centre). Tap to reload early.
            var ammoBtn = UiFactory.Button("Ammo", _safe, "", 10, () => { input.ReloadRequested = true; _hintedReload = true; });
            ammoBtn.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 24f), new Vector2(380f, 84f));
            _ammoName = UiFactory.Text("Weapon", ammoBtn.transform, "", UiTheme.Caption, TextAnchor.UpperLeft, UiTheme.InkDim, UiTheme.Weight.SemiBold);
            _ammoName.rectTransform.Stretch(new Vector2(0.05f, 0.5f), new Vector2(0.6f, 0.9f));
            _statusText = UiFactory.Text("Status", ammoBtn.transform, "", UiTheme.Caption, TextAnchor.LowerLeft, UiTheme.Caution, UiTheme.Weight.SemiBold);
            _statusText.rectTransform.Stretch(new Vector2(0.05f, 0.28f), new Vector2(0.6f, 0.56f));
            _ammoText = UiFactory.Text("Count", ammoBtn.transform, "", UiTheme.Value + 4, TextAnchor.MiddleRight, UiTheme.Ink, UiTheme.Weight.SemiBold);
            _ammoText.rectTransform.Stretch(new Vector2(0.45f, 0.28f), new Vector2(0.95f, 0.95f));
            _ammoFill = ThinBar(ammoBtn.transform, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.22f), UiTheme.System);
            _heatFill = ThinBar(ammoBtn.transform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.11f), UiTheme.Caution);

            // FIRE (bottom-right): dark disc with a red ring; fills when pressed, says why when it can't fire.
            var fire = UiFactory.Image("Fire", _safe, new Color(0.05f, 0.03f, 0.03f, 0.5f), UiFactory.Circle, raycast: true);
            fire.rectTransform.Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(200f, 200f));
            _fireRing = UiFactory.Image("Ring", fire.transform, UiTheme.Critical, UiFactory.Ring);
            _fireRing.rectTransform.Stretch(Vector2.zero, Vector2.one);
            _fireLabel = UiFactory.Text("Label", fire.transform, "FIRE", 38, TextAnchor.MiddleCenter, UiTheme.Ink, UiTheme.Weight.SemiBold);
            _fireLabel.rectTransform.Stretch(Vector2.zero, Vector2.one);
            var hold = fire.gameObject.AddComponent<HoldButton>();
            hold.Changed = held => { input.FireHeld = held; if (held) _fired = true; };
            _fireImage = fire;

            // Zoom: icon button above FIRE.
            var zoom = UiFactory.Button("Zoom", _safe, "", 10, () => input.ZoomToggleRequested = true);
            zoom.GetComponent<RectTransform>().Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 262f), new Vector2(96f, 96f));
            var lens = UiFactory.Image("Icon", zoom.transform, UiTheme.Ink, UiFactory.Lens);
            lens.rectTransform.Stretch(new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));

            // Contextual prompt card (placed beside the control it explains, see ShowPrompt).
            var card = UiFactory.PanelImage("Prompt", _safe);
            _promptCard = card.rectTransform;
            _prompt = UiFactory.Text("Text", card.transform, "", UiTheme.Body, TextAnchor.MiddleCenter, UiTheme.Ink, UiTheme.Weight.Medium);
            _prompt.rectTransform.Stretch(Vector2.zero, Vector2.one);
            _promptCard.gameObject.SetActive(false);
        }

        private static Image ThinBar(Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var bg = UiFactory.Image("BarBg", parent, UiTheme.Ink.WithAlpha(0.12f), UiFactory.White);
            bg.rectTransform.Stretch(min, max);
            var fill = UiFactory.Image("Fill", bg.transform, color, UiFactory.White);
            fill.rectTransform.Stretch(Vector2.zero, Vector2.one);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        private void Tile(Data.WeaponDefinition def, Vector2 pos)
        {
            var t = new WeaponTile { def = def };
            t.bg = UiFactory.Image("Tile_" + def.weaponId, _safe, UiTheme.Panel, UiFactory.Rounded, raycast: true);
            t.bg.rectTransform.Anchor(Vector2.zero, Vector2.zero, pos, new Vector2(176f, 116f));
            var button = t.bg.gameObject.AddComponent<Button>();
            button.targetGraphic = t.bg;
            button.onClick.AddListener(() => OnTileTapped(def));
            t.border = UiFactory.Border(t.bg.transform, UiTheme.PanelLine);
            t.accent = UiFactory.Image("Accent", t.bg.transform, UiTheme.System, UiFactory.White);
            t.accent.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(4f, 92f));

            t.name = UiFactory.Text("Name", t.bg.transform, def.displayName.Replace("2K22 ", "").ToUpperInvariant(), UiTheme.Label,
                                    TextAnchor.UpperLeft, UiTheme.Ink, UiTheme.Weight.SemiBold);
            t.name.rectTransform.Stretch(new Vector2(0.1f, 0.68f), new Vector2(0.96f, 0.95f));
            t.icon = UiFactory.Image("Icon", t.bg.transform, UiTheme.Ink, null);
            var tex = Resources.Load<Texture2D>("UI/icon_" + def.weaponId);
            if (tex != null) t.icon.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            t.icon.preserveAspect = true;
            t.icon.enabled = tex != null;
            t.icon.rectTransform.Stretch(new Vector2(0.1f, 0.34f), new Vector2(0.94f, 0.7f));
            t.state = UiFactory.Text("State", t.bg.transform, "", UiTheme.Caption, TextAnchor.MiddleLeft, UiTheme.InkDim, UiTheme.Weight.SemiBold);
            t.state.rectTransform.Stretch(new Vector2(0.1f, 0.1f), new Vector2(0.6f, 0.34f));
            t.value = UiFactory.Text("Value", t.bg.transform, "", UiTheme.Caption, TextAnchor.MiddleRight, UiTheme.Ink, UiTheme.Weight.Medium);
            t.value.rectTransform.Stretch(new Vector2(0.5f, 0.1f), new Vector2(0.94f, 0.34f));
            t.bar = ThinBar(t.bg.transform, new Vector2(0.1f, 0.05f), new Vector2(0.94f, 0.09f), UiTheme.System);
            t.barBg = t.bar.transform.parent.GetComponent<Image>();
            _tiles.Add(t);
        }

        /// <summary>Tile states from the saved profile and live weapon state (HUD_UI_SPECIFICATION.md §5).</summary>
        private void RefreshTiles()
        {
            PlayerProfile p = ProfileStore.Profile;
            Data.WeaponDefinition next = roster.NextUnlock(p);
            foreach (var t in _tiles)
            {
                var def = t.def;
                bool manual = def == player.mount.definition;
                bool unlocked = p.IsUnlocked(def.weaponId);
                WeaponStation st = player.StationFor(def);
                bool barOn = false;
                float bar = 0f;
                Color accent, barColor = UiTheme.System, stateColor;
                string state, value = "";

                if (manual || (unlocked && st != null))
                {
                    WeaponState ws = st != null ? st.State : player.State;
                    var auto = st != null ? st.GetComponent<AutonomousWeaponController>() : null;
                    accent = manual ? UiTheme.System : UiTheme.Info;
                    stateColor = accent;
                    state = manual ? "MANUAL" : auto != null && auto.Engaging ? "ENGAGING" : "AUTO";
                    value = $"{ws.Ammo:N0}";
                    barOn = true;
                    bar = (float)ws.Ammo / ws.Def.magazineSize;
                    barColor = accent;
                    if (ws.Reloading)
                    {
                        state = "RELOAD";
                        stateColor = barColor = UiTheme.Caution;
                        bar = ws.ReloadProgress01;
                        value = $"{(1f - ws.ReloadProgress01) * ws.Def.reloadTime:0}s";
                    }
                    else if (ws.Overheated)
                    {
                        state = "OVERHEAT";
                        stateColor = barColor = UiTheme.Caution;
                        bar = ws.Heat01;
                    }
                }
                else if (unlocked)
                {
                    accent = stateColor = UiTheme.Disabled;
                    state = "UNLOCKED";
                }
                else if (def == next)
                {
                    bool afford = p.credits >= def.unlockCost;
                    accent = afford ? UiTheme.Caution : UiTheme.Disabled;
                    stateColor = afford ? UiTheme.Caution : UiTheme.InkDim;
                    state = afford ? "TAP TO UNLOCK" : "LOCKED";
                    value = $"{def.unlockCost:N0} cr";
                    barOn = !afford;
                    bar = Mathf.Clamp01((float)p.credits / Mathf.Max(1, def.unlockCost));
                    barColor = UiTheme.Caution;
                }
                else
                {
                    accent = UiTheme.Disabled;
                    stateColor = UiTheme.InkDim;
                    state = "LOCKED";
                    value = $"{def.unlockCost:N0} cr";
                }

                bool active = manual || (unlocked && st != null);
                bool unlockReady = !unlocked && def == next && p.credits >= def.unlockCost;
                t.bg.color = manual ? new Color(0.07f, 0.17f, 0.11f, 0.9f) : UiTheme.Panel; // dark green: selected, text stays readable
                float pulse = unlockReady ? 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f) : 1f;
                t.border.color = manual ? UiTheme.System : unlockReady ? UiTheme.Caution.WithAlpha(pulse) : UiTheme.PanelLine;
                t.accent.color = accent;
                t.name.color = active || unlockReady ? UiTheme.Ink : UiTheme.InkDim;
                t.icon.color = manual ? UiTheme.Ink : active ? UiTheme.Ink.WithAlpha(0.8f) : UiTheme.Disabled.WithAlpha(0.7f);
                t.state.text = state;
                t.state.color = stateColor;
                t.value.text = value;
                t.value.color = active ? UiTheme.Ink : UiTheme.InkDim;
                t.barBg.enabled = t.bar.enabled = barOn;
                t.bar.fillAmount = bar;
                t.bar.color = barColor;
            }
        }

        private void OnTileTapped(Data.WeaponDefinition def)
        {
            PlayerProfile p = ProfileStore.Profile;
            if (p.IsUnlocked(def.weaponId))
            {
                WeaponStation st = player.StationFor(def);
                if (st == null) { Flash($"{def.displayName} joins the base in a later update", 3f); return; }
                if (player.Select(st)) Banner($"{def.displayName.ToUpperInvariant()} - MANUAL CONTROL", 2f, UiFactory.Accent);
                return;
            }
            if (roster.NextUnlock(p) != def)
            {
                Flash($"Unlock {roster.NextUnlock(p)?.displayName} first", 3f);
                return;
            }
            if (p.credits < def.unlockCost)
            {
                Flash($"{def.displayName} needs {def.unlockCost - p.credits:N0} more credits - earn them by defending the base", 4f);
                return;
            }
            AskUnlock(def);
        }

        /// <summary>Confirmation card before spending credits (no accidental purchases).</summary>
        private void AskUnlock(Data.WeaponDefinition def)
        {
            _pendingUnlock = def;
            _unlockConfirmText.text =
                $"Unlock {def.displayName} for {def.unlockCost:N0} credits?\n\n" +
                $"Balance after: {ProfileStore.Profile.credits - def.unlockCost:N0} cr\n\n" +
                (def.weaponId == "cram"
                    ? "20 mm six-barrel rotary gun, 75 rounds a second. It tracks threats by radar and engages only those heading into the base. Its rounds self-destruct at about 2 km."
                    : "");
            _unlockConfirm.SetActive(true);
            HidePrompt();
            _unlockConfirm.transform.SetAsLastSibling();
            if (director.Phase == MissionPhase.Running)
            {
                Time.timeScale = 0f;
                input.FireHeld = false;
                player.InputEnabled = false;
            }
        }

        private void CloseUnlock()
        {
            _unlockConfirm.SetActive(false);
            _pendingUnlock = null;
            if (director.Phase == MissionPhase.Running && !_pause.activeSelf)
            {
                Time.timeScale = 1f;
                player.InputEnabled = true;
            }
        }

        private void ConfirmUnlock()
        {
            var def = _pendingUnlock;
            CloseUnlock();
            if (def == null || !roster.TryUnlock(ProfileStore.Profile, def)) return;
            ProfileStore.Save();
            player.RefreshUnlocks();
            GameEvents.RaiseWeaponUnlocked(def);
            RefreshTiles();
            RefreshEndUnlock();
            WeaponStation st = player.StationFor(def);
            if (st != null && director.Phase == MissionPhase.Running)
            {
                player.Select(st);
                Banner($"{def.displayName.ToUpperInvariant()} ONLINE", 2.5f, UiFactory.Accent);
            }
            else Flash($"{def.displayName} unlocked - it joins the defence next mission", 4f);
        }

        private void BuildUnlockConfirm()
        {
            _unlockConfirm = Overlay("UnlockConfirm");
            var card = _unlockConfirm.transform.Find("Card");
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 600f);
            var t = UiFactory.Text("Title", card, "UNLOCK WEAPON", 48, TextAnchor.UpperCenter, UiFactory.Accent);
            t.rectTransform.Stretch(new Vector2(0f, 0.82f), new Vector2(1f, 0.95f));
            _unlockConfirmText = UiFactory.Text("Body", card, "", 30, TextAnchor.UpperCenter);
            _unlockConfirmText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _unlockConfirmText.rectTransform.Stretch(new Vector2(0.07f, 0.3f), new Vector2(0.93f, 0.78f));
            var yes = UiFactory.Button("Confirm", card, "UNLOCK", 40, ConfirmUnlock, new Color(0.15f, 0.45f, 0.2f, 1f));
            yes.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 50f), new Vector2(340f, 110f));
            var no = UiFactory.Button("Cancel", card, "CANCEL", 40, CloseUnlock);
            no.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 50f), new Vector2(340f, 110f));
            _unlockConfirm.SetActive(false);
        }

        private void OnManualWeaponChanged(Data.WeaponDefinition def)
        {
            if (_markers != null) _markers.gun = player.mount;
            if (_radar != null) _radar.gun = player.mount;
            RefreshTiles();
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
            var card = UiFactory.PanelImage("Card", bg.transform, false, UiTheme.PanelSolid);
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
            _briefCredits = UiFactory.Text("Credits", card, "", 26, TextAnchor.MiddleCenter, UiFactory.Warn);
            _briefCredits.rectTransform.Stretch(new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.30f));
            var pr = ProfileStore.Profile;
            var nextW = roster.NextUnlock(pr);
            _briefCredits.text = $"Credits: {pr.credits:N0}" + (nextW != null ? $"   -   next unlock: {nextW.displayName} at {nextW.unlockCost:N0}" : "");
            var start = UiFactory.Button("Start", card, "START", 44, StartMission, new Color(0.15f, 0.45f, 0.2f, 1f));
            start.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(380f, 120f));

            // CC-BY-4.0 attribution (docs/ASSET_REGISTER.md §8). Moves to a Credits screen in M2.
            var credit = UiFactory.Text("Credit", card,
                "Models, textures, fonts and sounds: see Pause > Credits (CC BY 4.0 / CC0 / OFL; full list in the credits).",
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

            _sensText = Row(card, 0.79f, "Aim sensitivity", () => ChangeSensitivity(-0.1f), () => ChangeSensitivity(0.1f));
            _invertText = Row(card, 0.69f, "Invert vertical", () => { GameSettings.InvertY = !GameSettings.InvertY; RefreshSettings(); }, null);
            _assistText = Row(card, 0.59f, "Aim assist (lead ring)", () =>
            {
                GameSettings.Assist = GameSettings.Assist == AimAssist.Off ? AimAssist.Light : AimAssist.Off;
                RefreshSettings();
            }, null);
            _shakeText = Row(card, 0.49f, "Screen shake (recoil)", () => { GameSettings.ScreenShake = !GameSettings.ScreenShake; RefreshSettings(); }, null);
            _qualityText = Row(card, 0.39f, "Graphics quality", () =>
            {
                // AUTO -> LOW -> MEDIUM -> HIGH -> AUTO
                int q = GameSettings.QualityOverride;
                GameSettings.QualityOverride = q >= 2 ? -1 : q + 1;
                RefreshSettings();
            }, null);

            var resume = UiFactory.Button("Resume", card, "RESUME", 40, () => SetPaused(false), new Color(0.15f, 0.45f, 0.2f, 1f));
            resume.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 50f), new Vector2(340f, 110f));
            var restart = UiFactory.Button("Restart", card, "RESTART", 40, Restart);
            restart.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 50f), new Vector2(340f, 110f));

            if (Debug.isDebugBuild)
            {
                var stress = UiFactory.Button("Stress", card, "DEV: +20 THREATS", 24, () => { SetPaused(false); director.DebugSpawnStress(20); });
                stress.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(420f, 64f));
                stress.GetComponentInChildren<Text>().text = "DEV: +20 THREATS (no rewards)";
                var cr = UiFactory.Button("DevCredits", card, "DEV: +5,000 CREDITS", 24, () =>
                {
                    ProfileStore.Profile.credits += 5000;
                    ProfileStore.Save();
                    RefreshTiles();
                });
                cr.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(420f, 64f));
                // Diagnostics readout: hidden by default (VS-1); also toggled by a four-finger tap or F3.
                Button diag = null;
                diag = UiFactory.Button("DevDiagnostics", card, "", 24, () =>
                {
                    DiagnosticsOverlay.Toggle();
                    diag.GetComponentInChildren<Text>().text = DiagnosticsOverlay.Visible ? "DEV: DIAGNOSTICS ON" : "DEV: DIAGNOSTICS OFF";
                });
                diag.GetComponentInChildren<Text>().text = "DEV: DIAGNOSTICS OFF";
                diag.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 315f), new Vector2(420f, 64f));

                // Performance switches (dev builds): change one feature live, then measure (tools/measure_fps.sh).
                (string, System.Func<string>, System.Func<string>)[] perf =
                {
                    ("DevMsaa", DevPerfToggles.Msaa, () => DevPerfToggles.MsaaLabel),
                    ("DevScale", DevPerfToggles.Scale, () => DevPerfToggles.ScaleLabel),
                    ("DevShadows", DevPerfToggles.Shadows, () => DevPerfToggles.ShadowsLabel),
                    ("DevPost", DevPerfToggles.Post, () => DevPerfToggles.PostLabel),
                    ("DevBeams", DevPerfToggles.Beams, () => DevPerfToggles.BeamsLabel),
                };
                for (int i = 0; i < perf.Length; i++)
                {
                    var (id, toggle, label) = perf[i];
                    Button b = null;
                    b = UiFactory.Button(id, _pause.transform, label(), 22, () => b.GetComponentInChildren<Text>().text = toggle());
                    b.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 2) * 230f, 30f), new Vector2(210f, 60f));
                }
            }

            var creditsBtn = UiFactory.Button("Credits", card, "CREDITS", 26, () => _credits.SetActive(true));
            creditsBtn.GetComponent<RectTransform>().Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(200f, 70f));
            var replay = UiFactory.Button("ReplayOpening", card, "REPLAY OPENING", 22, () =>
            {
                Cinematic.CinematicDirector.ForceNextLoad = true;
                Restart();
            });
            replay.GetComponent<RectTransform>().Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(240f, 70f));

            RefreshSettings();
            _pause.SetActive(false);
            BuildCredits();
        }

        /// <summary>Credits and third-party licences (CC-BY requires this; ASSET_REGISTER §8).</summary>
        private void BuildCredits()
        {
            _credits = Overlay("Credits");
            var card = _credits.transform.Find("Card");
            var t = UiFactory.Text("Title", card, "CREDITS", 46, TextAnchor.UpperCenter, UiFactory.Accent);
            t.rectTransform.Stretch(new Vector2(0f, 0.89f), new Vector2(1f, 0.97f));
            string body = credits != null ? Plain(credits.text) : "Credits file missing.";
            var b = UiFactory.Text("Body", card, body, 17, TextAnchor.UpperLeft);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            b.verticalOverflow = VerticalWrapMode.Truncate;
            b.rectTransform.Stretch(new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.88f));
            var close = UiFactory.Button("Close", card, "CLOSE", 34, () => _credits.SetActive(false), new Color(0.15f, 0.45f, 0.2f, 1f));
            close.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 90f));
            _credits.SetActive(false);
        }

        /// <summary>Strips simple Markdown so the credits read cleanly in-game.</summary>
        private static string Plain(string md)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string raw in md.Split('\n'))
            {
                string l = raw.TrimEnd('\r');
                if (l.StartsWith("# ")) continue;                 // page title (shown as the header)
                l = l.Replace("**", "").Replace("`", "");
                if (l.StartsWith("## ")) l = l.Substring(3).ToUpperInvariant();
                l = l.Replace("  - ", "     ").Replace("- ", "• ");
                sb.AppendLine(l);
            }
            return sb.ToString().Trim();
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
            // Left: performance. Right: credits earned. Below: next-unlock progress (ECONOMY §2, GDD §7).
            _endBody = UiFactory.Text("Body", card, "", 26, TextAnchor.UpperLeft);
            _endBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endBody.rectTransform.Stretch(new Vector2(0.06f, 0.30f), new Vector2(0.52f, 0.82f));
            _endRewards = UiFactory.Text("Rewards", card, "", 26, TextAnchor.UpperLeft, UiFactory.Warn);
            _endRewards.horizontalOverflow = HorizontalWrapMode.Wrap;
            _endRewards.rectTransform.Stretch(new Vector2(0.56f, 0.30f), new Vector2(0.95f, 0.82f));

            var barBg = UiFactory.Image("UnlockBar", card, new Color(1f, 1f, 1f, 0.12f), UiFactory.White);
            barBg.rectTransform.Stretch(new Vector2(0.06f, 0.215f), new Vector2(0.94f, 0.245f));
            _unlockFill = UiFactory.Image("Fill", barBg.transform, UiFactory.Warn, UiFactory.White);
            _unlockFill.rectTransform.Stretch(Vector2.zero, Vector2.one);
            _unlockFill.type = Image.Type.Filled;
            _unlockFill.fillMethod = Image.FillMethod.Horizontal;
            _unlockText = UiFactory.Text("UnlockText", card, "", 22, TextAnchor.MiddleLeft);
            _unlockText.rectTransform.Stretch(new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.30f));
            var retry = UiFactory.Button("Retry", card, "PLAY AGAIN", 40, Restart, new Color(0.15f, 0.45f, 0.2f, 1f));
            retry.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(400f, 110f));
            _endUnlockBtn = UiFactory.Button("UnlockNow", card, "UNLOCK", 30, () =>
            {
                var next = roster.NextUnlock(ProfileStore.Profile);
                if (next != null && roster.CanUnlock(ProfileStore.Profile, next)) AskUnlock(next);
            }, new Color(0.55f, 0.4f, 0.1f, 1f)).gameObject;
            _endUnlockBtn.GetComponent<RectTransform>().Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 60f), new Vector2(300f, 90f));
            _endUnlockBtn.SetActive(false);
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

            float h = director.Health01;
            Color hc = h > 0.5f ? UiTheme.System : h > 0.25f ? UiTheme.Caution : UiTheme.Critical;
            _healthFill.fillAmount = h;
            _healthFill.color = hc;
            _healthValue.text = $"{h * 100f:0}%";
            _healthValue.color = h > 0.5f ? UiTheme.Ink : hc;

            WeaponState s = player.State;
            _ammoName.text = s.Def.displayName.ToUpperInvariant();
            _ammoText.text = s.Reloading ? $"{(1f - s.ReloadProgress01) * s.Def.reloadTime:0.0}s" : $"{s.Ammo}<size={UiTheme.Caption}> / {s.Def.magazineSize}</size>";
            _ammoText.supportRichText = true;
            _ammoFill.fillAmount = s.Reloading ? s.ReloadProgress01 : (float)s.Ammo / s.Def.magazineSize;
            _ammoFill.color = s.Reloading ? UiTheme.Caution : UiTheme.System;
            _heatFill.fillAmount = s.Heat01;
            _heatFill.color = s.Overheated ? UiTheme.Critical : UiTheme.Caution;
            _statusText.text = s.Reloading ? "RELOADING" : s.Overheated ? "OVERHEAT" : s.Heat01 > 0.7f ? "BARREL HOT" : "";
            _statusText.color = s.Overheated ? UiTheme.Critical : UiTheme.Caution;

            // FIRE: pressed fill; when it cannot fire, a grey ring and the reason.
            bool blocked = s.Reloading || s.Overheated;
            _fireImage.color = input.FireHeld && !blocked ? UiTheme.Critical.WithAlpha(0.8f) : new Color(0.05f, 0.03f, 0.03f, 0.5f);
            _fireRing.color = blocked ? UiTheme.Disabled : UiTheme.Critical;
            _fireLabel.text = s.Reloading ? "RELOAD" : s.Overheated ? "HOT" : "FIRE";
            _fireLabel.color = blocked ? UiTheme.Caution : UiTheme.Ink;

            RefreshTiles();

            if (_bannerCard.gameObject.activeSelf && Time.unscaledTime > _bannerUntil)
            {
                _banner.text = "";
                _bannerCard.gameObject.SetActive(false);
            }

            UpdatePrompts();
        }

        private void LateUpdate()
        {
            // Runs after the camera has moved this frame (execution order), so the crosshair never lags.
            if (_crosshair == null || cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(player.AimPoint);
            _crosshair.gameObject.SetActive(sp.z > 0f);
            _crosshair.anchoredPosition = new Vector2(sp.x, sp.y) / _canvas.scaleFactor;
        }

        // ------------------------------------------------------- cinematic hand-over

        /// <summary>
        /// HUD elements grouped for the staged reveal: 0 radar + health, 1 warning banner,
        /// 2 weapon status (tiles, ammo), 3 controls and aiming aids.
        /// </summary>
        private static int StageOf(string name)
        {
            if (name == "Radar" || name == "HealthBar") return 0;
            if (name == "Banner") return 1;
            if (name.StartsWith("Tile_") || name == "Ammo") return 2;
            if (name == "Briefing" || name == "Pause" || name == "AfterAction" || name == "Credits" || name == "UnlockConfirm") return -1;
            return 3;
        }

        private void SetHudStageAlpha(int stage, float alpha)
        {
            foreach (Transform child in _safe)
            {
                if (StageOf(child.name) != stage) continue;
                var cg = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
                cg.interactable = cg.blocksRaycasts = alpha > 0.5f;
            }
        }

        /// <summary>Called by the cinematic at the hand-over: reveal the HUD in stages, then start Mission 1.</summary>
        public void RevealAfterCinematic(Cinematic.CinematicDirector cinematic) => StartCoroutine(Reveal(cinematic));

        private System.Collections.IEnumerator Reveal(Cinematic.CinematicDirector cinematic)
        {
            float[] at = { 0.2f, 1.0f, 1.8f, 2.6f };
            for (int stage = 0; stage < 4; stage++)
            {
                while (cinematic.HandoverElapsed < at[stage]) yield return null;
                for (float a = 0f; a <= 1f; a += Time.unscaledDeltaTime / 0.35f)
                {
                    SetHudStageAlpha(stage, a);
                    yield return null;
                }
                SetHudStageAlpha(stage, 1f);
                if (stage == 1) Banner("AIR RAID WARNING RED", 2.5f, UiFactory.Danger);
            }
            StartMission();
        }

        private enum PromptAt { Aim, Fire, Tip }

        private void UpdatePrompts()
        {
            if (director.Phase != MissionPhase.Running) { HidePrompt(); return; }

            if (!_dragged)
            {
                ShowPrompt("<  DRAG HERE TO AIM  >", PromptAt.Aim);
                return;
            }
            if (!_fired && threats.Active.Count > 0)
            {
                ShowPrompt("HOLD TO FIRE", PromptAt.Fire);
                return;
            }
            if (!_hintedLead && player.ManualRoundsFired >= 60 && director.Destroyed == 0)
            {
                _hintedLead = true;
                Flash(GameSettings.Assist == AimAssist.Light
                    ? "Aim at the amber ring - it leads moving targets"
                    : "Aim ahead of moving targets", 5f);
            }
            else if (!_hintedReload && player.State.Reloading)
            {
                _hintedReload = true;
                Flash("Tap the ammo panel to reload early", 4f);
            }
            if (Time.unscaledTime > _promptUntil) HidePrompt();
        }

        /// <summary>Shows the prompt card next to the control it explains, never over the weapon or the aim point.</summary>
        private void ShowPrompt(string text, PromptAt at)
        {
            _prompt.text = text;
            _promptCard.gameObject.SetActive(true);
            switch (at)
            {
                case PromptAt.Aim:  // inside the left aim zone
                    _promptCard.Anchor(new Vector2(0.27f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 64f));
                    break;
                case PromptAt.Fire: // left of the FIRE button
                    _promptCard.Anchor(new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-262f, 140f), new Vector2(250f, 64f));
                    break;
                default:            // above the ammo panel
                    _promptCard.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 124f), new Vector2(620f, 60f));
                    break;
            }
        }

        private void HidePrompt()
        {
            if (_promptCard.gameObject.activeSelf) _promptCard.gameObject.SetActive(false);
            _prompt.text = "";
        }

        private void Flash(string text, float seconds)
        {
            ShowPrompt(text, PromptAt.Tip);
            _promptUntil = Time.unscaledTime + seconds;
        }

        private void Banner(string text, float seconds, Color color)
        {
            _banner.text = text;
            _banner.color = color;
            _bannerCard.gameObject.SetActive(true);
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
                tip = "Accuracy was low. Fire short bursts and aim at the amber lead ring instead of directly at the target.";
            else
                tip = "Clean defence. Next: try with aim assist off, or the DEV stress test from the pause menu.";

            _endBody.text =
                $"Grade: {grade}\n" +
                $"Time: {r.duration:0} s\n" +
                $"Installation: {r.healthRemaining01 * 100f:0}%\n" +
                $"Destroyed: {r.threatsDestroyed} / {r.threatsTotal}  (reached base: {r.threatsLeaked})\n" +
                $"Accuracy: {r.Accuracy * 100f:0.0}%  ({r.roundsHit}/{r.roundsFired})\n\n" +
                tip;

            // Credits earned (already added to the profile and saved by MissionDirector).
            var sb = new System.Text.StringBuilder("CREDITS EARNED\n");
            var rw = director.Rewards;
            if (rw == null || rw.lines.Count == 0)
                sb.Append(director.StressTestUsed ? "None (dev stress test used)\n" : "None this time\n");
            else
                foreach (var line in rw.lines) sb.Append($"{line.label}:  +{line.credits:N0}\n");
            sb.Append($"\nTOTAL  +{rw?.Total ?? 0:N0}\nBalance: {ProfileStore.Profile.credits:N0} cr");
            _endRewards.text = sb.ToString();

            RefreshEndUnlock();
            RefreshTiles();
        }

        /// <summary>Next-unlock progress on the after-action screen, with an UNLOCK button once affordable.</summary>
        private void RefreshEndUnlock()
        {
            var p = ProfileStore.Profile;
            var next = roster.NextUnlock(p);
            if (next != null)
            {
                bool afford = p.credits >= next.unlockCost;
                _unlockFill.fillAmount = Mathf.Clamp01((float)p.credits / next.unlockCost);
                _unlockText.text = afford
                    ? $"{next.displayName}: enough credits ({next.unlockCost:N0} cr)"
                    : $"Next unlock: {next.displayName}  {p.credits:N0} / {next.unlockCost:N0} cr";
                _endUnlockBtn.SetActive(afford);
                _endUnlockBtn.GetComponentInChildren<Text>().text = $"UNLOCK {next.displayName.Replace("2K22 ", "").ToUpperInvariant()}";
            }
            else
            {
                _unlockFill.fillAmount = 1f;
                _unlockText.text = "All weapon systems unlocked";
                _endUnlockBtn.SetActive(false);
            }
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
            _qualityText.text = QualityTiers.Label(GameSettings.QualityOverride);
            _shakeText.text = GameSettings.ScreenShake ? "ON" : "OFF";
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

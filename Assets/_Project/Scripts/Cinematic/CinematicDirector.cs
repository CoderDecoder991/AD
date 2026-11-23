using System;
using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Missions;
using AirDefense.UI;
using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.Cinematic
{
    /// <summary>
    /// First-launch opening cinematic (docs/CINEMATIC_OPENING.md): six shots over ~45 s, played in the
    /// gameplay scene so it hands over to Mission 1 with no loading. Camera moves are scripted keyframes
    /// relative to the base layout; audio cues and subtitles are timed events. Skippable after 1 s;
    /// plays automatically only on first launch; replayable from the pause menu.
    /// </summary>
    [DefaultExecutionOrder(300)] // LateUpdate after the gun controller and HUD, so the cinematic owns the camera
    public sealed class CinematicDirector : MonoBehaviour
    {
        public Camera cam;
        public ManualWeaponController player;
        public MissionDirector director;
        public HudController hud;
        public ProjectileSystem projectiles;
        public AudioBank bank;
        public AudioClip radioSquelch;
        public Material navLightMaterial;
        public Mesh sphereMesh;

        [Header("Scene anchors")]
        public Transform radarDish;
        public Transform cram;
        public Transform tunguska;
        public Transform gunRoot;

        /// <summary>Set by the pause menu before reloading the scene to replay the opening.</summary>
        public static bool ForceNextLoad;
        /// <summary>True while the cinematic is running (HUD and input stay hidden).</summary>
        public static bool Playing { get; private set; }
        /// <summary>Base alarm state: false until the radar shot raises the alert.</summary>
        public static bool AlertRaised { get; private set; } = true;

        private const float Total = 45f;
        private const float HandoverStart = 39f;
        private float _t;
        private bool _handedOver;
        private Vector3 _lastPos;
        private Quaternion _lastRot;
        private float _lastFov = 50f;

        private sealed class Shot
        {
            public float start, end;
            public Func<float, (Vector3 pos, Vector3 look, float fov)> pose; // t = 0..1 within the shot
        }

        private readonly List<Shot> _shots = new List<Shot>();
        private readonly List<(float at, Action act, bool done)> _cues = new List<(float, Action, bool)>();

        // Overlay UI
        private Canvas _overlay;
        private Image _fade;
        private RectTransform _barTop, _barBottom;
        private Text _subtitle;
        private float _subtitleUntil;
        private GameObject _skip;
        private RectTransform _radar;
        private RectTransform _sweep;
        private readonly List<Image> _blips = new List<Image>();
        private Text _radarLabel;

        // Audio
        private AudioSource _siren, _fx, _radio;
        private Light[] _floods;
        private float[] _floodIntensity;
        private Transform _decoy;

        private void Awake()
        {
            bool play = ForceNextLoad || !ProfileStore.Profile.cinematicSeen;
            ForceNextLoad = false;
            Playing = play;
            AlertRaised = !play;
            if (!play) { enabled = false; return; }
        }

        private void Start()
        {
            if (!Playing) return;
            player.InputEnabled = false;
            BuildOverlay();
            BuildAudio();
            BuildShots();
            BuildCues();
            gunRoot.GetComponent<GunMount>().SetTarget(0f, 0f); // barrels level until the reveal

            // Floodlights on the gun pit start dark and switch on in shot 4.
            var lights = new List<Light>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l.name == "FloodSpot") lights.Add(l);
            _floods = lights.ToArray();
            _floodIntensity = new float[_floods.Length];
            for (int i = 0; i < _floods.Length; i++) { _floodIntensity[i] = _floods[i].intensity; _floods[i].intensity = 0f; }
        }

        // ------------------------------------------------------------------ shots

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private void Add(float start, float end, Vector3 p0, Vector3 p1, Vector3 l0, Vector3 l1, float f0, float f1)
        {
            _shots.Add(new Shot
            {
                start = start, end = end,
                pose = t => { float e = Ease(t); return (Vector3.Lerp(p0, p1, e), Vector3.Lerp(l0, l1, e), Mathf.Lerp(f0, f1, e)); }
            });
        }

        private void BuildShots()
        {
            Vector3 g = gunRoot.position;              // top of the ZU mound
            Vector3 dish = radarDish != null ? radarDish.position : new Vector3(-8f, 21f, 45f);
            Vector3 c = cram != null ? cram.position : new Vector3(-14f, 9f, -8f);
            Vector3 tg = tunguska != null ? tunguska.position : new Vector3(14f, 7f, -10f);

            // 1: black (camera parked on shot 2's start).
            Vector3 s2a = new Vector3(135f, 22f, -175f), s2b = new Vector3(78f, 17f, -118f);
            Vector3 l2a = new Vector3(-20f, 30f, 120f), l2b = new Vector3(-12f, 16f, 60f);
            Add(0f, 5f, s2a, s2a, l2a, l2a, 50f, 50f);
            // 2: low push-in from outside the south-east fence, across the base toward the hills and
            //    the night sky; searchlights sweep in the distance (looking up, so sky and horizon show).
            Add(5f, 13f, s2a, s2b, l2a, l2b, 50f, 46f);
            // 3: close on the radar mast, slow orbit (radar scope overlay on screen).
            Add(13f, 21f, dish + new Vector3(-16f, -1f, -14f), dish + new Vector3(10f, 2f, -16f), dish, dish + Vector3.up * 0.5f, 38f, 34f);
            // 4a: C-RAM on its mound, low angle from the open south-east side (clear of the hangar roof).
            Add(21f, 24f, c + new Vector3(9f, 2.5f, -13f), c + new Vector3(6.5f, 3f, -10f), c + Vector3.up * 2.5f, c + Vector3.up * 3f, 40f, 38f);
            // 4b: Tunguska, low angle across the front.
            Add(24f, 27f, tg + new Vector3(10f, 3f, -12f), tg + new Vector3(7f, 3.5f, -11f), tg + Vector3.up * 2f, tg + Vector3.up * 2.2f, 42f, 40f);
            // 4c: floodlights switching on over the ZU pit.
            Add(27f, 30f, g + new Vector3(13f, 4f, -11f), g + new Vector3(11f, 3.5f, -9f), g + Vector3.up * 1.2f, g + Vector3.up * 1.5f, 45f, 44f);
            // 5: low dolly around the ZU-23-2 from front-right to behind it.
            _shots.Add(new Shot
            {
                start = 30f, end = HandoverStart,
                pose = t =>
                {
                    float e = Ease(t);
                    float ang = Mathf.Lerp(60f, 180f, e) * Mathf.Deg2Rad;   // around the gun, ending behind it
                    float r = Mathf.Lerp(6.5f, 8f, e);
                    Vector3 pos = g + new Vector3(Mathf.Sin(ang) * r, Mathf.Lerp(1.3f, 2.8f, e), Mathf.Cos(ang) * r);
                    Vector3 look = Vector3.Lerp(g + Vector3.up * 1.2f, g + new Vector3(0f, 6f, 60f), e * e);
                    return (pos, look, Mathf.Lerp(42f, 58f, e));
                }
            });
        }

        private (Vector3 pos, Quaternion rot, float fov) PoseAt(float time)
        {
            foreach (var s in _shots)
            {
                if (time < s.start || time >= s.end) continue;
                var p = s.pose(Mathf.InverseLerp(s.start, s.end, time));
                return (p.pos, Quaternion.LookRotation(p.look - p.pos, Vector3.up), p.fov);
            }
            return (_lastPos, _lastRot, _lastFov);
        }

        // ------------------------------------------------------------------- cues

        private void Cue(float at, Action a) => _cues.Add((at, a, false));

        private void BuildCues()
        {
            Cue(0.6f, () => { Squelch(); Say("[RADIO] ...Northgate, Sentinel... multiple contacts... bearing zero-four-five...", 3.6f); });
            Cue(3.0f, () => _fx.PlayOneShot(bank.warningTone, 0.6f));
            Cue(3.8f, () => { _siren.volume = 0f; _siren.Play(); });
            Cue(9.6f, () => projectiles.SpawnAirburst(new Vector3(-420f, 360f, 2500f), 2700f));  // distant flak beyond the hills
            Cue(11.1f, () => projectiles.SpawnAirburst(new Vector3(-250f, 410f, 2650f), 2800f));
            Cue(13.0f, () => { AlertRaised = true; Squelch(); Say("[RADIO] All stations, all stations: air raid warning red. Man your positions.", 4.2f); });
            Cue(13.2f, () => _radar.gameObject.SetActive(true));
            Cue(21.0f, () => _radar.gameObject.SetActive(false));
            Cue(21.4f, () => _fx.PlayOneShot(bank.reload, 0.8f));
            Cue(27.6f, () => _fx.PlayOneShot(bank.emptyClick, 1f));     // switch clunk as the floodlights come on
            Cue(30.4f, () => { gunRoot.GetComponent<GunMount>().SetTarget(0f, 10f); _fx.PlayOneShot(bank.reload, 0.9f); SpawnDecoy(); });
            Cue(31.2f, () => { Squelch(); Say("[RADIO] Gun One, contacts north-east, low and slow. Weapons free.", 3.8f); });
            Cue(HandoverStart, BeginHandover);
        }

        private void Say(string text, float seconds)
        {
            _subtitle.text = text;
            _subtitleUntil = _t + seconds;
        }

        private void Squelch()
        {
            if (radioSquelch != null) _radio.PlayOneShot(radioSquelch, 0.7f);
        }

        // ------------------------------------------------------------- update loop

        private void Update()
        {
            if (!Playing) return;
            _t += Time.deltaTime;

            for (int i = 0; i < _cues.Count; i++)
            {
                var c = _cues[i];
                if (c.done || _t < c.at) continue;
                c.act();
                _cues[i] = (c.at, c.act, true);
            }

            // Fade from black over 5-6.5 s, letterbox until the handover.
            float fade = _t < 5f ? 1f : Mathf.Clamp01(1f - (_t - 5f) / 1.5f);
            if (_handedOver) fade = 0f;
            _fade.color = new Color(0f, 0f, 0f, fade);

            // Siren swells, holds, then fades as control is handed over.
            if (_siren.isPlaying)
                _siren.volume = _t < 8f ? Mathf.Clamp01((_t - 3.8f) / 4f) * 0.55f
                              : _t < HandoverStart ? 0.55f
                              : Mathf.Max(0f, 0.55f * (1f - (_t - HandoverStart) / 3f));

            // Floodlights come on through 27.6-29 s.
            float on = Mathf.Clamp01((_t - 27.6f) / 1.2f);
            for (int i = 0; i < _floods.Length; i++)
                _floods[i].intensity = _floodIntensity[i] * on * (on < 1f ? (0.6f + 0.4f * Mathf.PerlinNoise(_t * 20f, i)) : 1f);

            // Radar scope: sweep and contacts appearing one by one.
            if (_radar.gameObject.activeSelf)
            {
                _sweep.localRotation = Quaternion.Euler(0f, 0f, -_t * 120f);
                int shown = Mathf.Clamp(Mathf.FloorToInt((_t - 13.6f) / 0.9f) + 1, 0, _blips.Count);
                for (int i = 0; i < _blips.Count; i++) _blips[i].enabled = i < shown && Mathf.Repeat(_t * 1.5f + i * 0.3f, 1f) < 0.8f;
                _radarLabel.text = shown > 0 ? $"UNKNOWN CONTACTS x{shown}" : "SCANNING...";
            }

            if (_subtitle.text.Length > 0 && _t > _subtitleUntil) _subtitle.text = "";
            _skip.SetActive(_t > 1f && !_handedOver);

            if (_decoy != null) _decoy.position += new Vector3(-6f, 0f, -14f) * Time.deltaTime;

            if (_t >= Total) Finish();
        }

        private void LateUpdate()
        {
            if (!Playing) return;
            // The gun controller has already placed the camera at its gameplay pose this frame.
            Vector3 gamePos = cam.transform.position;
            Quaternion gameRot = cam.transform.rotation;
            float gameFov = cam.fieldOfView;

            if (!_handedOver)
            {
                var p = PoseAt(_t);
                _lastPos = p.pos; _lastRot = p.rot; _lastFov = p.fov;
                cam.transform.SetPositionAndRotation(p.pos, p.rot);
                cam.fieldOfView = p.fov;
            }
            else
            {
                // Shot 6: blend from the last cinematic pose into the live gameplay camera.
                float k = Ease(Mathf.Clamp01((_t - HandoverStart) / 1.6f));
                cam.transform.SetPositionAndRotation(Vector3.Lerp(_lastPos, gamePos, k), Quaternion.Slerp(_lastRot, gameRot, k));
                cam.fieldOfView = Mathf.Lerp(_lastFov, gameFov, k);
            }

            float bars = _handedOver ? Mathf.Clamp01(1f - (_t - HandoverStart) / 1.2f) : 1f;
            _barTop.localScale = _barBottom.localScale = new Vector3(1f, bars, 1f);
        }

        // ---------------------------------------------------------- handover / skip

        private void BeginHandover()
        {
            if (_handedOver) return;
            _handedOver = true;
            _subtitle.text = "";
            _radar.gameObject.SetActive(false);
            if (_decoy != null) Destroy(_decoy.gameObject);
            hud.RevealAfterCinematic(this);
        }

        /// <summary>Skip: jump straight to the handover blend (lights on, gun up, alert raised).</summary>
        public void Skip()
        {
            if (_handedOver) return;
            AlertRaised = true;
            for (int i = 0; i < _floods.Length; i++) _floods[i].intensity = _floodIntensity[i];
            gunRoot.GetComponent<GunMount>().SetTarget(0f, 10f);
            for (int i = 0; i < _cues.Count; i++) _cues[i] = (_cues[i].at, _cues[i].act, true);
            _t = HandoverStart;
            BeginHandover();
        }

        private void Finish()
        {
            Playing = false;
            ProfileStore.Profile.cinematicSeen = true;
            ProfileStore.Save();
            _siren.Stop();
            if (_overlay != null) Destroy(_overlay.gameObject);
            enabled = false;
        }

        /// <summary>Seconds since the handover began (for the HUD's staged reveal).</summary>
        public float HandoverElapsed => _handedOver ? _t - HandoverStart : -1f;

        // ---------------------------------------------------------------- building

        private void SpawnDecoy()
        {
            // A blinking contact far to the north-east: the first threat, seen before control is handed over.
            if (navLightMaterial == null || sphereMesh == null) return;
            var go = new GameObject("Cinematic contact");
            go.transform.position = new Vector3(260f, 150f, 1500f);
            go.transform.localScale = Vector3.one * 7f;
            go.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = navLightMaterial;
            go.AddComponent<Environment.NavLightBlink>();
            _decoy = go.transform;
        }

        private void BuildAudio()
        {
            _siren = gameObject.AddComponent<AudioSource>();
            _siren.clip = bank.siren;
            _siren.loop = true;
            _siren.playOnAwake = false;
            _fx = gameObject.AddComponent<AudioSource>();
            _fx.playOnAwake = false;
            _radio = gameObject.AddComponent<AudioSource>();
            _radio.playOnAwake = false;
        }

        private void BuildOverlay()
        {
            var go = new GameObject("Cinematic Overlay", typeof(RectTransform));
            _overlay = go.AddComponent<Canvas>();
            _overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlay.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)go.transform;

            // Letterbox (about 2.2:1) and the black fade.
            _barTop = UiFactory.Image("BarTop", root, Color.black).rectTransform;
            _barTop.Stretch(new Vector2(0f, 0.88f), Vector2.one);
            _barTop.pivot = new Vector2(0.5f, 1f);
            _barBottom = UiFactory.Image("BarBottom", root, Color.black).rectTransform;
            _barBottom.Stretch(Vector2.zero, new Vector2(1f, 0.12f));
            _barBottom.pivot = new Vector2(0.5f, 0f);
            _fade = UiFactory.Image("Fade", root, Color.black);
            _fade.rectTransform.Stretch(Vector2.zero, Vector2.one);

            _subtitle = UiFactory.Text("Subtitle", root, "", 30, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.9f));
            _subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _subtitle.rectTransform.Stretch(new Vector2(0.1f, 0.015f), new Vector2(0.9f, 0.11f));

            // Radar scope (shot 3).
            _radar = UiFactory.Image("RadarScope", root, new Color(0.02f, 0.1f, 0.05f, 0.85f), UiFactory.Circle).rectTransform;
            _radar.Anchor(new Vector2(0.75f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f));
            foreach (float k in new[] { 1f, 0.66f, 0.33f })
            {
                var ring = UiFactory.Image("Ring", _radar, new Color(0.3f, 0.95f, 0.4f, 0.4f), UiFactory.Ring).rectTransform;
                ring.Stretch(new Vector2(0.5f - k / 2f, 0.5f - k / 2f), new Vector2(0.5f + k / 2f, 0.5f + k / 2f));
            }
            _sweep = UiFactory.Image("Sweep", _radar, new Color(0.5f, 1f, 0.6f, 0.8f)).rectTransform;
            _sweep.anchorMin = _sweep.anchorMax = new Vector2(0.5f, 0.5f);
            _sweep.pivot = new Vector2(0.5f, 0f);
            _sweep.sizeDelta = new Vector2(3f, 200f);
            var rng = new System.Random(6);
            for (int i = 0; i < 6; i++)
            {
                var b = UiFactory.Image("Contact", _radar, new Color(1f, 0.75f, 0.25f), UiFactory.Circle);
                float a = (25f + (float)rng.NextDouble() * 45f) * Mathf.Deg2Rad;  // north-east sector
                float r = 120f + (float)rng.NextDouble() * 80f;
                b.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r), new Vector2(14f, 14f));
                b.enabled = false;
                _blips.Add(b);
            }
            _radarLabel = UiFactory.Text("Label", _radar, "", 26, TextAnchor.UpperCenter, new Color(1f, 0.4f, 0.3f));
            _radarLabel.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(520f, 40f));
            _radar.gameObject.SetActive(false);

            // Skip.
            var skip = UiFactory.Button("Skip", root, "SKIP  >>", 30, Skip, new Color(0f, 0f, 0f, 0.55f));
            skip.GetComponent<RectTransform>().Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(200f, 80f));
            _skip = skip.gameObject;
            _skip.SetActive(false);
        }
    }
}

using System.Collections.Generic;
using MergeCafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MergeCafe.Suika
{
    /// <summary>
    /// The watermelon game (수박게임): drop fruits with the left mouse button; when two
    /// equal fruits touch they merge into the next size. Built at runtime in world space
    /// (2D physics) with a screen-space HUD. See <see cref="SuikaCatalog"/>.
    /// </summary>
    public sealed class SuikaGame : MonoBehaviour
    {
        // Container geometry (world units).
        private const float HalfW = 2.75f;
        private const float FloorY = -4f;
        private const float WallTop = 5f;
        private const float DropY = 4.4f;
        private const float DangerY = 3.6f;
        private const float WallThk = 0.4f;
        private const float DropCooldown = 0.35f;

        private Camera _cam;
        private Transform _root;
        private PhysicsMaterial2D _mat;

        private readonly List<SuikaFruit> _fruits = new List<SuikaFruit>();
        private SuikaFruit _held;
        private int _currentLevel;
        private int _nextLevel;
        private float _cooldown;
        private float _overflowTimer;

        public int Score { get; private set; }
        public bool GameOver { get; private set; }
        public int FruitCount => _fruits.Count;

        private RectTransform _hudRoot;
        private TextMeshProUGUI _scoreText;
        private Image _nextIcon;
        private GameObject _overlay;

        public static SuikaGame Create()
        {
            var go = new GameObject("SuikaGame");
            var game = go.AddComponent<SuikaGame>();
            game.Build();
            return game;
        }

        private void Build()
        {
            _mat = new PhysicsMaterial2D("Fruit") { bounciness = 0.05f, friction = 0.55f };
            _root = new GameObject("SuikaRoot").transform;

            ConfigureCamera();
            BuildContainer();
            BuildHud();

            _currentLevel = RandomDropLevel();
            _nextLevel = RandomDropLevel();
            SpawnHeld();
            RefreshNextPreview();
        }

        private void ConfigureCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
                return;
            _cam.orthographic = true;
            _cam.transform.position = new Vector3(0f, 0.3f, -10f);
            _cam.backgroundColor = new Color(0.28f, 0.34f, 0.42f);
            FitCamera();
        }

        private void FitCamera()
        {
            if (_cam == null)
                return;
            float aspect = _cam.aspect;
            float needH = (WallTop - FloorY) * 0.5f + 0.6f;      // vertical span + margin
            float needW = (HalfW + WallThk + 0.6f);              // horizontal half-span + margin
            _cam.orthographicSize = Mathf.Max(needH, needW / Mathf.Max(aspect, 0.1f));
        }

        // ---- Container ----

        private void BuildContainer()
        {
            // Soft backdrop panel behind the fruits.
            var bg = MakeBar("Backdrop", 0f, (FloorY + WallTop) * 0.5f - 0.3f,
                HalfW * 2f + WallThk, WallTop - FloorY + 1f, new Color(0.9f, 0.92f, 0.86f, 1f), false);
            bg.GetComponent<SpriteRenderer>().sortingOrder = 0;

            MakeBar("Floor", 0f, FloorY, HalfW * 2f + WallThk * 2f, WallThk, UITheme.CellOpen, true);
            MakeBar("WallL", -(HalfW + WallThk * 0.5f), (FloorY + WallTop) * 0.5f, WallThk, WallTop - FloorY, UITheme.CellOpen, true);
            MakeBar("WallR", HalfW + WallThk * 0.5f, (FloorY + WallTop) * 0.5f, WallThk, WallTop - FloorY, UITheme.CellOpen, true);

            // Danger line (visual only).
            var danger = MakeBar("Danger", 0f, DangerY, HalfW * 2f, 0.05f, new Color(0.9f, 0.3f, 0.3f, 0.55f), false);
            danger.GetComponent<SpriteRenderer>().sortingOrder = 50;
        }

        private GameObject MakeBar(string name, float x, float y, float w, float h, Color color, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SuikaFruitSprites.Solid;
            sr.color = color;
            sr.sortingOrder = 1;

            if (solid)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                col.sharedMaterial = _mat;
            }
            return go;
        }

        // ---- Fruit flow ----

        private static int RandomDropLevel() => Random.Range(1, SuikaCatalog.MaxDropLevel + 1);

        private void SpawnHeld()
        {
            _held = SuikaFruit.Create(this, _currentLevel, new Vector2(0f, DropY), true, _mat, _root);
        }

        /// <summary>Spawns a fruit (optionally already dropped). Also used by tests.</summary>
        public SuikaFruit SpawnFruit(int level, Vector2 pos, bool dropped)
        {
            var f = SuikaFruit.Create(this, level, pos, !dropped, _mat, _root);
            if (dropped)
            {
                f.Drop();
                _fruits.Add(f);
            }
            return f;
        }

        private void Update()
        {
            FitCamera();

            if (GameOver)
                return;

            if (_held != null && _cam != null)
            {
                float r = SuikaCatalog.Radius(_currentLevel);
                Vector3 mouse = _cam.ScreenToWorldPoint(Input.mousePosition);
                float x = Mathf.Clamp(mouse.x, -HalfW + r, HalfW - r);
                _held.transform.position = new Vector3(x, DropY, 0f);

                if (Input.GetMouseButtonDown(0) && _cooldown <= 0f && !PointerOverHud())
                    DropHeld();
            }

            if (_cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0f && _held == null)
                    PrepareNext();
            }

            CheckGameOver();
        }

        private void DropHeld()
        {
            _held.Drop();
            _fruits.Add(_held);
            _held = null;
            _cooldown = DropCooldown;
        }

        private void PrepareNext()
        {
            _currentLevel = _nextLevel;
            _nextLevel = RandomDropLevel();
            SpawnHeld();
            RefreshNextPreview();
        }

        /// <summary>Merges two equal fruits into the next size (routed from collisions).</summary>
        public void RequestMerge(SuikaFruit a, SuikaFruit b)
        {
            if (a == null || b == null || a.Merged || b.Merged)
                return;
            a.Merged = true;
            b.Merged = true;

            Vector2 mid = (a.transform.position + b.transform.position) * 0.5f;
            _fruits.Remove(a);
            _fruits.Remove(b);
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            if (SuikaCatalog.IsMax(a.Level))
            {
                AddScore(SuikaCatalog.MergeScore(SuikaCatalog.Count) * 2); // two watermelons!
                return;
            }

            int newLevel = a.Level + 1;
            SpawnFruit(newLevel, mid, true);
            AddScore(SuikaCatalog.MergeScore(newLevel));
        }

        private void AddScore(int amount)
        {
            Score += amount;
            if (_scoreText != null)
                _scoreText.text = $"{Score:N0}";
        }

        private void CheckGameOver()
        {
            bool overflow = false;
            foreach (SuikaFruit f in _fruits)
            {
                if (f == null || !f.Dropped)
                    continue;
                // Give a freshly dropped fruit time to fall past the line.
                if (Time.time - f.DropTime < 1.2f)
                    continue;
                if (f.transform.position.y + f.WorldRadius > DangerY && f.Body.velocity.magnitude < 0.45f)
                {
                    overflow = true;
                    break;
                }
            }

            if (overflow)
            {
                _overflowTimer += Time.deltaTime;
                if (_overflowTimer >= 1.5f)
                    TriggerGameOver();
            }
            else
            {
                _overflowTimer = 0f;
            }
        }

        private void TriggerGameOver()
        {
            GameOver = true;
            if (_held != null)
            {
                Destroy(_held.gameObject);
                _held = null;
            }
            ShowOverlay();
        }

        public void Restart()
        {
            foreach (SuikaFruit f in _fruits)
                if (f != null) Destroy(f.gameObject);
            _fruits.Clear();
            if (_held != null) Destroy(_held.gameObject);
            _held = null;

            Score = 0;
            AddScore(0);
            GameOver = false;
            _overflowTimer = 0f;
            _cooldown = 0f;
            if (_overlay != null) { Destroy(_overlay); _overlay = null; }

            _currentLevel = RandomDropLevel();
            _nextLevel = RandomDropLevel();
            SpawnHeld();
            RefreshNextPreview();
        }

        private static void GoToMenu() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // ---- HUD ----

        private bool PointerOverHud() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void BuildHud()
        {
            var canvasGo = new GameObject("SuikaHud");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _hudRoot = (RectTransform)canvasGo.transform;

            // Score (top centre).
            var scoreLabel = UIFactory.CreateText(_hudRoot, "ScoreLabel", "점수", 30, UITheme.TextDim,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor((RectTransform)scoreLabel.transform, 0.5f, 1f, new Vector2(0f, -40f), new Vector2(200f, 44f));
            _scoreText = UIFactory.CreateText(_hudRoot, "Score", "0", 56, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor((RectTransform)_scoreText.transform, 0.5f, 1f, new Vector2(0f, -96f), new Vector2(360f, 70f));

            // Next preview (top right).
            Image nextPanel = UIFactory.CreateImage(_hudRoot, "NextPanel", new Color(0f, 0f, 0f, 0.28f));
            nextPanel.sprite = SpriteFactory.RoundedRect;
            nextPanel.type = Image.Type.Sliced;
            Anchor((RectTransform)nextPanel.transform, 1f, 1f, new Vector2(-110f, -110f), new Vector2(160f, 180f));
            var nextLabel = UIFactory.CreateText((RectTransform)nextPanel.transform, "NextLabel", "다음", 24,
                UITheme.TextMain, TextAnchor.UpperCenter, FontStyle.Bold);
            Anchor((RectTransform)nextLabel.transform, 0.5f, 1f, new Vector2(0f, -8f), new Vector2(140f, 34f));
            _nextIcon = UIFactory.CreateImage((RectTransform)nextPanel.transform, "NextIcon", Color.white);
            _nextIcon.preserveAspect = true;
            Anchor((RectTransform)_nextIcon.transform, 0.5f, 0.4f, Vector2.zero, new Vector2(96f, 96f));

            // Menu button (top left).
            Button menu = UIFactory.CreateButton(_hudRoot, "MenuButton", "메뉴", 26, UITheme.ButtonSecondary, out _);
            Anchor((RectTransform)menu.transform, 0f, 1f, new Vector2(90f, -46f), new Vector2(140f, 56f));
            menu.onClick.AddListener(GoToMenu);
        }

        private void RefreshNextPreview()
        {
            if (_nextIcon != null)
                _nextIcon.sprite = SuikaFruitSprites.Fruit(_nextLevel);
        }

        private void ShowOverlay()
        {
            Image dim = UIFactory.CreateImage(_hudRoot, "GameOver", new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Stretch((RectTransform)dim.transform);
            _overlay = dim.gameObject;

            Image panel = UIFactory.CreateImage((RectTransform)dim.transform, "Panel", UITheme.HudBg);
            panel.sprite = SpriteFactory.RoundedRect;
            panel.type = Image.Type.Sliced;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 380f);

            var title = UIFactory.CreateText(panelRect, "Title", "게임 오버", 48, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor((RectTransform)title.transform, 0.5f, 1f, new Vector2(0f, -70f), new Vector2(500f, 70f));
            var scoreText = UIFactory.CreateText(panelRect, "FinalScore", $"점수 {Score:N0}", 34, UITheme.TextGold,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor((RectTransform)scoreText.transform, 0.5f, 1f, new Vector2(0f, -150f), new Vector2(500f, 50f));

            Button retry = UIFactory.CreateButton(panelRect, "Retry", "다시하기", 28, UITheme.ButtonPrimary, out _);
            var retryRect = (RectTransform)retry.transform;
            retryRect.anchorMin = new Vector2(0f, 0f); retryRect.anchorMax = new Vector2(0.5f, 0f);
            retryRect.offsetMin = new Vector2(28f, 28f); retryRect.offsetMax = new Vector2(-12f, 92f);
            retry.onClick.AddListener(Restart);

            Button menu = UIFactory.CreateButton(panelRect, "Menu", "메뉴", 28, UITheme.ButtonSecondary, out _);
            var menuRect = (RectTransform)menu.transform;
            menuRect.anchorMin = new Vector2(0.5f, 0f); menuRect.anchorMax = new Vector2(1f, 0f);
            menuRect.offsetMin = new Vector2(12f, 28f); menuRect.offsetMax = new Vector2(-28f, 92f);
            menu.onClick.AddListener(GoToMenu);
        }

        private static void Anchor(RectTransform rect, float ax, float ay, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(ax, ay);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using MergeCafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MergeCafe.Bubble
{
    /// <summary>
    /// Bubble shooter: aim with the mouse, click to fire a bubble that bounces off the
    /// side walls and sticks to the field; 3+ same-colour connected bubbles pop, and any
    /// bubbles left hanging fall. Built at runtime in world space with a screen-space HUD.
    /// </summary>
    public sealed class BubbleShooterGame : MonoBehaviour
    {
        private const float BubbleR = 0.36f;
        private const int Cols = 9;
        private const int Rows = 13;
        private const int StartRows = 6;
        private static readonly float RowHeight = BubbleR * Mathf.Sqrt(3f);
        private const float TopY = 3.8f;
        private const float FieldHalf = Cols * BubbleR;      // 3.24
        private const float ShooterY = -4.2f;
        private const float DeadlineY = -3.0f;
        private const float Speed = 12f;

        private static Vector2 ShooterPos => new Vector2(0f, ShooterY);

        private Camera _cam;
        private Transform _root;
        private BubbleGrid _grid;
        private readonly Dictionary<int, GameObject> _views = new Dictionary<int, GameObject>();
        private readonly List<(int, int)> _nbuf = new List<(int, int)>();

        private LineRenderer _aim;
        private GameObject _shooterBubble;
        private GameObject _flying;
        private Vector2 _flyingPos;
        private Vector2 _flyingVel;
        private int _currentColor;
        private int _nextColor;

        public int Score { get; private set; }
        public bool GameOver { get; private set; }
        public bool Cleared { get; private set; }
        public BubbleGrid Grid => _grid;

        private RectTransform _hud;
        private TextMeshProUGUI _scoreText;
        private Image _nextIcon;
        private GameObject _overlay;

        public static BubbleShooterGame Create()
        {
            var go = new GameObject("BubbleShooterGame");
            var game = go.AddComponent<BubbleShooterGame>();
            game.Build();
            return game;
        }

        private void Build()
        {
            _root = new GameObject("BubbleRoot").transform;
            _grid = new BubbleGrid(Rows, Cols);

            ConfigureCamera();
            BuildFrame();
            BuildAim();
            BuildHud();
            FillInitialBoard();

            _currentColor = PickColor();
            _nextColor = PickColor();
            SpawnShooterBubble();
            RefreshNext();
        }

        private void ConfigureCamera()
        {
            _cam = Camera.main;
            if (_cam == null) return;
            _cam.orthographic = true;
            _cam.transform.position = new Vector3(0f, -0.02f, -10f);
            _cam.backgroundColor = new Color(0.16f, 0.19f, 0.27f);
            FitCamera();
        }

        private void FitCamera()
        {
            if (_cam == null) return;
            float needH = 4.75f;
            float needW = FieldHalf + 0.45f;
            _cam.orthographicSize = Mathf.Max(needH, needW / Mathf.Max(_cam.aspect, 0.1f));
        }

        // ---- geometry ----

        private static Vector2 CellToWorld(int row, int col)
        {
            float x = -FieldHalf + BubbleR + col * 2f * BubbleR + (row % 2 == 1 ? BubbleR : 0f);
            float y = TopY - row * RowHeight;
            return new Vector2(x, y);
        }

        // ---- field visuals ----

        private void BuildFrame()
        {
            Bar("BgField", 0f, (TopY + ShooterY) * 0.5f + 0.3f, FieldHalf * 2f + 0.3f,
                (TopY + 0.6f) - (ShooterY + 0.4f), new Color(0.11f, 0.13f, 0.2f, 1f), 0);
            float top = TopY + BubbleR;
            float bottom = ShooterY + 0.3f;
            float midY = (top + bottom) * 0.5f;
            float h = top - bottom;
            Bar("WallL", -(FieldHalf + 0.12f), midY, 0.24f, h, UITheme.CellOpen, 2);
            Bar("WallR", FieldHalf + 0.12f, midY, 0.24f, h, UITheme.CellOpen, 2);
            Bar("Ceiling", 0f, top + 0.1f, FieldHalf * 2f + 0.4f, 0.24f, UITheme.CellOpen, 2);
            Bar("Deadline", 0f, DeadlineY, FieldHalf * 2f, 0.04f, new Color(0.9f, 0.3f, 0.3f, 0.5f), 3);
        }

        private void Bar(string name, float x, float y, float w, float h, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BubbleSprites.Solid;
            sr.color = color;
            sr.sortingOrder = order;
        }

        private void BuildAim()
        {
            var go = new GameObject("Aim");
            go.transform.SetParent(_root, false);
            _aim = go.AddComponent<LineRenderer>();
            _aim.useWorldSpace = true;
            _aim.widthMultiplier = 0.06f;
            _aim.numCapVertices = 4;
            _aim.material = new Material(Shader.Find("Sprites/Default"));
            _aim.startColor = _aim.endColor = new Color(1f, 1f, 1f, 0.5f);
            _aim.sortingOrder = 20;
            _aim.positionCount = 0;
        }

        private void FillInitialBoard()
        {
            for (int r = 0; r < StartRows; r++)
                for (int c = 0; c < _grid.ColsInRow(r); c++)
                {
                    int color = Random.Range(0, BubbleCatalog.ColorCount);
                    _grid.Set(r, c, color);
                    CreateView(r, c, color);
                }
        }

        // ---- views ----

        private void CreateView(int row, int col, int color)
        {
            var go = new GameObject($"B_{row}_{col}");
            go.transform.SetParent(_root, false);
            go.transform.position = CellToWorld(row, col);
            float s = BubbleR / BubbleSprites.SpriteRadius;
            go.transform.localScale = new Vector3(s, s, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BubbleSprites.Bubble(color);
            sr.sortingOrder = 10;
            _views[_grid.Index(row, col)] = go;
        }

        private void RemoveView(int row, int col, bool pop)
        {
            int idx = _grid.Index(row, col);
            if (_views.TryGetValue(idx, out GameObject go))
            {
                _views.Remove(idx);
                if (go != null)
                {
                    if (pop) StartCoroutine(PopRoutine(go.transform));
                    else Destroy(go);
                }
            }
        }

        private static IEnumerator PopRoutine(Transform t)
        {
            Vector3 baseScale = t.localScale;
            const float dur = 0.14f;
            for (float e = 0f; e < dur; e += Time.deltaTime)
            {
                if (t == null) yield break;
                float p = e / dur;
                t.localScale = baseScale * (1f + 0.3f * p);
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - p);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // ---- input / loop ----

        private void Update()
        {
            FitCamera();
            if (GameOver || Cleared)
                return;

            if (_flying == null)
            {
                Vector2 dir = AimDirection();
                DrawAim(dir);
                if (Input.GetMouseButtonDown(0) && !PointerOverHud())
                    Fire(dir);
            }
            else
            {
                StepFlying(Time.deltaTime);
            }
        }

        private Vector2 AimDirection()
        {
            Vector2 dir = Vector2.up;
            if (_cam != null)
            {
                Vector2 mouse = _cam.ScreenToWorldPoint(Input.mousePosition);
                dir = (mouse - ShooterPos);
                if (dir.y < 0.25f) dir.y = 0.25f;   // keep the shot heading upward
                dir.Normalize();
            }
            return dir;
        }

        private void DrawAim(Vector2 dir)
        {
            List<Vector3> pts = AimPath(dir);
            _aim.positionCount = pts.Count;
            _aim.SetPositions(pts.ToArray());
        }

        private List<Vector3> AimPath(Vector2 dir)
        {
            var pts = new List<Vector3> { ShooterPos };
            Vector2 p = ShooterPos, d = dir;
            float remaining = 9f, lim = FieldHalf - BubbleR;
            for (int seg = 0; seg < 4 && remaining > 0.01f; seg++)
            {
                float tWall = float.PositiveInfinity;
                if (d.x > 0.001f) tWall = (lim - p.x) / d.x;
                else if (d.x < -0.001f) tWall = (-lim - p.x) / d.x;
                float tCeil = d.y > 0.001f ? (TopY - p.y) / d.y : float.PositiveInfinity;
                float t = Mathf.Min(remaining, Mathf.Min(tWall, tCeil));
                if (t < 0f) break;
                Vector2 np = p + d * t;
                pts.Add(np);
                remaining -= t;
                if (t >= tCeil) break;
                d.x = -d.x;
                p = np;
            }
            return pts;
        }

        private void Fire(Vector2 dir)
        {
            _aim.positionCount = 0;
            _flyingPos = ShooterPos;
            _flyingVel = dir * Speed;

            _flying = _shooterBubble;
            _shooterBubble = null;
            _flying.name = "Flying";
            _flying.transform.position = _flyingPos;
        }

        private void StepFlying(float dt)
        {
            int sub = Mathf.Clamp(Mathf.CeilToInt(_flyingVel.magnitude * dt / (BubbleR * 0.5f)), 1, 10);
            float sdt = dt / sub;
            float lim = FieldHalf - BubbleR;

            for (int i = 0; i < sub; i++)
            {
                _flyingPos += _flyingVel * sdt;
                if (_flyingPos.x < -lim) { _flyingPos.x = -lim; _flyingVel.x = Mathf.Abs(_flyingVel.x); }
                else if (_flyingPos.x > lim) { _flyingPos.x = lim; _flyingVel.x = -Mathf.Abs(_flyingVel.x); }

                if (TrySnap(_flyingPos, out int lr, out int lc))
                {
                    Land(lr, lc);
                    return;
                }
            }
            if (_flying != null)
                _flying.transform.position = _flyingPos;
        }

        private bool TrySnap(Vector2 pos, out int row, out int col)
        {
            if (pos.y >= TopY)
            {
                NearestEmptyCell(new Vector2(pos.x, TopY), out row, out col);
                return row >= 0;
            }
            float hitSq = (2f * BubbleR * 0.9f) * (2f * BubbleR * 0.9f);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < _grid.ColsInRow(r); c++)
                    if (_grid.IsOccupied(r, c) && ((Vector2)CellToWorld(r, c) - pos).sqrMagnitude < hitSq)
                    {
                        NearestEmptyCell(pos, out row, out col);
                        return row >= 0;
                    }
            row = -1; col = -1;
            return false;
        }

        private void NearestEmptyCell(Vector2 pos, out int row, out int col)
        {
            row = -1; col = -1;
            float best = float.PositiveInfinity;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < _grid.ColsInRow(r); c++)
                {
                    if (!_grid.IsEmptyCell(r, c) || !(r == 0 || HasOccupiedNeighbor(r, c)))
                        continue;
                    float d = ((Vector2)CellToWorld(r, c) - pos).sqrMagnitude;
                    if (d < best) { best = d; row = r; col = c; }
                }
        }

        private bool HasOccupiedNeighbor(int row, int col)
        {
            _grid.Neighbors(row, col, _nbuf);
            foreach (var (nr, nc) in _nbuf)
                if (_grid.IsOccupied(nr, nc)) return true;
            return false;
        }

        private void Land(int row, int col)
        {
            if (_flying != null) { Destroy(_flying); _flying = null; }

            if (row < 0)
            {
                PrepareNext();
                return;
            }

            _grid.Set(row, col, _currentColor);
            CreateView(row, col, _currentColor);
            Resolve(row, col);

            if (_grid.IsEmpty) { Win(); return; }
            if (CellToWorld(row, col).y <= DeadlineY) { Lose(); return; }

            PrepareNext();
        }

        private void Resolve(int row, int col)
        {
            List<(int, int)> group = _grid.SameColorGroup(row, col);
            if (group.Count < 3)
                return;

            foreach (var (r, c) in group) { _grid.Clear(r, c); RemoveView(r, c, true); }
            AddScore(group.Count * 10);

            List<(int, int)> floating = _grid.FindFloating();
            foreach (var (r, c) in floating) { _grid.Clear(r, c); RemoveView(r, c, true); }
            if (floating.Count > 0)
                AddScore(floating.Count * 20);
        }

        /// <summary>Places a colour and resolves matches without the flying phase (tests).</summary>
        public void TestLand(int row, int col, int color)
        {
            _currentColor = color;
            _grid.Set(row, col, color);
            CreateView(row, col, color);
            Resolve(row, col);
        }

        private int PickColor()
        {
            List<int> present = _grid.PresentColors();
            if (present.Count == 0)
                return Random.Range(0, BubbleCatalog.ColorCount);
            return present[Random.Range(0, present.Count)];
        }

        private void SpawnShooterBubble()
        {
            _shooterBubble = new GameObject("Shooter");
            _shooterBubble.transform.SetParent(_root, false);
            _shooterBubble.transform.position = ShooterPos;
            float s = BubbleR / BubbleSprites.SpriteRadius;
            _shooterBubble.transform.localScale = new Vector3(s, s, 1f);
            var sr = _shooterBubble.AddComponent<SpriteRenderer>();
            sr.sprite = BubbleSprites.Bubble(_currentColor);
            sr.sortingOrder = 15;
        }

        private void PrepareNext()
        {
            _currentColor = _nextColor;
            _nextColor = PickColor();
            SpawnShooterBubble();
            RefreshNext();
        }

        private void AddScore(int amount)
        {
            Score += amount;
            if (_scoreText != null) _scoreText.text = $"{Score:N0}";
        }

        private void Win()
        {
            Cleared = true;
            ShowOverlay("클리어! 🎉", true);
        }

        private void Lose()
        {
            GameOver = true;
            if (_shooterBubble != null) { Destroy(_shooterBubble); _shooterBubble = null; }
            ShowOverlay("게임 오버", false);
        }

        public void Restart()
        {
            foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value);
            _views.Clear();
            if (_flying != null) { Destroy(_flying); _flying = null; }
            if (_shooterBubble != null) { Destroy(_shooterBubble); _shooterBubble = null; }
            if (_overlay != null) { Destroy(_overlay); _overlay = null; }

            _grid = new BubbleGrid(Rows, Cols);
            Score = 0; AddScore(0);
            GameOver = false; Cleared = false;
            FillInitialBoard();
            _currentColor = PickColor();
            _nextColor = PickColor();
            SpawnShooterBubble();
            RefreshNext();
        }

        private static void GoToMenu() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // ---- HUD ----

        private bool PointerOverHud() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void BuildHud()
        {
            var canvasGo = new GameObject("BubbleHud");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _hud = (RectTransform)canvasGo.transform;

            var scoreLabel = UIFactory.CreateText(_hud, "ScoreLabel", "점수", 28, UITheme.TextDim,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)scoreLabel.transform, 0.5f, 1f, new Vector2(0f, -38f), new Vector2(200f, 40f));
            _scoreText = UIFactory.CreateText(_hud, "Score", "0", 54, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)_scoreText.transform, 0.5f, 1f, new Vector2(0f, -92f), new Vector2(360f, 68f));

            Image nextPanel = UIFactory.CreateImage(_hud, "NextPanel", new Color(0f, 0f, 0f, 0.28f));
            nextPanel.sprite = SpriteFactory.RoundedRect; nextPanel.type = Image.Type.Sliced;
            Place((RectTransform)nextPanel.transform, 1f, 1f, new Vector2(-110f, -108f), new Vector2(150f, 176f));
            var nextLabel = UIFactory.CreateText((RectTransform)nextPanel.transform, "NextLabel", "다음", 24,
                UITheme.TextMain, TextAnchor.UpperCenter, FontStyle.Bold);
            Place((RectTransform)nextLabel.transform, 0.5f, 1f, new Vector2(0f, -8f), new Vector2(130f, 34f));
            _nextIcon = UIFactory.CreateImage((RectTransform)nextPanel.transform, "NextIcon", Color.white);
            _nextIcon.preserveAspect = true;
            Place((RectTransform)_nextIcon.transform, 0.5f, 0.4f, Vector2.zero, new Vector2(90f, 90f));

            Button menu = UIFactory.CreateButton(_hud, "MenuButton", "메뉴", 26, UITheme.ButtonSecondary, out _);
            Place((RectTransform)menu.transform, 0f, 1f, new Vector2(90f, -44f), new Vector2(140f, 56f));
            menu.onClick.AddListener(GoToMenu);
        }

        private void RefreshNext()
        {
            if (_nextIcon != null) _nextIcon.sprite = BubbleSprites.Bubble(_nextColor);
        }

        private void ShowOverlay(string title, bool cleared)
        {
            Image dim = UIFactory.CreateImage(_hud, "Result", new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Stretch((RectTransform)dim.transform);
            _overlay = dim.gameObject;

            Image panel = UIFactory.CreateImage((RectTransform)dim.transform, "Panel", UITheme.HudBg);
            panel.sprite = SpriteFactory.RoundedRect; panel.type = Image.Type.Sliced;
            var pr = (RectTransform)panel.transform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(560f, 380f);

            var t = UIFactory.CreateText(pr, "Title", title, 46, cleared ? UITheme.TextGold : UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)t.transform, 0.5f, 1f, new Vector2(0f, -72f), new Vector2(520f, 70f));
            var st = UIFactory.CreateText(pr, "FinalScore", $"점수 {Score:N0}", 34, UITheme.TextGold,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)st.transform, 0.5f, 1f, new Vector2(0f, -152f), new Vector2(520f, 50f));

            Button retry = UIFactory.CreateButton(pr, "Retry", "다시하기", 28, UITheme.ButtonPrimary, out _);
            var rr = (RectTransform)retry.transform;
            rr.anchorMin = new Vector2(0f, 0f); rr.anchorMax = new Vector2(0.5f, 0f);
            rr.offsetMin = new Vector2(28f, 28f); rr.offsetMax = new Vector2(-12f, 92f);
            retry.onClick.AddListener(Restart);

            Button menu = UIFactory.CreateButton(pr, "Menu", "메뉴", 28, UITheme.ButtonSecondary, out _);
            var mr = (RectTransform)menu.transform;
            mr.anchorMin = new Vector2(0.5f, 0f); mr.anchorMax = new Vector2(1f, 0f);
            mr.offsetMin = new Vector2(12f, 28f); mr.offsetMax = new Vector2(-28f, 92f);
            menu.onClick.AddListener(GoToMenu);
        }

        private static void Place(RectTransform rect, float ax, float ay, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(ax, ay);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
    }
}

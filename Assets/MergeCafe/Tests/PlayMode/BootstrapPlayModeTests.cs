using System.Collections;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Orders;
using MergeCafe.Save;
using MergeCafe.Suika;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MergeCafe.Tests
{
    /// <summary>
    /// End-to-end smoke test: boots the whole runtime-built game (as the real scene
    /// does), exercises the core loop and checks persistence across a "reload".
    /// </summary>
    public sealed class BootstrapPlayModeTests
    {
        private GameObject _bootstrapGo;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(SaveManager.PrefsKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(SaveManager.PrefsKey);

            if (_bootstrapGo != null)
                Object.Destroy(_bootstrapGo);

            foreach (string canvasName in new[] { "Canvas", "TitleCanvas" })
            {
                var canvas = GameObject.Find(canvasName);
                if (canvas != null)
                    Object.Destroy(canvas);
            }

            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
                Object.Destroy(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsUi_SpawnsAndMerges_AndPersists()
        {
            _bootstrapGo = new GameObject("TestBootstrap");
            var bootstrap = _bootstrapGo.AddComponent<GameBootstrap>();
            yield return null; // let Awake run — title screen only

            // Title screen is up; the game is not built until the player starts.
            Assert.IsNotNull(GameObject.Find("TitleCanvas"), "title screen");
            Assert.IsNull(bootstrap.Game, "game not started yet");

            bootstrap.StartGame(); // simulate the title-screen click
            yield return null;

            // --- UI built ---
            Assert.IsNotNull(GameObject.Find("Canvas"), "canvas");
            Assert.IsNotNull(Object.FindObjectOfType<EventSystem>(), "event system");
            Assert.IsNotNull(GameObject.Find("Cell_00"), "first cell");
            Assert.IsNotNull(GameObject.Find("Cell_62"), "last cell");
            Assert.IsNotNull(GameObject.Find("OrderCard_4"), "fifth order card");
            Assert.IsNotNull(GameObject.Find("TotalGauge"), "shared energy gauge");

            GameManager game = bootstrap.Game;
            Assert.IsNotNull(game);
            Assert.AreEqual(0, game.Economy.Gold);
            Assert.AreEqual(35, game.Board.UnlockedCount);
            Assert.AreEqual(5, game.Orders.Orders.Count);

            // Generators are placed on the board.
            int coffeeGenCell = GeneratorCatalog.CoffeeMachine.InitialCell;
            Assert.IsTrue(game.Board.HasGenerator(coffeeGenCell));
            Assert.AreEqual(20, game.Generators.Energy.Current);

            // --- Tap the coffee generator twice (items spawn near it) and merge them ---
            double now = TimeUtil.NowUnixSeconds();
            Assert.IsTrue(game.RequestSpawn(ItemType.Coffee, coffeeGenCell, now));
            Assert.IsTrue(game.RequestSpawn(ItemType.Coffee, coffeeGenCell, now));
            Assert.AreEqual(18, game.Generators.Energy.Current);
            yield return null;

            // Spawn cells are random near the generator — find the two Lv.1 coffees.
            var coffeeCells = new List<int>();
            for (int i = 0; i < BoardManager.CellCount; i++)
            {
                var it = game.Board.GetItem(i);
                if (it != null && it.Type == ItemType.Coffee && it.Level == 1)
                    coffeeCells.Add(i);
            }
            Assert.AreEqual(2, coffeeCells.Count, "two coffees spawned");
            int first = coffeeCells[0];
            int second = coffeeCells[1];

            var outcome = game.RequestMoveItem(first, second);
            Assert.AreEqual(MergeCafe.Items.MoveOutcome.Merged, outcome);
            Assert.AreEqual(2, game.Board.GetItem(second).Level);
            yield return null;

            // Token view mirrors the merged item.
            GameObject cellGo = GameObject.Find($"Cell_{second:D2}");
            Assert.IsNotNull(cellGo.transform.Find("Token"), "merged token view");

            // --- Autosave happened; a fresh GameManager restores the same state ---
            Assert.IsTrue(SaveManager.HasSave(), "autosave written");
            var restored = new GameManager(now);
            Assert.IsTrue(SaveManager.TryLoadInto(restored, now));
            Assert.AreEqual(2, restored.Board.GetItem(second).Level);
            Assert.AreEqual(18, restored.Generators.Energy.Current);
        }

        [UnityTest]
        public IEnumerator Stress_ExercisesEveryUiPath_WithoutRecursion()
        {
            _bootstrapGo = new GameObject("TestBootstrap");
            var bootstrap = _bootstrapGo.AddComponent<GameBootstrap>();
            yield return null;
            bootstrap.StartGame();
            yield return null;

            GameManager game = bootstrap.Game;

            // Open the merge guide (builds all 15 item icons + generator icons).
            var popupLayer = (RectTransform)GameObject.Find("PopupLayer").transform;
            MergeCafe.UI.MergeGuideView.Show(popupLayer);
            yield return null;

            // Economy actions.
            game.Economy.AddGold(50000);
            Assert.IsTrue(game.RequestUpgradeEnergy());
            Assert.IsTrue(game.RequestExpandBoard());
            yield return null;

            // Many spawns near the generator.
            double now = TimeUtil.NowUnixSeconds();
            int genCell = GeneratorCatalog.CoffeeMachine.InitialCell;
            for (int i = 0; i < 8; i++)
                game.RequestSpawn(ItemType.Coffee, genCell, now);
            yield return null;

            // Move a generator.
            game.Board.TryFindEmptyCell(out int genTarget);
            game.RequestMoveGenerator(genCell, genTarget);
            yield return null;

            // Complete an order.
            CafeOrder order = game.Orders.Orders[0];
            Assert.IsTrue(game.Board.TryFindEmptyCell(out int freeCell));
            game.Board.TryPlaceItem(freeCell,
                new ItemInstance(order.requiredItemType, order.requiredItemLevel));
            Assert.IsTrue(game.RequestCompleteOrder(order.orderId));
            yield return null;

            // Run several frames (Update/Tick + gauge countdown).
            for (int f = 0; f < 8; f++)
                yield return null;

            Assert.IsNotNull(game.Board); // reached here → no stack overflow along these paths
        }

        [UnityTest]
        public IEnumerator Suika_TwoEqualFruitsMergeIntoNextSize()
        {
            var game = MergeCafe.Suika.SuikaGame.Create();
            yield return null;

            Assert.AreEqual(0, game.Score);
            var a = game.SpawnFruit(1, new Vector2(-0.5f, 0f), true);
            var b = game.SpawnFruit(1, new Vector2(0.5f, 0f), true);
            Assert.AreEqual(2, game.FruitCount);

            game.RequestMerge(a, b);
            yield return null;

            Assert.AreEqual(1, game.FruitCount, "two fruits became one");
            Assert.AreEqual(SuikaCatalog.MergeScore(2), game.Score, "score awarded");

            var fruits = Object.FindObjectsOfType<MergeCafe.Suika.SuikaFruit>();
            bool hasLevel2 = false;
            foreach (var f in fruits)
                if (f.Level == 2) hasLevel2 = true;
            Assert.IsTrue(hasLevel2, "a level-2 fruit was created");

            foreach (string n in new[] { "SuikaGame", "SuikaRoot", "SuikaHud" })
            {
                var go = GameObject.Find(n);
                if (go != null) Object.Destroy(go);
            }
            var es = Object.FindObjectOfType<EventSystem>();
            if (es != null) Object.Destroy(es.gameObject);
        }

        [UnityTest]
        public IEnumerator Tmp_RendersKoreanGlyphs()
        {
            var canvasGo = new GameObject("TmpCanvas", typeof(Canvas));
            var tmp = new GameObject("T").AddComponent<TMPro.TextMeshProUGUI>();
            tmp.transform.SetParent(canvasGo.transform, false);
            tmp.font = MergeCafe.UI.UITheme.TmpFont;
            Assert.IsNotNull(tmp.font, "TMP font asset created");
            tmp.fontSize = 40;
            tmp.text = "머지 카페 커피머신 12345";
            tmp.ForceMeshUpdate();
            yield return null;

            Assert.Greater(tmp.textInfo.characterCount, 0, "characters laid out");
            // Every visible character resolved to a glyph in the (dynamic) atlas.
            int visible = 0;
            for (int i = 0; i < tmp.textInfo.characterCount; i++)
            {
                var ci = tmp.textInfo.characterInfo[i];
                if (ci.isVisible)
                {
                    visible++;
                    Assert.IsNotNull(ci.fontAsset, $"glyph for '{ci.character}' resolved");
                }
            }
            Assert.Greater(visible, 0, "some visible glyphs");

            Object.Destroy(canvasGo);
            Object.Destroy(tmp.gameObject);
        }

        [UnityTest]
        public IEnumerator TitleScreen_Click_StartsGameAndRemovesTitle()
        {
            _bootstrapGo = new GameObject("TestBootstrap");
            var bootstrap = _bootstrapGo.AddComponent<GameBootstrap>();
            yield return null;

            var title = Object.FindObjectOfType<MergeCafe.UI.TitleScreenView>();
            Assert.IsNotNull(title, "title screen view present");
            Assert.IsNull(bootstrap.Game, "game not started before selection");

            // Select the merge-game card.
            title.SelectMerge();
            yield return null;

            Assert.IsNotNull(bootstrap.Game, "game started after selection");
            Assert.IsNull(GameObject.Find("TitleCanvas"), "title screen removed after selection");
            Assert.IsNotNull(GameObject.Find("Canvas"), "game canvas built after selection");
        }
    }
}

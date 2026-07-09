using System.Collections;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Save;
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
        public IEnumerator TitleScreen_Click_StartsGameAndRemovesTitle()
        {
            _bootstrapGo = new GameObject("TestBootstrap");
            var bootstrap = _bootstrapGo.AddComponent<GameBootstrap>();
            yield return null;

            var title = Object.FindObjectOfType<MergeCafe.UI.TitleScreenView>();
            Assert.IsNotNull(title, "title screen view present");
            Assert.IsNull(bootstrap.Game, "game not started before click");

            // Simulate the click on the title screen.
            title.OnPointerClick(new PointerEventData(EventSystem.current));
            yield return null;

            Assert.IsNotNull(bootstrap.Game, "game started after click");
            Assert.IsNull(GameObject.Find("TitleCanvas"), "title screen removed after click");
            Assert.IsNotNull(GameObject.Find("Canvas"), "game canvas built after click");
        }
    }
}

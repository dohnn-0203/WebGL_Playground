using System.Collections;
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

            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
                Object.Destroy(canvas);

            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
                Object.Destroy(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsUi_SpawnsAndMerges_AndPersists()
        {
            _bootstrapGo = new GameObject("TestBootstrap");
            var bootstrap = _bootstrapGo.AddComponent<GameBootstrap>();
            yield return null; // let Awake run

            // --- UI built ---
            Assert.IsNotNull(GameObject.Find("Canvas"), "canvas");
            Assert.IsNotNull(Object.FindObjectOfType<EventSystem>(), "event system");
            Assert.IsNotNull(GameObject.Find("Cell_00"), "first cell");
            Assert.IsNotNull(GameObject.Find("Cell_35"), "last cell");
            Assert.IsNotNull(GameObject.Find("Generator_Coffee"), "coffee generator button");
            Assert.IsNotNull(GameObject.Find("OrderCard_2"), "third order card");

            GameManager game = bootstrap.Game;
            Assert.IsNotNull(game);
            Assert.AreEqual(0, game.Economy.Gold);
            Assert.AreEqual(16, game.Board.UnlockedCount);
            Assert.AreEqual(3, game.Orders.Orders.Count);

            // --- Spawn two coffees and merge them ---
            double now = TimeUtil.NowUnixSeconds();
            Assert.IsTrue(game.RequestSpawn(ItemType.Coffee, now));
            Assert.IsTrue(game.RequestSpawn(ItemType.Coffee, now));
            yield return null;

            int first = BoardManager.IndexOf(1, 1);
            int second = BoardManager.IndexOf(1, 2);
            Assert.IsNotNull(game.Board.GetItem(first));
            Assert.IsNotNull(game.Board.GetItem(second));

            var outcome = game.RequestMove(first, second);
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
            Assert.AreEqual(8, restored.Generators.Get(ItemType.Coffee).Energy);
        }
    }
}

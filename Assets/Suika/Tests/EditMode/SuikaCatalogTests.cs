using MergeCafe.Suika;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class SuikaCatalogTests
    {
        [Test]
        public void Has11Fruits_EndingInWatermelon()
        {
            Assert.AreEqual(11, SuikaCatalog.Count);
            Assert.AreEqual("체리", SuikaCatalog.Name(1));
            Assert.AreEqual("수박", SuikaCatalog.Name(11));
        }

        [Test]
        public void Radii_StrictlyIncrease()
        {
            for (int level = 2; level <= SuikaCatalog.Count; level++)
                Assert.Greater(SuikaCatalog.Radius(level), SuikaCatalog.Radius(level - 1),
                    $"level {level} radius");
        }

        [Test]
        public void MergeScore_IsTriangular()
        {
            Assert.AreEqual(3, SuikaCatalog.MergeScore(2));   // 1+2
            Assert.AreEqual(6, SuikaCatalog.MergeScore(3));   // 1+2+3
            Assert.AreEqual(66, SuikaCatalog.MergeScore(11)); // watermelon
        }

        [Test]
        public void IsMax_OnlyForWatermelon()
        {
            Assert.IsFalse(SuikaCatalog.IsMax(10));
            Assert.IsTrue(SuikaCatalog.IsMax(11));
        }

        [Test]
        public void Clamp_HandlesOutOfRange()
        {
            Assert.AreEqual(SuikaCatalog.Name(1), SuikaCatalog.Name(0));
            Assert.AreEqual(SuikaCatalog.Name(11), SuikaCatalog.Name(99));
            Assert.AreEqual(SuikaCatalog.Radius(1), SuikaCatalog.Radius(-5));
        }

        [Test]
        public void MaxDropLevel_IsWithinRange()
        {
            Assert.GreaterOrEqual(SuikaCatalog.MaxDropLevel, 1);
            Assert.Less(SuikaCatalog.MaxDropLevel, SuikaCatalog.Count);
        }
    }
}

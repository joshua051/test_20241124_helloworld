using IronSand.Arena;
using NUnit.Framework;
using UnityEngine;

namespace IronSand.Tests
{
    public sealed class CrowdFavorSystemTests
    {
        private GameObject root;
        private CrowdFavorSystem system;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CrowdFavorSystemTests");
            system = root.AddComponent<CrowdFavorSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void AddFavor_IgnoresNonPositiveValues()
        {
            system.AddFavor(0);
            system.AddFavor(-10);
            Assert.That(system.Favor, Is.EqualTo(0));
        }

        [Test]
        public void AddFavor_ClampsToMaximum()
        {
            system.AddFavor(500);
            Assert.That(system.Favor, Is.EqualTo(system.MaxFavor));
        }

        [Test]
        public void ResetFavor_ReturnsToZero()
        {
            system.AddFavor(40);
            system.ResetFavor();
            Assert.That(system.Favor, Is.Zero);
        }
    }
}

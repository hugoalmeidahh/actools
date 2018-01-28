﻿using FirstFloor.ModernUI.Serialization;
using NUnit.Framework;

namespace FirstFloor.ModernUI.Tests {
    [TestFixture]
    public class SimpleSerializationTest {
        [Test]
        public void BoolTest() {
            Assert.AreEqual(true, "1".As<bool>());
            Assert.AreEqual(true, "1".As<bool?>());
            Assert.AreEqual(true, ((bool?)true).As<bool?>());
            Assert.AreEqual(true, "true".As<bool?>());
            Assert.AreEqual(false, "false".As<bool?>());
            Assert.AreEqual(false, "0".As<bool?>());
            Assert.AreEqual(null, ((object)null).As<bool?>());
        }
    }
}
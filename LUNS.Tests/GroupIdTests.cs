using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using LUNS.Library;

namespace LUNS.Tests
{
    [TestClass]
    public class GroupIdTests
    {
        [TestMethod]
        public void EqualsTest()
        {
            var g1a = new GroupId(1);
            var g1b = new GroupId(1);
            var g2 = new GroupId(2);

            Assert.AreEqual(g1a, g1b);
            Assert.AreEqual(g1b, g1a);

            Assert.IsTrue(g1a.Equals(g1b));
            Assert.IsTrue(g1b.Equals(g1a));

            Assert.AreNotEqual(g1a, g2);
            Assert.AreNotEqual(g2, g1a);

            Assert.IsFalse(g1a.Equals(g2));
            Assert.IsFalse(g2.Equals(g1a));
        }

        [TestMethod]
        public void EqualityTest()
        {
            var g1a = new GroupId(1);
            var g1b = new GroupId(1);
            var g2 = new GroupId(2);

            Assert.IsTrue(g1a == g1b);
            Assert.IsTrue(g1b == g1a);

            Assert.IsFalse(g1a == g2);
            Assert.IsFalse(g2 == g1a);
        }

        [TestMethod]
        public void InequalityTest()
        {
            var g1a = new GroupId(1);
            var g1b = new GroupId(1);
            var g2 = new GroupId(2);

            Assert.IsFalse(g1a != g1b);
            Assert.IsFalse(g1b != g1a);

            Assert.IsTrue(g1a != g2);
            Assert.IsTrue(g2 != g1a);
        }

        [TestMethod]
        public void ReservedValuesTest()
        {
            Assert.ThrowsException<ArgumentException>(() => new GroupId(GroupId.None.Index));
            Assert.ThrowsException<ArgumentException>(() => new GroupId(GroupId.Reserved.Index));
        }
    }
}

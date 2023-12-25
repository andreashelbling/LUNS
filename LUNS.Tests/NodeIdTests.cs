using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using LUNS.Library;

namespace LUNS.Tests
{
    [TestClass]
    public class NodeIdTests
    {
        [TestMethod]
        public void EqualsTest()
        {
            var n1a = new NodeId(1);
            var n1b = new NodeId(1);
            var n2 = new NodeId(2);

            Assert.AreEqual(n1a, n1b);
            Assert.AreEqual(n1b, n1a);

            Assert.IsTrue(n1a.Equals(n1b));
            Assert.IsTrue(n1b.Equals(n1a));

            Assert.AreNotEqual(n1a, n2);
            Assert.AreNotEqual(n2, n1a);

            Assert.IsFalse(n1a.Equals(n2));
            Assert.IsFalse(n2.Equals(n1a));
        }

        [TestMethod]
        public void EqualityTest()
        {
            var n1a = new NodeId(1);
            var n1b = new NodeId(1);
            var n2 = new NodeId(2);

            Assert.IsTrue(n1a == n1b);
            Assert.IsTrue(n1b == n1a);

            Assert.IsFalse(n1a == n2);
            Assert.IsFalse(n2 == n1a);
        }

        [TestMethod]
        public void InequalityTest()
        {
            var n1a = new NodeId(1);
            var n1b = new NodeId(1);
            var n2 = new NodeId(2);

            Assert.IsFalse(n1a != n1b);
            Assert.IsFalse(n1b != n1a);

            Assert.IsTrue(n1a != n2);
            Assert.IsTrue(n2 != n1a);
        }

        [TestMethod]
        public void ReservedValuesTest()
        {
            Assert.ThrowsException<ArgumentException>(() => new NodeId(NodeId.Undefined.Index));
            Assert.ThrowsException<ArgumentException>(() => new NodeId(NodeId.Router.Index));
        }
    }
}

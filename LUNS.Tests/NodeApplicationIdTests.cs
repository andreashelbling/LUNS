using Microsoft.VisualStudio.TestTools.UnitTesting;
using LUNS.Library;

namespace LUNS.Tests
{
    [TestClass]
    public class NodeApplicationIdTests
    {
        [TestMethod]
        public void EqualsTest()
        {
            var n11a = new NodeApplicationId(new NodeId(1), 1);
            var n11b = new NodeApplicationId(new NodeId(1), 1);
            var n12 = new NodeApplicationId(new NodeId(1), 2);
            var n21 = new NodeApplicationId(new NodeId(2), 1);

            Assert.AreEqual(n11a, n11b);
            Assert.AreEqual(n11b, n11a);

            Assert.IsTrue(n11a.Equals(n11b));
            Assert.IsTrue(n11b.Equals(n11a));

            Assert.AreNotEqual(n11a, n12);
            Assert.AreNotEqual(n11a, n21);
            Assert.AreNotEqual(n12, n11a);
            Assert.AreNotEqual(n21, n11a);

            Assert.IsFalse(n11a.Equals(n12));
            Assert.IsFalse(n11a.Equals(n21));
            Assert.IsFalse(n12.Equals(n11a));
            Assert.IsFalse(n21.Equals(n11a));
        }

        [TestMethod]
        public void EqualityTest()
        {
            var n11a = new NodeApplicationId(new NodeId(1), 1);
            var n11b = new NodeApplicationId(new NodeId(1), 1);
            var n12 = new NodeApplicationId(new NodeId(1), 2);
            var n21 = new NodeApplicationId(new NodeId(2), 1);

            Assert.IsTrue(n11a == n11b);
            Assert.IsTrue(n11b == n11a);

            Assert.IsFalse(n11a == n12);
            Assert.IsFalse(n11a == n21);
            Assert.IsFalse(n12 == n11a);
            Assert.IsFalse(n21 == n11a);
        }

        [TestMethod]
        public void InequalityTest()
        {
            var n11a = new NodeApplicationId(new NodeId(1), 1);
            var n11b = new NodeApplicationId(new NodeId(1), 1);
            var n12 = new NodeApplicationId(new NodeId(1), 2);
            var n21 = new NodeApplicationId(new NodeId(2), 1);

            Assert.IsFalse(n11a != n11b);
            Assert.IsFalse(n11b != n11a);

            Assert.IsTrue(n11a != n12);
            Assert.IsTrue(n11a != n21);
            Assert.IsTrue(n12 != n11a);
            Assert.IsTrue(n21 != n11a);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var n12 = new NodeApplicationId(new NodeId(1), 2);
            Assert.AreEqual("0x0102", n12.ToString());
        }

        [TestMethod]
        public void NodeIdTest()
        {
            var n12 = new NodeApplicationId(new NodeId(1), 2);
            NodeId n1 = n12.NodeId;
            Assert.AreEqual(new NodeId(1), n1);
        }
    }
}

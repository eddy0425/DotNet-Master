using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class Rect2dTests
    {
        [TestMethod]
        public void Constructor_RejectsNegativeSize()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Rect2d(0.0, 0.0, -1.0, 1.0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Rect2d(0.0, 0.0, 1.0, -1.0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Rect2d(Point2d.Zero, new Size2d(-1, 1)));
            Geom.AreClose(0, 0, 0, 0, new Rect2d());
        }

        [TestMethod]
        public void FromLTRB()
        {
            Geom.AreClose(1, 2, 3, 4, Rect2d.FromLTRB(1, 2, 4, 6));
            Assert.ThrowsException<ArgumentException>(() => Rect2d.FromLTRB(5, 0, 4, 1));
            Assert.ThrowsException<ArgumentException>(() => Rect2d.FromLTRB(0, 5, 1, 4));
        }

        [TestMethod]
        public void Edges()
        {
            var r = new Rect2d(1.0, 2.0, 3.0, 4.0);
            Assert.AreEqual(1.0, r.Left);
            Assert.AreEqual(4.0, r.Right);
            Assert.AreEqual(2.0, r.Top);
            Assert.AreEqual(6.0, r.Bottom);
            Assert.AreEqual(2.5, r.CenterX);
            Assert.AreEqual(4.0, r.CenterY);
            Geom.AreClose(1, 2, r.TopLeft);
            Geom.AreClose(4, 6, r.BottomRight);
            Geom.AreClose(1, 2, r.Location);
            Assert.AreEqual(3.0, r.Size.Width);
            Assert.AreEqual(4.0, r.Size.Height);
        }

        [TestMethod]
        public void ContainsPoint_IsHalfOpen()
        {
            var r = new Rect2d(0.0, 0.0, 10.0, 10.0);
            Assert.IsTrue(r.Contains(0, 0));
            Assert.IsTrue(r.Contains(9.999, 9.999));
            Assert.IsFalse(r.Contains(10, 5), "右边界不算");
            Assert.IsFalse(r.Contains(5, 10), "下边界不算");
            Assert.IsFalse(r.Contains(new Point2d(-0.001, 5)));
        }

        [TestMethod]
        public void ContainsRect_IsClosed()
        {
            var r = new Rect2d(0.0, 0.0, 10.0, 10.0);
            Assert.IsTrue(r.Contains(r));
            Assert.IsTrue(r.Contains(new Rect2d(2.0, 2.0, 8.0, 8.0)));
            Assert.IsFalse(r.Contains(new Rect2d(2.0, 2.0, 9.0, 1.0)));
        }

        [TestMethod]
        public void Inflate_ReturnsNewInstance()
        {
            var r = new Rect2d(5.0, 5.0, 10.0, 10.0);
            var bigger = r.Inflate(1, 2);
            Geom.AreClose(4, 3, 12, 14, bigger);
            Geom.AreClose(5, 5, 10, 10, r);
            Geom.AreClose(4, 3, 12, 14, r.Inflate(new Size2d(1, 2)));
            Geom.AreClose(4, 3, 12, 14, Rect2d.Inflate(r, 1, 2));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => r.Inflate(-6, 0));
        }

        [TestMethod]
        public void Intersect()
        {
            var a = new Rect2d(0.0, 0.0, 10.0, 10.0);
            Geom.AreClose(5, 5, 5, 5, a.Intersect(new Rect2d(5.0, 5.0, 10.0, 10.0)));
            Geom.AreClose(5, 5, 5, 5, a & new Rect2d(5.0, 5.0, 10.0, 10.0));
            // 边相接：零宽交集
            Geom.AreClose(10, 0, 0, 10, a.Intersect(new Rect2d(10.0, 0.0, 5.0, 10.0)));
        }

        [TestMethod]
        public void Intersect_Disjoint_ReturnsFreshEmptyRect()
        {
            var a = new Rect2d(0.0, 0.0, 1.0, 1.0);
            var b = new Rect2d(5.0, 5.0, 1.0, 1.0);
            var e1 = a.Intersect(b);
            var e2 = Rect2d.Intersect(a, b);
            Geom.AreClose(0, 0, 0, 0, e1);
            Assert.IsFalse(ReferenceEquals(e1, e2), "不能返回共享单例");
        }

        [TestMethod]
        public void IntersectsWith_IsStrict()
        {
            var a = new Rect2d(0.0, 0.0, 10.0, 10.0);
            Assert.IsTrue(a.IntersectsWith(new Rect2d(9.0, 9.0, 5.0, 5.0)));
            Assert.IsFalse(a.IntersectsWith(new Rect2d(10.0, 0.0, 5.0, 5.0)), "仅边相接不算相交");
            Assert.IsFalse(a.IntersectsWith(new Rect2d(20.0, 20.0, 1.0, 1.0)));
        }

        [TestMethod]
        public void Union()
        {
            var a = new Rect2d(0.0, 0.0, 2.0, 2.0);
            var b = new Rect2d(5.0, 1.0, 1.0, 4.0);
            Geom.AreClose(0, 0, 6, 5, a.Union(b));
            Geom.AreClose(0, 0, 6, 5, a | b);
            Geom.AreClose(0, 0, 6, 5, Rect2d.Union(b, a));
        }

        [TestMethod]
        public void OffsetOperators()
        {
            var r = new Rect2d(1.0, 1.0, 2.0, 2.0);
            Geom.AreClose(4, 5, 2, 2, r + new Point2d(3, 4));
            Geom.AreClose(-2, -3, 2, 2, r - new Point2d(3, 4));
            Geom.AreClose(1, 1, 5, 6, r + new Size2d(3, 4));
            Geom.AreClose(1, 1, 1, 1, r - new Size2d(1, 1));
        }

        [TestMethod]
        public void Equality()
        {
            var a = new Rect2d(1.0, 2.0, 3.0, 4.0);
            var b = new Rect2d(1.004, 2.0, 3.0, 4.004);
            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals((object)b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a != new Rect2d(1.0, 2.0, 3.0, 5.0));

            Rect2d? n1 = null, n2 = null;
            Assert.IsTrue(n1 == n2);
            Assert.IsFalse(a == n1);
            Assert.IsFalse(n1 == a);
            Assert.IsFalse(a.Equals(null));
        }

        [TestMethod]
        public void ToRect_Truncates()
        {
            var r = new Rect2d(1.9, 2.9, 3.9, 4.9).ToRect();
            Assert.AreEqual(1, r.X);
            Assert.AreEqual(2, r.Y);
            Assert.AreEqual(3, r.Width);
            Assert.AreEqual(4, r.Height);
        }

        [TestMethod]
        public void Json_RoundTrips()
        {
            var r = new Rect2d(1.5, 2.0, 3.0, 4.25);
            var back = JsonConvert.DeserializeObject<Rect2d>(JsonConvert.SerializeObject(r))!;
            Geom.AreClose(1.5, 2, 3, 4.25, back);
        }

        [TestMethod]
        public void Json_NegativeSize_IsRejected()
        {
            // [JsonConstructor] 走构造函数校验，落盘数据也不能造出非法矩形
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => JsonConvert.DeserializeObject<Rect2d>("{\"X\":0,\"Y\":0,\"Width\":-1,\"Height\":1}"));
        }
    }
}

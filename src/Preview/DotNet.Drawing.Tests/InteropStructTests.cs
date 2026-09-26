using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    /// <summary>
    /// OpenCV 互操作结构体：<see cref="Point2f"/> / <see cref="Size2f"/> / <see cref="Rect"/> / <see cref="Rect2f"/>。
    /// </summary>
    /// <remarks>这些类型按分量精确判等，且内存布局须与原生结构一致。</remarks>
    [TestClass]
    public class InteropStructTests
    {
        [TestMethod]
        public void SizeOf_MatchesMarshalledLayout()
        {
            Assert.AreEqual(Marshal.SizeOf(typeof(Point2f)), Point2f.SizeOf);
            Assert.AreEqual(Marshal.SizeOf(typeof(Rect)), Rect.SizeOf);
            Assert.AreEqual(Marshal.SizeOf(typeof(Rect2f)), Rect2f.SizeOf);
        }

        #region Point2f

        [TestMethod]
        public void Point2f_Operators()
        {
            var a = new Point2f(1, 2);
            var b = new Point2f(3, 5);
            Assert.AreEqual(new Point2f(4, 7), a + b);
            Assert.AreEqual(new Point2f(-2, -3), a - b);
            Assert.AreEqual(new Point2f(-1, -2), -a);
            Assert.AreEqual(new Point2f(2.5f, 5), a * 2.5);
        }

        [TestMethod]
        public void Point2f_ConversionsWithIntegerPoint()
        {
            Assert.AreEqual(new Point(1, -1), (Point)new Point2f(1.9f, -1.9f), "向零截断");
            Assert.AreEqual(new Point2f(3, 4), (Point2f)new Point(3, 4));
        }

        [TestMethod]
        public void Point2f_Equality_IsExact()
        {
            var a = new Point2f(1, 2);
            Assert.IsTrue(a == new Point2f(1, 2));
            Assert.AreEqual(a.GetHashCode(), new Point2f(1, 2).GetHashCode());
            Assert.IsTrue(a != new Point2f(1.0001f, 2));
            Assert.IsFalse(a.Equals((object)new Point2d(1, 2)));
        }

        #endregion

        #region Size2f

        [TestMethod]
        public void Size2f_ConstructorsAndEquality()
        {
            var s = new Size2f(1.5, 2.25);
            Assert.AreEqual(1.5f, s.Width);
            Assert.AreEqual(2.25f, s.Height);
            Assert.IsTrue(s == new Size2f(1.5f, 2.25f));
            Assert.AreEqual(s.GetHashCode(), new Size2f(1.5f, 2.25f).GetHashCode());
            Assert.IsTrue(s != new Size2f(1.5f, 2.5f));
            Assert.IsFalse(s.Equals("1.5x2.25"));
        }

        #endregion

        #region Rect

        [TestMethod]
        public void Rect_ConstructorsAndOperators()
        {
            var r = new Rect(new Point(1, 2), new Size(3, 4));
            Assert.AreEqual(new Rect(1, 2, 3, 4), r);
            Assert.AreEqual(new Rect(11, 22, 3, 4), r + new Point(10, 20));
            Assert.AreEqual(new Rect(-9, -18, 3, 4), r - new Point(10, 20));
            Assert.AreEqual(new Rect(1, 2, 13, 24), r + new Size(10, 20));
            Assert.AreEqual(new Rect(1, 2, 2, 3), r - new Size(1, 1));
        }

        [TestMethod]
        public void Rect_Equality()
        {
            var r = new Rect(1, 2, 3, 4);
            Assert.IsTrue(r == new Rect(1, 2, 3, 4));
            Assert.AreEqual(r.GetHashCode(), new Rect(1, 2, 3, 4).GetHashCode());
            Assert.IsTrue(r != new Rect(1, 2, 3, 5));
            Assert.IsFalse(r.Equals((object)new Rect2f(1, 2, 3, 4)));
        }

        #endregion

        #region Rect2f

        [TestMethod]
        public void Rect2f_ConstructorsAndOperators()
        {
            var r = new Rect2f(new Point2f(1, 2), new Size2f(3f, 4f));
            Assert.AreEqual(new Rect2f(1, 2, 3, 4), r);
            Assert.AreEqual(new Rect2f(1.5f, 2.5f, 3, 4), r + new Point2f(0.5f, 0.5f));
            Assert.AreEqual(new Rect2f(0.5f, 1.5f, 3, 4), r - new Point2f(0.5f, 0.5f));
            Assert.AreEqual(new Rect2f(1, 2, 3.5f, 4.5f), r + new Size2f(0.5f, 0.5f));
            Assert.AreEqual(new Rect2f(1, 2, 2.5f, 3.5f), r - new Size2f(0.5f, 0.5f));
        }

        [TestMethod]
        public void Rect2f_Equality()
        {
            var r = new Rect2f(1, 2, 3, 4);
            Assert.IsTrue(r == new Rect2f(1, 2, 3, 4));
            Assert.AreEqual(r.GetHashCode(), new Rect2f(1, 2, 3, 4).GetHashCode());
            Assert.IsTrue(r != new Rect2f(1, 2, 3, 4.5f));
            Assert.IsFalse(r.Equals((object)new Rect(1, 2, 3, 4)));
        }

        #endregion
    }
}

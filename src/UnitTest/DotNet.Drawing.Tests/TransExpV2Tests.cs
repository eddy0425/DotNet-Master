using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class TransExpV2Tests
    {
        public class Source
        {
            public int Number { get; set; }
            public string Text { get; set; }
            public List<int> Items { get; set; }
            public int Field;
        }

        public class Target
        {
            public int Number { get; set; }
            public string Text { get; set; }
            public List<int> Items { get; set; }
            public int ReadOnly => 42;
            public int Field;
        }

        [TestMethod]
        public void Trans_CopiesWritableProperties()
        {
            var src = new Source { Number = 7, Text = "t", Items = new List<int> { 1 } };
            var dst = TransExpV2<Source, Target>.Trans(src);
            Assert.AreEqual(7, dst.Number);
            Assert.AreEqual("t", dst.Text);
            Assert.AreEqual(42, dst.ReadOnly);
        }

        [TestMethod]
        public void Trans_IsShallow()
        {
            var src = new Source { Items = new List<int> { 1 } };
            var dst = TransExpV2<Source, Target>.Trans(src);
            Assert.AreSame(src.Items, dst.Items);
        }

        [TestMethod]
        public void Trans_SkipsFields()
        {
            // 文档化的限制：字段不会被复制
            var dst = TransExpV2<Source, Target>.Trans(new Source { Field = 5 });
            Assert.AreEqual(0, dst.Field);
        }

        [TestMethod]
        public void Trans_SameType_ProducesNewInstance()
        {
            var src = new Target { Number = 3 };
            var dst = TransExpV2<Target, Target>.Trans(src);
            Assert.AreNotSame(src, dst);
            Assert.AreEqual(3, dst.Number);
        }
    }
}

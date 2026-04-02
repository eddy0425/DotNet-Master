using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNet.CvTuples;

namespace DotNet.CvTuplesTest;

[TestClass]
public class TupleConstructorTests
{
    [TestMethod]
    public void TestEmptyConstructor()
    {
        var tuple = new Tuple<int>();
        Assert.AreEqual(TupleType.Int32, tuple.Type);
        Assert.AreEqual(0, tuple.Length);
    }

    [TestMethod]
    public void TestSingleValueConstructor()
    {
        var tuple = new Tuple<int>(42);
        Assert.AreEqual(TupleType.Int32, tuple.Type);
        Assert.AreEqual(1, tuple.Length);
        Assert.AreEqual(42, tuple[0]);
    }

    [TestMethod]
    public void TestArrayConstructor()
    {
        var values = new int[] { 1, 2, 3, 4, 5 };
        var tuple = new Tuple<int>(values);
        Assert.AreEqual(TupleType.Int32, tuple.Type);
        Assert.AreEqual(5, tuple.Length);
        CollectionAssert.AreEqual(values, tuple.ToArray());
    }

    [TestMethod]
    public void TestDoubleConstructor()
    {
        var tuple = new Tuple<double>(3.14159);
        Assert.AreEqual(TupleType.Double, tuple.Type);
        Assert.AreEqual(1, tuple.Length);
        Assert.AreEqual(3.14159, tuple[0], 0.00001);
    }

    [TestMethod]
    public void TestBoolConstructor()
    {
        var tuple = new Tuple<bool>(true, false, true);
        Assert.AreEqual(TupleType.Bool, tuple.Type);
        Assert.AreEqual(3, tuple.Length);
        Assert.AreEqual(true, tuple[0]);
        Assert.AreEqual(false, tuple[1]);
        Assert.AreEqual(true, tuple[2]);
    }
}

[TestClass]
public class TupleFactoryTests
{
    [TestMethod]
    public void TestCreateInt()
    {
        var tuple = CvTuple.Create(1, 2, 3);
        Assert.AreEqual(3, tuple.Length);
        Assert.AreEqual(1, tuple[0]);
        Assert.AreEqual(2, tuple[1]);
        Assert.AreEqual(3, tuple[2]);
    }

    [TestMethod]
    public void TestCreateDouble()
    {
        var tuple = CvTuple.Create(1.0, 2.0, 3.0);
        Assert.AreEqual(3, tuple.Length);
        Assert.AreEqual(1.0, tuple[0], 0.001);
    }

    [TestMethod]
    public void TestRange()
    {
        var tuple = CvTuple.Range(5);
        Assert.AreEqual(5, tuple.Length);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4 }, tuple.ToArray());
    }

    [TestMethod]
    public void TestRangeWithStart()
    {
        var tuple = CvTuple.Range(10, 5);
        Assert.AreEqual(5, tuple.Length);
        CollectionAssert.AreEqual(new int[] { 10, 11, 12, 13, 14 }, tuple.ToArray());
    }

    [TestMethod]
    public void TestLinSpace()
    {
        var tuple = CvTuple.LinSpace(0, 1, 5);
        Assert.AreEqual(5, tuple.Length);
        Assert.AreEqual(0.0, tuple[0], 0.001);
        Assert.AreEqual(0.25, tuple[1], 0.001);
        Assert.AreEqual(0.5, tuple[2], 0.001);
        Assert.AreEqual(0.75, tuple[3], 0.001);
        Assert.AreEqual(1.0, tuple[4], 0.001);
    }

    [TestMethod]
    public void TestRepeat()
    {
        var tuple = CvTuple.Repeat(42, 5);
        Assert.AreEqual(5, tuple.Length);
        foreach (var val in tuple)
        {
            Assert.AreEqual(42, val);
        }
    }
}

[TestClass]
public class TupleIndexingTests
{
    [TestMethod]
    public void TestSingleIndexAccess()
    {
        var tuple = new Tuple<int>(10, 20, 30, 40, 50);
        Assert.AreEqual(10, tuple[0]);
        Assert.AreEqual(30, tuple[2]);
        Assert.AreEqual(50, tuple[4]);
    }

    [TestMethod]
    public void TestIndexFromEnd()
    {
        var tuple = new Tuple<int>(10, 20, 30, 40, 50);
        Assert.AreEqual(50, tuple[^1]);
        Assert.AreEqual(40, tuple[^2]);
    }

    [TestMethod]
    public void TestRangeSlice()
    {
        var tuple = new Tuple<int>(10, 20, 30, 40, 50);
        var slice = tuple[1..4];
        Assert.AreEqual(3, slice.Length);
        CollectionAssert.AreEqual(new int[] { 20, 30, 40 }, slice.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void TestIndexOutOfRange()
    {
        var tuple = new Tuple<int>(1, 2, 3);
        _ = tuple[5];
    }

    [TestMethod]
    public void TestIndexSetter()
    {
        var tuple = new Tuple<int>(10, 20, 30);
        tuple[1] = 99;
        Assert.AreEqual(99, tuple[1]);
    }
}

[TestClass]
public class NumericTupleOperatorTests
{
    [TestMethod]
    public void TestAdditionIntegers()
    {
        var t1 = new NumericTuple<int>(10, 20, 30);
        var t2 = new NumericTuple<int>(5, 10, 15);
        var result = t1 + t2;
        CollectionAssert.AreEqual(new int[] { 15, 30, 45 }, result.ToArray());
    }

    [TestMethod]
    public void TestSubtractionIntegers()
    {
        var t1 = new NumericTuple<int>(10, 20, 30);
        var t2 = new NumericTuple<int>(5, 10, 15);
        var result = t1 - t2;
        CollectionAssert.AreEqual(new int[] { 5, 10, 15 }, result.ToArray());
    }

    [TestMethod]
    public void TestMultiplicationIntegers()
    {
        var t1 = new NumericTuple<int>(10, 20, 30);
        var t2 = new NumericTuple<int>(2, 3, 4);
        var result = t1 * t2;
        CollectionAssert.AreEqual(new int[] { 20, 60, 120 }, result.ToArray());
    }

    [TestMethod]
    public void TestDivisionIntegers()
    {
        var t1 = new NumericTuple<int>(10, 20, 30);
        var t2 = new NumericTuple<int>(2, 4, 5);
        var result = t1 / t2;
        CollectionAssert.AreEqual(new int[] { 5, 5, 6 }, result.ToArray());
    }

    [TestMethod]
    public void TestAdditionDoubles()
    {
        var t1 = new NumericTuple<double>(1.5, 2.5, 3.5);
        var t2 = new NumericTuple<double>(0.5, 1.0, 1.5);
        var result = t1 + t2;
        var expected = new double[] { 2.0, 3.5, 5.0 };
        var actual = result.ToArray();
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], actual[i], 0.0001);
        }
    }
}

[TestClass]
public class TupleExtensionOperatorTests
{
    [TestMethod]
    public void TestSum()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5);
        var sum = tuple.Sum();
        Assert.AreEqual(15, sum);
    }

    [TestMethod]
    public void TestAverage()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5);
        var avg = tuple.Average();
        Assert.AreEqual(3.0, avg, 0.001);
    }

    [TestMethod]
    public void TestMin()
    {
        var tuple = new Tuple<int>(5, 2, 8, 1, 9);
        var min = tuple.Min();
        Assert.AreEqual(1, min);
    }

    [TestMethod]
    public void TestMax()
    {
        var tuple = new Tuple<int>(5, 2, 8, 1, 9);
        var max = tuple.Max();
        Assert.AreEqual(9, max);
    }

    [TestMethod]
    public void TestNegate()
    {
        var tuple = new Tuple<int>(10, -20, 30);
        var result = tuple.Negate();
        CollectionAssert.AreEqual(new int[] { -10, 20, -30 }, result.ToArray());
    }
}

[TestClass]
public class TupleConcatTests
{
    [TestMethod]
    public void TestConcat()
    {
        var t1 = new Tuple<int>(1, 2, 3);
        var t2 = new Tuple<int>(4, 5, 6);
        var result = t1.Concat(t2);
        Assert.AreEqual(6, result.Length);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 }, result.ToArray());
    }

    [TestMethod]
    public void TestConcatMultiple()
    {
        var t1 = new Tuple<int>(1, 2);
        var t2 = new Tuple<int>(3, 4);
        var t3 = new Tuple<int>(5, 6);
        var result = CvTuple.Concat(t1, t2, t3);
        Assert.AreEqual(6, result.Length);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 }, result.ToArray());
    }
}

[TestClass]
public class MixedTupleTests
{
    [TestMethod]
    public void TestMixedTupleCreation()
    {
        var mixed = CvTuple.CreateMixed(1, 2.5, "Hello", true);
        Assert.AreEqual(TupleType.Mixed, mixed.Type);
        Assert.AreEqual(4, mixed.Length);
    }

    [TestMethod]
    public void TestMixedTupleAccess()
    {
        var mixed = CvTuple.CreateMixed(42, 3.14, "Test");
        Assert.AreEqual(42, mixed.GetInt32(0));
        Assert.AreEqual(3.14, mixed.GetDouble(1), 0.001);
        Assert.AreEqual("Test", mixed.GetString(2));
    }

    [TestMethod]
    public void TestMixedTupleArithmetic()
    {
        var m1 = CvTuple.CreateMixed(10, 20, 30);
        var m2 = CvTuple.CreateMixed(5, 10, 15);
        var result = m1 + m2;
        Assert.AreEqual(15, result.GetInt32(0));
        Assert.AreEqual(30, result.GetInt32(1));
        Assert.AreEqual(45, result.GetInt32(2));
    }
}

[TestClass]
public class TupleValueTests
{
    [TestMethod]
    public void TestTupleValueInt()
    {
        TupleValue val = 42;
        Assert.AreEqual(TupleType.Int32, val.Type);
        Assert.AreEqual(42, val.AsInt32);
    }

    [TestMethod]
    public void TestTupleValueDouble()
    {
        TupleValue val = 3.14;
        Assert.AreEqual(TupleType.Double, val.Type);
        Assert.AreEqual(3.14, val.AsDouble, 0.001);
    }

    [TestMethod]
    public void TestTupleValueString()
    {
        TupleValue val = "Hello";
        Assert.AreEqual(TupleType.String, val.Type);
        Assert.AreEqual("Hello", val.AsString);
    }

    [TestMethod]
    public void TestTupleValueArithmetic()
    {
        TupleValue v1 = 10;
        TupleValue v2 = 5;
        var result = v1.Add(v2);
        Assert.AreEqual(15, result.AsInt32);
    }
}

[TestClass]
public class TupleTransformTests
{
    [TestMethod]
    public void TestSelect()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5);
        var result = tuple.Select(x => x * 2);
        CollectionAssert.AreEqual(new int[] { 2, 4, 6, 8, 10 }, result.ToArray());
    }

    [TestMethod]
    public void TestWhere()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5, 6);
        var result = tuple.Where(x => x % 2 == 0);
        CollectionAssert.AreEqual(new int[] { 2, 4, 6 }, result.ToArray());
    }

    [TestMethod]
    public void TestReverse()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5);
        var result = tuple.Reverse();
        CollectionAssert.AreEqual(new int[] { 5, 4, 3, 2, 1 }, result.ToArray());
    }
}

[TestClass]
public class TupleModificationTests
{
    [TestMethod]
    public void TestAppend()
    {
        var tuple = new Tuple<int>(1, 2, 3);
        tuple.Append(4);
        Assert.AreEqual(4, tuple.Length);
        Assert.AreEqual(4, tuple[3]);
    }

    [TestMethod]
    public void TestInsert()
    {
        var tuple = new Tuple<int>(1, 2, 4);
        tuple.Insert(2, 3);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, tuple.ToArray());
    }

    [TestMethod]
    public void TestRemoveAt()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4);
        tuple.RemoveAt(2);
        CollectionAssert.AreEqual(new int[] { 1, 2, 4 }, tuple.ToArray());
    }

    [TestMethod]
    public void TestClear()
    {
        var tuple = new Tuple<int>(1, 2, 3, 4, 5);
        tuple.Clear();
        Assert.AreEqual(0, tuple.Length);
    }
}

[TestClass]
public class TupleEqualityTests
{
    [TestMethod]
    public void TestEquals()
    {
        var t1 = new Tuple<int>(1, 2, 3);
        var t2 = new Tuple<int>(1, 2, 3);
        Assert.IsTrue(t1.Equals(t2));
    }

    [TestMethod]
    public void TestNotEquals()
    {
        var t1 = new Tuple<int>(1, 2, 3);
        var t2 = new Tuple<int>(1, 2, 4);
        Assert.IsFalse(t1.Equals(t2));
    }

    [TestMethod]
    public void TestGetHashCode()
    {
        var t1 = new Tuple<int>(1, 2, 3);
        var t2 = new Tuple<int>(1, 2, 3);
        Assert.AreEqual(t1.GetHashCode(), t2.GetHashCode());
    }
}

[TestClass]
public class TupleDisposalTests
{
    [TestMethod]
    public void TestDispose()
    {
        var tuple = new Tuple<int>(1, 2, 3);
        tuple.Dispose();
        Assert.AreEqual(0, tuple.Length);
    }
}

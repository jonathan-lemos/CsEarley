using CsEarley;
using NUnit.Framework;

namespace CsEarleyTests
{
    public class OrderedSetTests
    {
        [Test]
        public void ContainsTest()
        {
            var oset = new OrderedSet<int> {2, 1, 1, 2};
            Assert.True(oset.Contains(2));
            Assert.True(oset.Contains(1));
            Assert.AreEqual(oset.Count, 2);
            oset.Add(3);
            Assert.True(oset.Contains(2));
            Assert.True(oset.Contains(1));
            Assert.True(oset.Contains(3));
            Assert.AreEqual(oset.Count, 3);
        }

        [Test]
        public void EnumerationTest()
        {
            var oset = new OrderedSet<int> {2, 1};
            CollectionAssert.AreEqual(new[] {2, 1}, oset);
            oset.Add(3);
            CollectionAssert.AreEqual(new[] {2, 1, 3}, oset);
        }

        [Test]
        public void ExceptWithTest()
        {
            var oset = new OrderedSet<int> {2, 1, 3};
            oset.ExceptWith(new[] {1, 4});
            CollectionAssert.AreEqual(oset, new[] {2, 3});
        }

        [Test]
        public void IntersectWithTest()
        {
            var oset = new OrderedSet<int> {2, 1, 3};
            oset.IntersectWith(new[] {1, 4});
            CollectionAssert.AreEqual(oset, new[] {1});
        }

        [Test]
        public void IsProperSubsetTest()
        {
            var oset = new OrderedSet<int> {2, 1};
            Assert.True(oset.IsProperSubsetOf(new[] {1, 2, 3}));
            Assert.False(oset.IsProperSubsetOf(new[] {1, 3, 4}));
            Assert.False(oset.IsProperSubsetOf(new[] {1, 2}));
            Assert.False(oset.IsProperSubsetOf(new[] {1}));
        }

        [Test]
        public void IsProperSupersetTest()
        {
            var oset = new OrderedSet<int> {2, 1, 3};
            Assert.True(oset.IsProperSupersetOf(new[] {1, 2}));
            Assert.False(oset.IsProperSupersetOf(new[] {1, 3, 4}));
            Assert.False(oset.IsProperSupersetOf(new[] {1, 2, 3}));
            Assert.False(oset.IsProperSupersetOf(new[] {1, 2, 3, 4}));
        }

        [Test]
        public void IsSubsetTest()
        {
            var oset = new OrderedSet<int> {2, 1};
            Assert.True(oset.IsSubsetOf(new[] {1, 2, 3}));
            Assert.False(oset.IsSubsetOf(new[] {1, 3, 4}));
            Assert.True(oset.IsSubsetOf(new[] {1, 2}));
            Assert.False(oset.IsSubsetOf(new[] {1}));
        }

        [Test]
        public void IsSupersetTest()
        {
            var oset = new OrderedSet<int> {2, 1, 3};
            Assert.True(oset.IsSupersetOf(new[] {1, 2}));
            Assert.False(oset.IsSupersetOf(new[] {1, 3, 4}));
            Assert.True(oset.IsSupersetOf(new[] {1, 2, 3}));
            Assert.False(oset.IsSupersetOf(new[] {1, 2, 3, 4}));
        }

        [Test]
        public void OverlapsTest()
        {
            var oset = new OrderedSet<int> {2, 1};
            Assert.True(oset.Overlaps(new[] {1}));
            Assert.True(oset.Overlaps(new[] {1, 3}));
            Assert.True(oset.Overlaps(new[] {1, 3, 2}));
            Assert.False(oset.Overlaps(new[] {3, 4}));
        }

        [Test]
        public void UnionWithTest()
        {
            var oset = new OrderedSet<int> {2, 1, 3};
            oset.UnionWith(new[] {1, 4});
            CollectionAssert.AreEqual(oset, new[] {2, 1, 3, 4});
        }
    }
}
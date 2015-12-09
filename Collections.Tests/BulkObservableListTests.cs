using System;
using NUnit.Framework;

namespace SIL.Collections.Tests
{
	[TestFixture]
	public class BulkObservableListTests
	{
		[Test]
		public void MoveRange_OldIndexLessThanNewIndex_ItemsMoved()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			list.MoveRange(0, 2, 3);
			Assert.That(list, Is.EqualTo(new[] {"c", "d", "e", "a", "b"}));
		}

		[Test]
		public void MoveRange_OldIndexGreaterThanNewIndex_ItemsMoved()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			list.MoveRange(3, 2, 0);
			Assert.That(list, Is.EqualTo(new[] {"d", "e", "a", "b", "c"}));
		}

		[Test]
		public void MoveRange_OldIndexEqualToNewIndex_ItemsNotMoved()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			list.MoveRange(1, 2, 1);
			Assert.That(list, Is.EqualTo(new[] {"a", "b", "c", "d", "e"}));
		}

		[Test]
		public void MoveRange_InvalidOldIndex_Throws()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			Assert.That(() => list.MoveRange(5, 2, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MoveRange_InvalidCount_Throws()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			Assert.That(() => list.MoveRange(3, 3, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MoveRange_InvalidNewIndex_Throws()
		{
			var list = new BulkObservableList<string> {"a", "b", "c", "d", "e"};
			Assert.That(() => list.MoveRange(0, 2, 4), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}
	}
}

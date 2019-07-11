using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CollatzExperiments
{
	public class RangeSet
	{
		private SortedSet<BigIntRangeBound> _sortedSet;

		public RangeSet() {
			_sortedSet = new SortedSet<BigIntRangeBound>();
		}

		public BigInteger? Min => _sortedSet.Count == 0 ? (BigInteger?)null : _sortedSet.Min.Bound;
		public BigInteger? Max => _sortedSet.Count == 0 ? (BigInteger?)null : _sortedSet.Max.Bound;

		private int _rangeCount = -1;
		private void InvalidateRangeCount() {
			_rangeCount = -1;
		}
		public int Count
			=> _rangeCount >= 0
			? _rangeCount
			: (_rangeCount = _sortedSet.Select(b => b.Direction == Direction.Nowhere ? 2 : 1).Sum()/2);

		public IEnumerable<BigIntRange> IncludedRanges => EnumerateIncludedRanges(reverse: false);
		public IEnumerable<BigInteger> IncludedValues => EnumerateIncludedValues(reverse: false);

		//	public void DebugBounds() {
		//		Console.WriteLine("#50: Bounds: " + string.Join(", ", _sortedSet.Select(x => x.Bound + "-->" + x.Direction)));
		//	}
	
		public IEnumerable<BigIntRange> EnumerateIncludedRanges(bool reverse = false)
		{
			if (reverse) {
				//If reverse is true, reverse the order of the provided bounds. However, they are expected to be in
				//ascending order, so to make this true, multiply them by negative 1 (and reverse their Directions).
				//Then, take the negative of the results to get the correct output, still in reverse order.
				return RangeSet.EnumerateIncludedRanges(
					sortedBounds: _sortedSet.Reverse().Select(x => x.GetNegative())
				).Select(x => x.GetNegative());			
			} else {
				return RangeSet.EnumerateIncludedRanges(sortedBounds: _sortedSet);
			}
		}

		public IEnumerable<BigInteger> EnumerateIncludedValues(BigIntRange searchRange, bool reverse = false)
		{
			if (reverse) {
				//If reverse is true, reverse the order of the provided bounds. However, they are expected to be in
				//ascending order, so to make this true, multiply them by negative 1 (and reverse their Directions,
				//as well as taking the negative of the search range's limits). Then, take the negative of the results
				//to get the correct output, still in reverse order.
				return RangeSet.EnumerateExcludedValues(
					sortedBounds: _sortedSet.Reverse().Select(x => x.GetNegative()),
					searchRange: searchRange.GetNegative()
				).Select(x => -x);
			} else {
				return RangeSet.EnumerateExcludedValues(
					sortedBounds: _sortedSet,
					searchRange: searchRange
				);
			}
		}
		public IEnumerable<BigInteger> EnumerateIncludedValues(bool reverse = false)
		{
			if (_sortedSet.Count == 0) return Enumerable.Empty<BigInteger>();
			else return EnumerateIncludedValues(BigIntRange.CreateStartEnd(this.Min.Value, this.Max.Value), reverse);
		}

		public IEnumerable<BigInteger> EnumerateExcludedValues(BigIntRange searchRange, bool reverse = false)
		{
			if (reverse) {
				//If reverse is true, reverse the order of the provided bounds. However, they are expected to be in
				//ascending order, so to make this true, multiply them by negative 1 (and reverse their directions,
				//as well as taking the negative of the search range's limits). Then, take the negative of the results,
				//to get the correct output, still in reverse order.
				return RangeSet.EnumerateExcludedValues(
					sortedBounds: _sortedSet.Reverse().Select(x => x.GetNegative()),
					searchRange: searchRange.GetNegative()
				).Select(x => -x);
			} else {
				return RangeSet.EnumerateExcludedValues(
					sortedBounds: _sortedSet,
					searchRange: searchRange
				);
			}
		}
		public IEnumerable<BigInteger> EnumerateExcludedValues(bool reverse = false)
		{
			if (_sortedSet.Count == 0) return Enumerable.Empty<BigInteger>();
			else return EnumerateExcludedValues(BigIntRange.CreateStartEnd(this.Min.Value, this.Max.Value), reverse);
		}

		private static IEnumerable<BigIntRange> EnumerateIncludedRanges(IEnumerable<BigIntRangeBound> sortedBounds)
		{
			BigInteger? prevBound = null; //only non-null when the previous bound is incomplete and needs the next bound before anything can be returned
			foreach (var bound in sortedBounds) 
			{
				if (bound.Direction == Direction.Nowhere)
				{
					if (prevBound != null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - previous bound (at value " + prevBound.Value + ") expected a matching closing bound next but current bound (at value " + bound.Bound + ") represents a single point.");
					yield return BigIntRange.Single(bound.Bound);
				}
				else if (bound.Direction == Direction.Up)
				{
					if (prevBound != null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - previous bound (at value " + prevBound.Value + ") expected a matching closing bound next but current bound (at value " + bound.Bound + ") represents the start of a new range.");
					prevBound = bound.Bound;
				}
				else
				{
					if (prevBound == null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - current bound (at value " + bound.Bound + ") represents the end of a range, but the previous bound was a complete range or there was no previous bound");
					yield return BigIntRange.CreateStartEnd(prevBound.Value, bound.Bound);
					prevBound = null;
				}
			}
		}

		private static IEnumerable<BigInteger> EnumerateIncludedValues(IEnumerable<BigIntRangeBound> sortedBounds, BigIntRange searchRange)
		{
			BigInteger i = searchRange.Start;

			foreach (var bound in sortedBounds)
			{
				while (i <= searchRange.End)
				{
					if (bound.Direction == Direction.Up) {
						//Move i to the start of the range, but only if that means moving forwards,
						//and then continue to the next bound
						i = BigInteger.Max(i, bound.Bound);
						break;
					}
					else if (bound.Direction == Direction.Down) {
						//Keep returning sequential values until i passes the bound (or the end of the search range)
						if (i > bound.Bound) break;
						yield return i;
						i++;
					}
					else {
						if (searchRange.Includes(bound.Bound)) {
							yield return bound.Bound;
							i = bound.Bound + 1;
						}
						break;
					}
				}
			}
		}

		private static IEnumerable<BigInteger> EnumerateExcludedValues(IEnumerable<BigIntRangeBound> sortedBounds, BigIntRange searchRange)
		{
			//	Console.WriteLine("40.0");
		
			var includedValues = EnumerateIncludedValues(sortedBounds, searchRange);

			BigInteger prev = searchRange.Start - 1;
			foreach (BigInteger included in includedValues)
			{
				//	Console.WriteLine("40.1: " + included + ", " + prev);
			
				//If there's been a jump of at least 2 (ie 1 number in gap),
				//loop until all numbers in the gap have been yielded
				while (prev + 1 < included) {
					prev++;
					yield return prev;
				}

				prev = included;
			}
			//	Console.WriteLine("40.2: searchRange.End: " + searchRange.End + ", prev: " + prev);

			//Yield all final values (that are in a gap) until the search range is exited
			while (prev < searchRange.End) {
				prev++;
				yield return prev;
			}
		}
	
		public bool Includes(BigInteger x) {
			BigIntRange range; //Not used
			return TryGetSurroundingIncludedRange(x, out range);
		}
	
		public bool InclduesInEntirety(BigIntRange range) {
			BigIntRange foundRange;
			return TryGetSurroundingIncludedRange(range.Start, out foundRange) && foundRange.Start <= range.Start && range.End <= foundRange.End;
		}
	
		public bool TryGetSurroundingGap(BigInteger x, out BigIntRange gap) {
			bool rangeIsIncluded;
			if (TryGetSurroundingRange(x, out gap, out rangeIsIncluded)) {
				if (rangeIsIncluded) {
					gap = default(BigIntRange);
					return false;
				} else {
					return true;
				}
			} else {
				gap = default(BigIntRange);
				return false;
			}
		}
	
		public bool TryGetSurroundingIncludedRange(BigInteger x, out BigIntRange range) {
			bool rangeIsIncluded;
			if (TryGetSurroundingRange(x, out range, out rangeIsIncluded)) {
				if (rangeIsIncluded) {
					return true;
				} else {
					range = default(BigIntRange);
					return false;
				}
			} else {
				range = default(BigIntRange);
				return false;
			}
		}
	
		public bool TryGetSurroundingRange(BigInteger x, out BigIntRange range, out bool rangeIsIncluded)
		{
			BigIntRangeBound below;
			BigIntRangeBound above;
			if (TryGetSurroundingBounds(x, out below, out above))
			{
				switch (below.Direction) {
					case Direction.Nowhere:
						range = BigIntRange.Single(below.Bound); //below == above
						rangeIsIncluded = true;
						return true;
					case Direction.Down:
						switch (above.Direction) {
							case Direction.Down: throw new InvalidOperationException(
								"Underlying sorted set of range bounds is corrupt - the bound below or equal to " + nameof(x) + "=" + x + " "
								+ "(at value " + below.Bound + ") represents the end of a range, but the bound above or equal "
								+ "to " + nameof(x) + "=" + x + " (at value " + above.Bound + ") also represents the end of a range, meaning "
								+ "this second range would have no starting value."
							);
							case Direction.Nowhere: //Same behaviour as for Direction.Up
							case Direction.Up:
								if (BigInteger.Abs(below.Bound - above.Bound) >= 2) { //If there is a gap between the bounds
									range = BigIntRange.CreateStartEnd(below.Bound + 1, above.Bound - 1);
									rangeIsIncluded = false;
									return true;
								} else {
									range = default(BigIntRange);
									rangeIsIncluded = default(bool);
									return false;
								}
							default: break;
						}
						break;
					case Direction.Up:
						switch (above.Direction) {
							case Direction.Nowhere: throw new InvalidOperationException(
								"Underlying sorted set of range bounds is corrupt - the bound below or equal to " + nameof(x) + "=" + x + " "
								+ "(at value " + below.Bound + ") represents the start of a range, so and end to this range is expected next, "
								+ "but the bound above or equal to " + nameof(x) + "=" + x + " (at value " + above.Bound + ") represents a range "
								+ "containing only single value."
							);
							case Direction.Up: throw new InvalidOperationException(
								"Underlying sorted set of range bounds is corrupt - the bound below or equal to " + nameof(x) + "=" + x + " "
								+ "(at value " + below.Bound + ") represents the end of a range, so and end to this range is expected next, "
								+ "but the bound above or equal to " + nameof(x) + "=" + x + " (at value " + above.Bound + ") represents "
								+ "the start of another range."
							);
							case Direction.Down:
								range = BigIntRange.CreateStartEnd(below.Bound, above.Bound);
								rangeIsIncluded = true;
								return true;
							default: break;
						}
						break;
					default: break;
				}
			}
		
			range = default(BigIntRange);
			rangeIsIncluded = default(bool);
			return false;
		}
	
		//	public bool TryGetSurroundingRange(BigInteger x, out BigIntRange range) {
		//		//This is where using SortedSet doesn't work so well - ideally we'd be able to traverse
		//		//over the inner tree properly to do this search in O(log n) time (or if SortedSet had
		//		//'TryGetLessThan()' and 'TryGetGreaterThan()' methods in addition to TryGetValue()). Instead...	
		//		
		//		BigInteger? prevLowerBound = null; //only non-null when the previous bound has direction Direction.Up
		//		foreach (var bound in sortedBounds) 
		//		{
		//			if (bound.Bound >= x)
		//			{
		//				if (bound.Direction == Direction.Nowhere) {
		//					if (prevLowerBound != null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - previous bound (at value " + prevLowerBound.Value + ") expected a matching closing bound next but current bound (at value " + bound.Bound + ") represents a single point.");
		//					range = BigIntRange.Single(bound.Bound);
		//					return true;
		//				}
		//				else if (bound.Direction == Direction.Up) {
		//					if (prevLowerBound != null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - previous bound (at value " + prevLowerBound.Value + ") expected a matching closing bound next but current bound (at value " + bound.Bound + ") represents the start of a new range.");
		//					range = default(BigIntRange);
		//					return false;
		//				}
		//				else {
		//					if (prevLowerBound == null) throw new InvalidOperationException("Underlying sorted set of range bounds is corrupt - current bound (at value " + bound.Bound + ") represents the end of a range, but the previous bound was a complete range or there was no previous bound");
		//					range = BigIntRange.CreateStartEnd(prevBound.Value, bound.Bound);
		//					return true;
		//				}
		//			}
		//			else
		//			{
		//				prevLowerBound = bound.Direction == Direction.Up ? bound : null;
		//			}
		//		}
		//		
		//		range = default(BigIntRange);
		//		return false;
		//		
		//		//	BigInteger? prevLowerBound = null;
		//		//	foreach (var bound in _sortedSet) {
		//		//		if (bound.Bound >= x) {
		//		//			if (bound.Direction == Direction.Nowhere) {
		//		//				if (prevLowerBound != null) todo;
		//		//				range = BigIntRange.CreateStartEnd(start: prevLowerBound.Value, end: bound);
		//		//				return true;
		//		//			} else if (bound.Direction == Direction.Down) {
		//		//				if (prevLowerBound != null) todo;
		//		//				range = BigIntRange.CreateStartEnd(start: prevLowerBound.Value, end: bound);
		//		//				return true;
		//		//			}
		//		//			else {
		//		//				if (prevLowerBound != 
		//		//				range = default(BigIntRange);
		//		//				return false;
		//		//			}
		//		//		}
		//		//		
		//		//	}
		//	}
	
		private bool TryGetSurroundingBounds(BigInteger x, out BigIntRangeBound below, out BigIntRangeBound above) {
			//This is where using SortedSet doesn't work so well - ideally we'd be able to traverse
			//over the inner tree properly to do this search in O(log n) time (or if SortedSet had
			//'TryGetLessThan()' and 'TryGetGreaterThan()' methods in addition to TryGetValue()). Instead...	
		
			BigIntRangeBound? prevBound = null;
			foreach (var bound in _sortedSet)
			{
				if (bound.Bound <= x)
				{
					if (bound.Direction == Direction.Nowhere) {
						below = bound;
						above = bound;
						return true;
					} else {
						prevBound = bound;
					}
				}
				else
				{
					if (prevBound == null) {
						below = default(BigIntRangeBound);
						above = default(BigIntRangeBound);
						return false;
					} else {
						below = prevBound.Value;
						above = bound;
						return true;					
					}
				}
			}
		
			below = default(BigIntRangeBound);
			above = default(BigIntRangeBound);
			return false;
		}
	
		public void AddRange(BigIntRange range)
		{
			//	Console.WriteLine("51: " + range);
		
			SortedSet<BigIntRangeBound> nearbyBounds = GetOverlappingOrAdjacentBounds(range.Start, range.End);

			if (nearbyBounds.Count == 0)
			{
				//No existing ranges are adjacent or *partially* overlapping, however there could
				//be an existing range that entirely includes the new range. To check for that,
				//check if some value in the new range is already included in the RangeSet.
				if (this.Includes(range.Start)) {
					//Don't need to do anything - the new range is already included.
				} else {
					//Need to add the range, and there's no bounds nearby to affect doing this, so its easy
					if (range.Start == range.End) {
						AddBound(range.Start, Direction.Nowhere);
					} else {
						AddBound(range.Start, Direction.Up  );
						AddBound(range.End  , Direction.Down);
					}
				}
			}
			else
			{
				var minNearby = nearbyBounds.Min;
				var maxNearby = nearbyBounds.Max;

				RemoveSubset(nearbyBounds); //All contents & ends are unneeded (both ends are to be either deleted or modified/re-added)
			
				bool resultingRangeIsPoint = (
					//If the new min and max are different, the resulting range will include at least 2 points
					range.Start == range.End
					//If there's more than one existing bound, they have to point either be at different points,
					//or point different ways, so the resulting range won't be a single point
					&& nearbyBounds.Count == 1
					//If the new point and the existing bound are different, the resulting range will include at least 2 points
					&& range.Start == minNearby.Bound
					//If the existing bound points up or down, the resulting range will be more than a single point
					&& minNearby.Direction == Direction.Nowhere
				);
			
				if (resultingRangeIsPoint)
				{
					AddBound(range.Start, Direction.Nowhere); //minimum == maximum == minInExpandedRange == maxInExpandedRange
				}
				else
				{
					//If the minimum existing bound doesn't point down (ie. join onto a lower range), provide a new lower bound
					if (minNearby.Direction != Direction.Down) {
						AddBound(BigInteger.Min(minNearby.Bound, range.Start), Direction.Up);
					}

					//If the maximum existing bound doesn't point up (ie. join onto a higher range), provide a new upper bound
					if (maxNearby.Direction != Direction.Up) {
						AddBound(BigInteger.Max(maxNearby.Bound, range.End), Direction.Down);
					}
				}
			}
		}

		public void RemoveRange(BigIntRange range)
		{
			SortedSet<BigIntRangeBound> nearbyBounds = GetOverlappingOrAdjacentBounds(range.Start, range.End);

			if (nearbyBounds.Count == 0) {
				//There's no bounds nearby, but there might still be some larger range
				//that includes the range to be removed. To check for this, check if
				//some value in the new range is already included in the RangeSet.
				if (this.Includes(range.Start)) {
					AddBound(range.Start - 1, Direction.Down);
					AddBound(range.End   + 1, Direction.Up  );
				}
			}
			else
			{
				var minNearby = nearbyBounds.Min;
				var maxNearby = nearbyBounds.Max;

				RemoveSubset(nearbyBounds);

				if (minNearby.Direction == Direction.Down) {
					//If lowest existing pointed down (ie. joins onto a lower range),
					//then add a new bound outside the removed range, pointing down
					AddBound(range.Start - 1, Direction.Down);
				}
				else if (minNearby.Bound == range.Start - 1) {
					//If the lowest existing bound is exactly one below the removed range (and doesn't point down),
					//then removing the range cuts its range down to a single point, so add that point
					AddBound(range.Start - 1, Direction.Nowhere);
				}

				//Vice versa for maximum
				if (maxNearby.Direction == Direction.Up) {
					AddBound(range.End + 1, Direction.Up);
				}
				else if (maxNearby.Bound == range.End + 1) {
					AddBound(range.End + 1, Direction.Nowhere);
				}
			}
		}

		private void AddBound(BigInteger bound, Direction direction)
		{
			_sortedSet.Add(
				new BigIntRangeBound(bound, direction)
			);
			InvalidateRangeCount();
		}

		private void RemoveSubset(SortedSet<BigIntRangeBound> subset)
		{
			subset.Clear();
			InvalidateRangeCount();
		}

		///<summary>Returns an editable view of all existing bounds that are overlapping or adjacent to the provided range.</summary>
		private SortedSet<BigIntRangeBound> GetOverlappingOrAdjacentBounds(BigInteger minimum, BigInteger maximum)
		{
			//To find adjacent ranges, just expand the new range by 1 on each side (and ensure
			//that all existing bounds at those points will be included, by making the lower
			//bound's direction as low as possible, and vice versa for the upper bound)
			var expandedMinimum = new BigIntRangeBound(minimum - 1, Direction.Down);
			var expandedMaximum = new BigIntRangeBound(maximum + 1, Direction.Up);
			return _sortedSet.GetViewBetween(expandedMinimum, expandedMaximum);
		}

		//The whole idea of storing individual bounds is somewhat ugly and shouldn't be
		//visible to the caller, so keep the whole class private to help ensure this
		private struct BigIntRangeBound : IComparable<BigIntRangeBound>
		{
			/// <summary>The maximum and/or minimum value of the represented range (which one is determined by <see cref="Direction"/>)</summary>
			public readonly BigInteger Bound;

			/// <summary>
			/// The direction from <see cref="Bound"/> from which values included in the range are to be found.
			/// Will always be exactly <see cref="Direction.Down"/>, <see cref="Direction.Nowhere"/>, or <see cref="Direction.Up"/>.
			/// </summary>
			public readonly Direction Direction;
		
			public BigIntRangeBound(BigInteger bound, Direction direction) {
				this.Bound = bound;
				this.Direction = (Direction)Math.Sign((sbyte)direction); //Make sure it's just -1, 0, or 1
			}
	
			public BigIntRangeBound WithDirection(Direction newDirection) => new BigIntRangeBound(this.Bound, newDirection);

			/// <summary>Returns new BigIntRangeBound(-Bound, (Direction)(-((sbyte)Direction)))</summary>
			public BigIntRangeBound GetNegative() => new BigIntRangeBound(-this.Bound, (Direction)(-((sbyte)Direction)));
		
			public static bool Equals(BigIntRangeBound a, BigIntRangeBound b) => a.Bound == b.Bound && a.Direction == b.Direction;
			public override bool Equals(object other) => other is BigIntRangeBound && Equals(this, (BigIntRangeBound)other);
		
			public override int GetHashCode() {
				unchecked {
					int hash = 17;
					hash = hash * 23 + Bound.GetHashCode();
					hash = hash * 23 + Direction.GetHashCode();
					return hash;
				}
			}
		
			public int CompareTo(BigIntRangeBound other) {
				int boundComparison = this.Bound.CompareTo(other.Bound);
				if (boundComparison != 0) return boundComparison;
		
				return Math.Sign((sbyte)this.Direction).CompareTo(Math.Sign((sbyte)other.Direction));
			}
		
			public static bool operator ==(BigIntRangeBound a, BigIntRangeBound b) => Equals(a, b);
			public static bool operator !=(BigIntRangeBound a, BigIntRangeBound b) => !(a==b);
		
			public static bool operator >(BigIntRangeBound a, BigIntRangeBound b) => a.CompareTo(b) > 0;
			public static bool operator <(BigIntRangeBound a, BigIntRangeBound b) => a.CompareTo(b) < 0;
		
			public static bool operator >=(BigIntRangeBound a, BigIntRangeBound b) => a.CompareTo(b) >= 0;
			public static bool operator <=(BigIntRangeBound a, BigIntRangeBound b) => a.CompareTo(b) <= 0;
		}

		private enum Direction : sbyte //memory use is important in this project, have hit problems with it
		{
			Up = 1,
			Down = -1,
			Nowhere = 0
		}
	}
}

//*/
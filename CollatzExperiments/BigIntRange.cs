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
	public struct BigIntRange
	{
		public static readonly BigIntRange ZeroToZero = default(BigIntRange);
	
		private BigInteger _countMinusOne;
	
		public BigInteger Start { get; }
		public BigInteger Count => _countMinusOne + 1;
	
		public BigInteger End => Start + _countMinusOne;

		public BigIntRange(BigInteger start, BigInteger count) {
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), count, "Must be at least 1.");
		
			this.Start = start;
			this._countMinusOne = count - 1;
		}
	
		public static BigIntRange CreateStartEnd(BigInteger start, BigInteger end) {
			if (end < start) throw new ArgumentOutOfRangeException(
				nameof(end),
				end,
				"Must be greater than or equal to " + nameof(start) + " value '" + start + "'."
			);
		
			return new BigIntRange(start: start, count: (end - start) + 1);
		}
	
		public static BigIntRange Single(BigInteger point) => new BigIntRange(start: point, count: 1);
	
		public bool Includes(BigInteger x) => Start <= x && x <= End;
		public bool IsAdjacentOrIncludes(BigInteger x) => Start - 1 <= x && x <= End + 1;
	
		public bool IsBelowInt(BigInteger x) => x > End;
		public bool IsAboveInt(BigInteger x) => x < Start;
	
		public bool OverlapsWith(BigIntRange other) => other.Start <= this.End && other.End >= this.Start;

		public bool IsAdjacentOrOverlaps(BigIntRange other) => other.Start <= this.End + 1 && other.End >= this.Start - 1;

		public bool IncludesInEntirety(BigIntRange other) => this.Start <= other.Start && other.End <= this.End;

		public BigIntRange Expand(BigInteger downBy, BigInteger upBy) => BigIntRange.CreateStartEnd(this.Start - downBy, this.End + upBy);

		public BigIntRange ExpandToInclude(BigInteger x) => BigIntRange.CreateStartEnd(
			start: BigInteger.Min(this.Start, x),
			end:   BigInteger.Max(this.End  , x)
		);
	
		//Not a clear name & difficult to define (it's not a union - what is it)
		//	public static BigIntRange Combine(BigIntRange a, BigIntRange b) => BigIntRange.CreateStartEnd(
		//		start: BigInteger.Min(a.Start, b.Start),
		//		end:   BigInteger.Max(a.End  , b.End  )
		//	);
		//Instead do:
		public BigIntRange ExpandToInclude(BigIntRange other) => BigIntRange.CreateStartEnd(
			start: BigInteger.Min(this.Start, other.Start),
			end:   BigInteger.Max(this.End  , other.End  )
		);

		/// <summary>Gets a range from (-End) to (-Start)</summary>
		public BigIntRange GetNegative() => BigIntRange.CreateStartEnd(start: -this.End, end: -this.Start);

		public IEnumerable<BigInteger> EnumerateValues() {
			BigInteger i = this.Start;
			while (i <= this.End) {
				yield return i;
				i++;
			}
		}
		public IEnumerable<BigInteger> EnumerateValuesBackwards() {
			BigInteger i = this.End;
			while (i >= this.Start) {
				yield return i;
				i--;
			}
		}

		public override string ToString() => "[" + Start + "..." + End + "]";

		public static bool Equals(BigIntRange a, BigIntRange b) => a.Start == b.Start && a.End == b.End;
		public override bool Equals(object obj) => obj is BigIntRange && Equals(this, (BigIntRange)obj);

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + Start.GetHashCode();
				hash = hash * 23 + _countMinusOne.GetHashCode();
				return hash;
			}
		}
	}
}

//*/
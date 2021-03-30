using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using static CSharpEquivalent.Helpers;

namespace CSharpEquivalent
{
	public static class Predicates
	{
		public static bool scan(int m, int s, Func<int, bool> ignoreRule)
			=> between(1, int.MaxValue, out var nums)
			&& nums.Any(
				num => !ignoreRule(num)
				&& Write(num)
				&& Write(": ")
				&& testNum(m, s, num)
				&& false
			);

		public static bool testNum(int m, int s, int num)
			=> Write(num)
			&& Write(": ")
			&& (reachable(m, s, num) ? Write("reachable") : Write("unreachable"))
			&& NL;

		public static bool testNums(int m, int s, IImmutableStack<int> nums)
			=> !nums.IsEmpty
			&& Logic.Succeed(out var tNums, nums.Pop(out var hNums))
			&& testNum(m, s, hNums)
			&& testNums(m, s, tNums);

		public static bool findZeros(int m, int s, Func<int, bool> ignoreRule)
			=> between(1, int.MaxValue, out var nums)
			&& nums.Any(
				num => !ignoreRule(num)
				&& testZero(m, s, num)
			);

		public static bool testZero(int m, int s, int num)
			=> (reachable(m, s, num) ? false : Write(num) && NL)
			&& false;

		public static bool reachable(int m, int s, int num)
			=> num > 0
			&& m > 0
			&& s > 0
			&& reachable(m, s, num, out var _);

		public static bool reachable(int m, int s, int num, out IEnumerable<int> lastAddedPow)
			=> Logic.SucceedIf(lastAddedPowCandidate(m, s, num, out var lastAddedPow_options))
			&& Logic.SucceedIf(lastAddedPow_options.Where(
				x => reachableFrom(m, s, num, x, 1, 0)
			).SucceedIfAny(out lastAddedPow))
			|| Logic.Fail(out lastAddedPow);

		public static bool reachableFrom(int m, int s, int num, int lastAddedPow, int start, int startPrevPow)
			=> num == start
			&& lastAddedPow == startPrevPow
			&& start > 0
			&& startPrevPow >= 0
			|| inrange(num, lastAddedPow, start, startPrevPow)
			&& dividingStep(m, s, num, out var divided)
			&& reachableFrom(m, s, divided, lastAddedPow, start, startPrevPow)
			|| inrange(num, lastAddedPow, start, startPrevPow)
			&& subtractingStep(m, s, num, lastAddedPow, out int subtracted, out int prevPow)
			&& reachableFrom(m, s, subtracted, prevPow, start, startPrevPow);

		public static bool inrange(int num, int lastAddedPow, int start, int startPrevPow)
			=> start > 0
			&& startPrevPow >= 0
			&& num > 0
			&& lastAddedPow >= 0
			&& num > start;

		public static bool dividingStep(int m, int s, int num, out int divided)
			=> Logic.Succeed(out divided, Math.DivRem(num, m, out var remainder))
			&& remainder == 0;

		public static bool subtractingStep(int m, int s, int num, int lastAddedPow, out int newNum, out int newLastAddedPow)
			=> Logic.Succeed(out newNum, num - (int)Math.Pow(s, lastAddedPow))
			&& Logic.Succeed(out newLastAddedPow, lastAddedPow - 1)
			|| Logic.Fail(out newNum, out newLastAddedPow);

		public static bool lastAddedPowCandidate(int m, int s, int num, out IEnumerable<int> lastAddedPowCandidate)
			=> Logic.Succeed(out var maxPow, (int)Math.Ceiling(Math.Log(num) / Math.Log(s)))
			&& between(0, maxPow, out lastAddedPowCandidate)
			|| Logic.Fail(out lastAddedPowCandidate);

		public static bool between(int min, int max, int value)
			=> min <= value
			&& value <= max;

		public static bool between(int min, int max, out IEnumerable<int> value)
			=> min <= max
			&& Logic.Succeed(out value, Enumerable.Range(min, count: max - min + 1))
			|| Logic.Fail(out value);
	}
}

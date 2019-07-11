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
	///<summary>Designed to be used only by EnumerateBinaryDecisionSequenceResults(int max).
	///For efficiency, does not provide any error checking.</summary>
	public class TwoThreeDecisionTracker
	{
		private readonly int[] _doublingsSinceLastPowerOfThree;
		private int _count;
		public long Current { get; private set; }
	
		public int Max { get; }
	
		public bool IsAtRoot => _count == 1 && _doublingsSinceLastPowerOfThree[0] == 0;
		public int LastAddedThreeExponent => _count - 1;
	
		//For keeping track of progress - monitored asynchronously (hackish but doesn't matter here)
		private static long _globalRepeatedDoublingOpsDone;
		public static long GlobalRepeatedDoublingOpsDone => _globalRepeatedDoublingOpsDone;
	
		public TwoThreeDecisionTracker(int max)
		{
			this.Max = max;
			this._doublingsSinceLastPowerOfThree = new int[GetRequiredCapacity(this.Max)];
			this.Current = 1;
			this._doublingsSinceLastPowerOfThree[0] = 0; //Purely for clarity with setting _count=1; unneeded
			this._count = 1;
		}
	
		private static int GetRequiredCapacity(int max) {
			long x = 1;
			int i = 1;
			while (x <= max) {
				//x += MathUtils.IntThreeToThe(i);
				x += (long)BigInteger.Pow(3, i);
				i++;
			}
			return i; //might be one higher than needed, idk, but that's fine anyway
		}
	
		public void DoubleNTimes(int n) { unchecked {
			_doublingsSinceLastPowerOfThree[_count - 1] += n;
			Current *= MathUtils.IntTwoToThe(n);
			//Current *= (int)BigInteger.Pow(2, n);
		}}
	
		public void Halve() { unchecked {
			_doublingsSinceLastPowerOfThree[_count - 1]--;
			Current /= 2;
		}}
	
		public bool TryAddNextPowerOf3() { unchecked {
			long next = Current + MathUtils.LongThreeToThe(_count);
			//long next = Current + (long)BigInteger.Pow(3, _count);
			//	Console.WriteLine("next: " + next + ", _count: " + _count);
			if (next <= Max) {
				_count += 1;
				//_doublingsSinceLastPowerOfThree[_count - 1] = 0; //not strictly needed, but with the lack of error checking (eg. in Halve()), could be a good idea
				Current = next;
				return true;
			} else {
				return false;
			}
		}}
	
		public bool BacktrackAndCheckIfWasDoublingOp() { unchecked {
			if (_doublingsSinceLastPowerOfThree[_count - 1] > 0) {
				Current /= 2;
				_doublingsSinceLastPowerOfThree[_count - 1]--;
				return true;
			} else {
				_count -= 1;
				Current -= MathUtils.IntThreeToThe(_count); //Shouldnt this be long??
				//Current -= (int)BigInteger.Pow(3, _count);
				return false;
			}
		}}
	
		public int GetNumDoublingsBeforeExceedingMax() { unchecked {
			//n doublings results in a value equal to 2^n * Current
			//Solving the following for n:
			//	2^n * Current = Max
			//	2^n = Max/Current
			//	n = log2(Max/Current)
			//.'. n doublings takes it to exactly Max, so floor(n) doublings will
			//be the last valid integer number of doublings.
			//	numDoublings = floor(n) = floor(log2(Max/Current))
			//log(x) increases whenever x increases, and for x >= 1, log(x) always has a
			//gradient less than 1, so for x >= 1 floor(log(x)) = floor(log(floor(x))).
			//The lowest value Current will ever take is 1, Max must be at least 1, and
			//Current < Max, so the input to the log is always greater than 1. Therefore:
			//	numDoublings = floor(n) = floor(log2(floor(Max/Current)))
			//Max and Current are both positive, so "floor(Max/Current)" is just integer division.
			//Putting this in terms of the relevant methods,
			return MathUtils.FloorLog2((ulong)(Max/Current));
			//return (int)Math.Floor(BigInteger.Log((BigInteger)Math.Floor(Max/(double)Current), 2));
		}}
	
		public int DoubleRepeatedlyUpToMax() { unchecked {
			int doublings = GetNumDoublingsBeforeExceedingMax();
			DoubleNTimes(n: doublings);
			_globalRepeatedDoublingOpsDone++;
			return doublings;
		}}
	
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(/*"[" + */_doublingsSinceLastPowerOfThree[0]);
			for (int i = 1; i < _count; i++) {
				sb.Append("," + _doublingsSinceLastPowerOfThree[i]);
			}
			//sb.Append("]");
			return sb.ToString();
		}
	}
}

//*/
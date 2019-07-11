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
	//Binary decision between multiplying by multiplier and adding the next sequential power of summand
	//Starts from 1, first power of summand added is summand^1 = summand
	public class BinaryDecisionTracker
	{
		public int Max { get; }
		public int Multiplier { get; }
		public int Summand { get; }

		private readonly int[] _multiplicationsSinceLastAddition;
		private int _count;
		public long Current { get; private set; }

		//Used to avoid divide by zero when undoing the multiplications
		private int _multZeroCount = 0;
		private readonly long[] _valuesBeforeMultZero;

		public bool IsAtRoot => _count == 1 && _multiplicationsSinceLastAddition[0] == 0;
		public int LastAddedExponent => _count - 1;

		//For keeping track of progress - monitored asynchronously (hackish but doesn't matter here)
		private static long _globalRepeatedFirstPathsDone;
		public static long GlobalRepeatedFirstPathsDone => _globalRepeatedFirstPathsDone;

		public BinaryDecisionTracker(int max, int multiplier, int summand)
		{
			this.Max = max;
			this.Multiplier = multiplier;
			this.Summand = summand;
			this._multiplicationsSinceLastAddition = new int[GetRequiredCapacity(this.Max, this.Summand)];
			this.Current = 1;
			this._multiplicationsSinceLastAddition[0] = 0; //Purely for clarity with setting _count=1; unneeded
			this._count = 1;

			this._multZeroCount = 0;
			if (multiplier == 0) {
				this._valuesBeforeMultZero = new long[GetValuesBeforeMultZeroCapacity(this.Max, this.Summand)];
			}
		}

		private static int GetRequiredCapacity(int max, int summand) {
			//only max and summand are needed, not the multiplier, as that just determines
			//the maximum value of each element in the array, not the number of elements
			if (summand == 0) return 1;

			long x = 1;
			int i = 1;
			while (x <= max) {
				x += (long)Math.Pow(summand, i);
				i++;
			}
			return i; //might be one higher than needed, idk, but that's fine anyway
		}

		private static int GetValuesBeforeMultZeroCapacity(int max, int summand)
		{
			if (summand == 0) return 2;
			if (summand == 1) return 2;

			return (int)Math.Ceiling(Math.Log(max, summand)) + 1;
		}

		public void MultiplyOnce()
		{
			if (Multiplier == 0) {
				_valuesBeforeMultZero[_multZeroCount] = Current;
				_multZeroCount++;
			}
			Current *= Multiplier;
			_multiplicationsSinceLastAddition[_count - 1]++;
		}

		public void MultiplyNTimes(int n) {
			unchecked {
				if (n == 0) return; //Avoid 0^0

				if (Multiplier == 0) {
					_valuesBeforeMultZero[_multZeroCount] = Current;
					_multZeroCount++;
				}
				Current *= MathUtils.LookupLongPow(Multiplier, n);
				_multiplicationsSinceLastAddition[_count - 1] += n;
			}
		}

		public void DivideOnce() {
			unchecked {
				if (Multiplier == 0) {
					Current = _valuesBeforeMultZero[_multZeroCount - 1];
					_multZeroCount--;
				} else {
					Current /= Multiplier;
				}
				_multiplicationsSinceLastAddition[_count - 1]--;
			}
		}

		public bool TryAddNextPowerOfSummand() {
			unchecked {
				if (Summand == 0 || (Summand == 1 && Current == 0))
				{
					return false;
				}

				long next;
				next = Current + (long)Math.Pow(Summand, _count);

				if (next <= Max) {
					_count += 1;
					//_multiplicationsSinceLastAddition[_count - 1] = 0; //not strictly needed, but with the lack of error checking (eg. in Halve()), could be a good idea
					Current = next;
					return true;
				} else {
					return false;
				}
			}
		}

		public bool BacktrackAndCheckIfWasMultiplyOp() {
			unchecked {
				if (_multiplicationsSinceLastAddition[_count - 1] > 0) {
					DivideOnce();
					return true;
				} else {
					_count -= 1;
					Current -= MathUtils.LookupLongPow(Summand, _count);
					return false;
				}
			}
		}

		public int GetNumMultiplicationsBeforeExceedingMax() {
			unchecked {
				if (Multiplier == 0) {
					return 1; //Could multiply more than once, but no point //TODO: Test
				} else if (Multiplier == 1) {
					return 0; //No point multiplying at all //TODO: Test
				}

				//n multiplications results in a value equal to Multiplier^n * Current
				//Solving the following for n:
				//	Multiplier^n * Current = Max
				//	Multiplier^n = Max/Current
				//	n = log_Multiplier(Max/Current)
				//.'. n multiplications takes it to exactly Max, so floor(n) multiplications
				//will be the last valid integer number of multiplications.
				//	numMultiplications = floor(n) = floor(log_Multiplier(Max/Current))
				//log(x) increases whenever x increases, and for x >= 1, log(x) always has a
				//gradient less than 1, so for x >= 1 floor(log(x)) = floor(log(floor(x))).
				//The lowest value Current will ever take is 1 (given Multiplier > 1), Max
				//must be at least 1, and Current < Max, so the input to the log is always
				//greater than 1. Therefore:
				//	numMultiplications = floor(n) = floor(log_Multiplier(floor(Max/Current)))
				//Max and Current are both positive, so "floor(Max/Current)" is just integer division.
				//Putting this in terms of the relevant methods,
				return (int)Math.Floor(BigInteger.Log(Max/Current, Multiplier));
				//return (int)Math.Floor(BigInteger.Log((BigInteger)Math.Floor(Max/(double)Current), Multiplier));
			}
		}

		public void TakeFirstPathRepeatedlyUpToMax() {
			unchecked {
				if (Multiplier != 0)
				{
					MultiplyNTimes(n: GetNumMultiplicationsBeforeExceedingMax());
				}
				else
				{
					//In this case, multiplying makes it possible to effectively move backwards,
					//but is only useful once - after that it does nothing and an addition is
					//required before repeating.
					do {
						MultiplyOnce();
					} while (TryAddNextPowerOfSummand());
				}
				_globalRepeatedFirstPathsDone++;
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(/*"[" + */_multiplicationsSinceLastAddition[0]);
			for (int i = 1; i < _count; i++) {
				sb.Append("," + _multiplicationsSinceLastAddition[i]);
			}
			//sb.Append("]");
			return sb.ToString();
		}
	}
}

//*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CollatzExperiments
{
	//Binary decision between multiplying by multiplier and adding the next sequential power of summand
	//Starts from 1, first power of summand added is summand^1 = summand
	//Z = the name of the resulting number (from context elsewhere, arbitrary name here)
	//M = multiplier
	//S = summand
	public static class BinaryDecisionZMSFinder
	{
		public static IEnumerable<long> EnumerateBinaryDecisionSequenceResults(int max, int multiplier, int summand)
		{
			if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
				nameof(max),
				max,
				nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
			);
			if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
			if (multiplier < 0) throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Cannot be negative.");
			if (summand < 0) throw new ArgumentOutOfRangeException(nameof(summand), summand, "Cannot be negative.");

			var decisionTracker = new BinaryDecisionTracker(max: max, multiplier: multiplier, summand: summand);

			//Console.WriteLine("#A : " + decisionTracker.Current + " " + decisionTracker.ToString());
			decisionTracker.TakeFirstPathRepeatedlyUpToMax();

			while (decisionTracker.TryAddNextPowerOfSummand()) {
				//Console.WriteLine("#A2: " + decisionTracker.Current + " " + decisionTracker.ToString());
			}

			//Console.WriteLine("#B : " + decisionTracker.Current + " " + decisionTracker.ToString());
			yield return decisionTracker.Current; //Reached maximum length of this branch; yield the branch's result

			while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
			{
				//Console.WriteLine("#C : " + decisionTracker.Current + " " + decisionTracker.ToString());
				if (decisionTracker.BacktrackAndCheckIfWasMultiplyOp())
				{
					//Console.WriteLine("#D : " + decisionTracker.Current + " " + decisionTracker.ToString());
					if (decisionTracker.TryAddNextPowerOfSummand())
					{
						//Console.WriteLine("#E : " + decisionTracker.Current + " " + decisionTracker.ToString());
						decisionTracker.TakeFirstPathRepeatedlyUpToMax();

						while (decisionTracker.TryAddNextPowerOfSummand()) {
							//Console.WriteLine("#E2: " + decisionTracker.Current + " " + decisionTracker.ToString());
						}

						//Console.WriteLine("#F : " + decisionTracker.Current + " " + decisionTracker.ToString());
						yield return decisionTracker.Current; //Reached maximum length of this branch; yield the branch's result
					}
					else
					{
						//Console.WriteLine("#G : " + decisionTracker.Current + " " + decisionTracker.ToString());
						//Adding the next power of 3 makes it too big; loop around to backtrack again
						//But first, yield the current branch as we've now explored all sub-branches (see below)
						yield return decisionTracker.Current;

						continue; 
					}
				}
				else
				{
					//Console.WriteLine("#H : " + decisionTracker.Current + " " + decisionTracker.ToString());
					//Explored all sub-branches of the current branch; yield the current branch
					//When we first go into the branch, we're doubling repeatedly so can't yield at every step,
					//but here we can't do anything repeatedly, so for efficiency do the yielding here.
					yield return decisionTracker.Current;
				}
			}
		}

		public static int[] GetExpansionCounts(int max, int multiplier, int summand)
		{
			//	if (multiplier <= 1 || summand <= 1)
			//	{
			//		int[] expansionCounts = new int[max * 4];
			//	
			//		foreach (long sum in EnumerateBinaryDecisionSequenceResults(max, multiplier, summand)) {
			//			expansionCounts[sum + 2 * max]++;
			//		}
			//	
			//		return expansionCounts;
			//	}
			//	else
			//	{
				int[] expansionCounts = new int[max + 1];

				foreach (long sum in EnumerateBinaryDecisionSequenceResults(max, multiplier, summand)) {
					if (sum < 0) {
						"".ToString();
					} else {
						expansionCounts[sum]++;
					}
				}

				return expansionCounts;
			//	}

		}

		public static LongChunkedArray<int> GetExpansionCounts_chunkedArray(int max, int multiplier, int summand)
		{
			var expansionCounts = new LongChunkedArray<int>(max + 1, 16777216);

			foreach (long sum in EnumerateBinaryDecisionSequenceResults(max, multiplier, summand)) {
				expansionCounts[sum]++;
			}

			return expansionCounts;
		}

		public static IEnumerable<int> GetZeros(int max, int multiplier, int summand)
		{
			if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
				nameof(max),
				max,
				nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
			);
			if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");

			var decisionTracker = new BinaryDecisionTracker(max: max, multiplier: multiplier, summand: summand);
			var expansionRegister = new LongChunkedArray<int>(max + 1, 16777216);
			// ^ For each position corresponding to a result found, contains exponent that was used on the
			//last power of 3 added to get to that result. This is encoded as a binary flag, so that if a
			//given result is reached multiple times using different powers of 3, all of the powers can be
			//recorded without needing additional storage.
			//	//If that result has been reached multiple times,
			//	//contains the one that was reached most recently.
			//	//	//contains the one that's closest to 3/4 * log3(10,000). This is a rough estimate for the
			//	//	//exponent that is likely to be repeated the most, saving the most steps.

			//Console.WriteLine("#A : " + decisionTracker.Current + " " + decisionTracker.ToString());
			decisionTracker.TakeFirstPathRepeatedlyUpToMax();

			while (decisionTracker.TryAddNextPowerOfSummand()) {
				//Console.WriteLine("#A2: " + decisionTracker.Current + " " + decisionTracker.ToString());
			}

			//Console.WriteLine("#B : " + decisionTracker.Current + " " + decisionTracker.ToString());
			//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
			//yield return decisionTracker.Current;
			//Reached maximum length of this branch; yield the branch's result
			expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedExponent;

			while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
			{
				//Console.WriteLine("#C : " + decisionTracker.Current + " " + decisionTracker.ToString());
				if (decisionTracker.BacktrackAndCheckIfWasMultiplyOp())
				{
					if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedExponent)) > 0) continue; //Already done this branch

					//Console.WriteLine("#D : " + decisionTracker.Current + " " + decisionTracker.ToString());
					if (decisionTracker.TryAddNextPowerOfSummand())
					{
						if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedExponent)) > 0) continue; //Already done this branch

						//Console.WriteLine("#E : " + decisionTracker.Current + " " + decisionTracker.ToString());
						decisionTracker.TakeFirstPathRepeatedlyUpToMax();

						if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedExponent)) > 0) continue; //Already done this branch

						while (decisionTracker.TryAddNextPowerOfSummand()) {
							if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedExponent)) > 0) continue; //Already done this branch
							//Console.WriteLine("#E2: " + decisionTracker.Current + " " + decisionTracker.ToString());
						}

						//Console.WriteLine("#F : " + decisionTracker.Current + " " + decisionTracker.ToString());
						//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
						//yield return decisionTracker.Current;
						//Reached maximum length of this branch; yield the branch's result
						expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedExponent;
					}
					else
					{
						//Console.WriteLine("#G : " + decisionTracker.Current + " " + decisionTracker.ToString());
						//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
						//yield return decisionTracker.Current;
						//Adding the next power of 3 makes it too big; loop around to backtrack again
						//But first, yield the current branch as we've now explored all sub-branches (see below)
						expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedExponent;

						continue; 
					}
				}
				else
				{
					//Console.WriteLine("#H : " + decisionTracker.Current + " " + decisionTracker.ToString());
					//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
					//yield return decisionTracker.Current;
					//Explored all sub-branches of the current branch; yield the current branch
					//When we first go into the branch, we're doubling repeatedly so can't yield at every step,
					//but here we can't do anything repeatedly, so for efficiency do the yielding here.
					expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedExponent;
				}
			}

			for (int i = 0; i < expansionRegister.Length; i++)
			{
				if (expansionRegister[i] == 0) yield return i;
			}
		}

		public static List<long> GetNonTrivialZeros(int max, int multiplier, int summand, out List<(long mod, long offset)> trivialModulos)
		{
			var zeros = GetZeros(max: max, multiplier: multiplier, summand: summand).ToList();

			trivialModulos = new List<(long mod, long offset)>();
			//Set a low maximum for what counts as 'trivial' to save on processing time
			//Otherwise this is something like O(n^3)...
			var maxTrivial = Math.Max(2 * multiplier, 2 * summand);
			for (int mod = 0; mod < maxTrivial; mod++) {
				for (int offset = 0; offset < mod; offset++) {
					//Check if this modulo & offset works for all zeros
					bool allZero = true;
					for (int i = 0; i < zeros.Count; i++) {
						allZero &= (zeros[i] - offset) % mod == 0;
					}
					if (allZero) trivialModulos.Add((mod: mod, offset: offset));
				}
			}

			var nonTrivialZeros = new List<long>();
			for (int i = 0; i < zeros.Count; i++) {
				bool fitsNoTrivialModulos = true;
				for (int j = 0; j < trivialModulos.Count; j++) {
					fitsNoTrivialModulos &= (zeros[i] - trivialModulos[j].offset) % trivialModulos[j].mod == 0;
				}
				if (fitsNoTrivialModulos) nonTrivialZeros.Add(zeros[i]);
			}
			return nonTrivialZeros;
		}
	}
}

//*/
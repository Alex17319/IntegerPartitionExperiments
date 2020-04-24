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
	public static class TwoThreeDecisionZFinder
	{
		//Binary decision between doubling and adding the next sequential power of 3
		//Starts from 1, first power of 3 added is 3^1 = 3
		public static IEnumerable<long> EnumerateTwoThreeDecisionSequenceResults(int max) //capToMaximum = true
		{
			if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
				nameof(max),
				max,
				nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
			);
			if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
	
			var decisionTracker = new TwoThreeDecisionTracker(max: max);
	
			//Console.WriteLine("#A : " + decisionTracker.Current + " " + decisionTracker.ToString());
			decisionTracker.DoubleRepeatedlyUpToMax();
	
			while (decisionTracker.TryAddNextPowerOf3()) {
				//Console.WriteLine("#A2: " + decisionTracker.Current + " " + decisionTracker.ToString());
			}
	
			//Console.WriteLine("#B : " + decisionTracker.Current + " " + decisionTracker.ToString());
			yield return decisionTracker.Current; //Reached maximum length of this branch; yield the branch's result
	
			while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
			{
				//Console.WriteLine("#C : " + decisionTracker.Current + " " + decisionTracker.ToString());
				if (decisionTracker.BacktrackAndCheckIfWasDoublingOp())
				{
					//Console.WriteLine("#D : " + decisionTracker.Current + " " + decisionTracker.ToString());
					if (decisionTracker.TryAddNextPowerOf3())
					{
						//Console.WriteLine("#E : " + decisionTracker.Current + " " + decisionTracker.ToString());
						decisionTracker.DoubleRepeatedlyUpToMax();
				
						while (decisionTracker.TryAddNextPowerOf3()) {
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

		//This is the version that was leaving in lots of zeroes.
		//The flaw is that when it reaches step 'F', it might be possible to add the
		//next power of 3 (possibly more than once), but it doesn't attempt to do this.
		//	//Binary decision between doubling and adding the next sequential power of 3
		//	//Starts from 1, first power of 3 added is 3^1 = 3
		//	static IEnumerable<long> EnumerateBinaryDecisionSequenceResults(int max) //capToMaximum = true
		//	{
		//		if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
		//			nameof(max),
		//			max,
		//			nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
		//		);
		//		if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
		//		
		//		var decisionTracker = new BinaryDecisionTracker(max: max);
		//		
		//		Console.WriteLine("#A: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//		decisionTracker.DoubleRepeatedlyUpToMax();
		//		
		//		Console.WriteLine("#B: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//		yield return decisionTracker.Current; //Reached maximum length of this branch; yield the branch's result
		//		
		//		while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
		//		{
		//			Console.WriteLine("#C: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//			if (decisionTracker.BacktrackAndCheckIfWasDoublingOp())
		//			{
		//				Console.WriteLine("#D: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//				if (decisionTracker.TryAddNextPowerOf3())
		//				{
		//					Console.WriteLine("#E: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//					decisionTracker.DoubleRepeatedlyUpToMax();
		//					
		//					Console.WriteLine("#F: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//					yield return decisionTracker.Current; //Reached maximum length of this branch; yield the branch's result
		//				}
		//				else
		//				{
		//					Console.WriteLine("#G: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//					//Adding the next power of 3 makes it too big; loop around to backtrack again
		//					//But first, yield the current branch as we've now explored all sub-branches (see below)
		//					yield return decisionTracker.Current;
		//					
		//					continue; 
		//				}
		//			}
		//			else
		//			{
		//				Console.WriteLine("#H: " + decisionTracker.Current + " " + decisionTracker.ToString());
		//				//Explored all sub-branches of the current branch; yield the current branch
		//				//When we first go into the branch, we're doubling repeatedly so can't yield at every step,
		//				//but here we can't do anything repeatedly, so for efficiency do the yielding here.
		//				yield return decisionTracker.Current;
		//			}
		//		}
		//	}

		public static List<long> GetNonTrivialZeros(int max) //capToMaximum = true
		{
			var expansionRegister = GetExpansionRegister(max);
	
			var nonTrivialZeros = new List<long>();
			for (int i = 0; i < expansionRegister.Length; i++) {
				if (i % 3 != 0 && expansionRegister[i] == 0) nonTrivialZeros.Add(i);
			}
			return nonTrivialZeros;
		}

		//Returns an array that, for each position corresponding to a result found, contains the exponents
		//used on the highest power of 3 involved each time that result was reached, all encoded as binary flags.
		public static LongChunkedArray<int> GetExpansionRegister(int max)
		{
			if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
				nameof(max),
				max,
				nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
			);
			if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
	
			var decisionTracker = new TwoThreeDecisionTracker(max: max);
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
			decisionTracker.DoubleRepeatedlyUpToMax();
	
			while (decisionTracker.TryAddNextPowerOf3()) {
				//Console.WriteLine("#A2: " + decisionTracker.Current + " " + decisionTracker.ToString());
			}
	
			//Console.WriteLine("#B : " + decisionTracker.Current + " " + decisionTracker.ToString());
			//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
			//yield return decisionTracker.Current;
			//Reached maximum length of this branch; yield the branch's result
			expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedThreeExponent;
	
			while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
			{
				//Console.WriteLine("#C : " + decisionTracker.Current + " " + decisionTracker.ToString());
				if (decisionTracker.BacktrackAndCheckIfWasDoublingOp())
				{
					if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedThreeExponent)) > 0) continue; //Already done this branch
			
					//Console.WriteLine("#D : " + decisionTracker.Current + " " + decisionTracker.ToString());
					if (decisionTracker.TryAddNextPowerOf3())
					{
						if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedThreeExponent)) > 0) continue; //Already done this branch
				
						//Console.WriteLine("#E : " + decisionTracker.Current + " " + decisionTracker.ToString());
						decisionTracker.DoubleRepeatedlyUpToMax();
				
						if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedThreeExponent)) > 0) continue; //Already done this branch
				
						while (decisionTracker.TryAddNextPowerOf3()) {
							if ((expansionRegister[decisionTracker.Current] & (1 << decisionTracker.LastAddedThreeExponent)) > 0) continue; //Already done this branch
							//Console.WriteLine("#E2: " + decisionTracker.Current + " " + decisionTracker.ToString());
						}
				
						//Console.WriteLine("#F : " + decisionTracker.Current + " " + decisionTracker.ToString());
						//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
						//yield return decisionTracker.Current;
						//Reached maximum length of this branch; yield the branch's result
						expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedThreeExponent;
					}
					else
					{
						//Console.WriteLine("#G : " + decisionTracker.Current + " " + decisionTracker.ToString());
						//Console.WriteLine(decisionTracker.LastAddedThreeExponent + " " + decisionTracker.Current);
						//yield return decisionTracker.Current;
						//Adding the next power of 3 makes it too big; loop around to backtrack again
						//But first, yield the current branch as we've now explored all sub-branches (see below)
						expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedThreeExponent;
				
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
					expansionRegister[decisionTracker.Current] |= 1 << decisionTracker.LastAddedThreeExponent;
				}
			}

			return expansionRegister;
		}

		//	static IEnumerable<KeyValuePair<long, char>> EnumerateBinaryDecisionSequenceResults_validatable(int max, BinaryDecisionTracker decisionTracker = null) //capToMaximum = true
		//	{
		//		if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
		//			nameof(max),
		//			max,
		//			nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
		//		);
		//		if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
		//		
		//		decisionTracker = decisionTracker ?? new BinaryDecisionTracker(max: max);
		//		
		//		//	Console.WriteLine("#128");
		//		decisionTracker.DoubleRepeatedlyUpToMax();
		//		
		//		//	Console.WriteLine("#129");
		//		yield return new KeyValuePair<long, char>(decisionTracker.Current, 'a'); //Reached maximum length of this branch; yield the branch's result
		//		
		//		while (!decisionTracker.IsAtRoot) //Starts at root but we did an initial repeated doubling; when we get back to the root we're done
		//		{
		//			//	Console.WriteLine("#130: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//			if (decisionTracker.BacktrackAndCheckIfWasDoublingOp())
		//			{
		//				yield return new KeyValuePair<long, char>(decisionTracker.Current, 'b'); //for validation only
		//				
		//				//	Console.WriteLine("#131: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//				if (decisionTracker.TryAddNextPowerOf3())
		//				{
		//					yield return new KeyValuePair<long, char>(decisionTracker.Current, 'c'); //for validation only
		//					
		//					//	Console.WriteLine("#132: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//					decisionTracker.DoubleRepeatedlyUpToMax();
		//					
		//					//	Console.WriteLine("#133: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//					yield return new KeyValuePair<long, char>(decisionTracker.Current, 'd'); //Reached maximum length of this branch; yield the branch's result
		//				}
		//				else
		//				{
		//					//	Console.WriteLine("#134: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//					//Adding the next power of 3 makes it too big; loop around to backtrack again
		//					//But first, yield the current branch as we've now explored all sub-branches (see below)
		//					yield return new KeyValuePair<long, char>(decisionTracker.Current, 'e');
		//					
		//					continue; 
		//				}
		//			}
		//			else
		//			{
		//				//	Console.WriteLine("#135: " + decisionTracker.ToString() + ", " + decisionTracker.Current);
		//				//Explored all sub-branches of the current branch; yield the current branch
		//				//When we first go into the branch, we're doubling repeatedly so can't yield at every step,
		//				//but here we can't do anything repeatedly, so for efficiency do the yielding here.
		//				yield return new KeyValuePair<long, char>(decisionTracker.Current, 'f');
		//			}
		//		}
		//	}
		//	
		//	void ValidateBinaryDecisionSequence_validation1(int max)
		//	{
		//		//Validates that at every step, either the number is multiplied/divided
		//		//by a power of 2, or a power of 3 is added to/subtracted from it.
		//		
		//		var decisionTracker = new BinaryDecisionTracker(max: max);
		//		
		//		int digits = max.ToString().Length;
		//		
		//		var prev = new KeyValuePair<long, char>(decisionTracker.Current, '-');
		//		long i = 0;
		//		foreach (KeyValuePair<long, char> current in EnumerateBinaryDecisionSequenceResults_validatable(max, decisionTracker))
		//		{
		//			if (i % 50000000 == 0) {
		//				Console.WriteLine("@" + i + ", " + current.Key);
		//			}
		//			i++;
		//			
		//			if (current.Key == prev.Key) {
		//				prev = current;
		//				continue;
		//			} else if (current.Key == prev.Key * 2) {
		//				prev = current;
		//				continue;
		//			} else if (prev.Key == current.Key * 2) {
		//				prev = current;
		//				continue;
		//			} else {
		//				long bigger = Math.Max(current.Key, prev.Key);
		//				long smaller = Math.Min(current.Key, prev.Key);
		//				
		//				while (smaller < bigger) {
		//					smaller = smaller * 2;
		//					if (smaller == bigger)
		//					{
		//						break;
		//					}
		//					else if (smaller > bigger)
		//					{
		//						//.'. not dividing or multiplying by a power of 2,
		//						//so check if adding/subtracting a power of 3
		//						
		//						long diff = Math.Abs(prev.Key - current.Key);
		//						while (diff > 1) {
		//							if (diff % 3 == 0) {
		//								diff = diff / 3;
		//							} else {
		//								//.'. doing neither; display an error
		//								Console.WriteLine(
		//									"E2: c:" + current.Key.ToString().PadLeft(digits) +
		//									" p:" + prev.Key.ToString().PadLeft(digits) +
		//									" ct:" + current.Value +
		//									" pt:" + prev.Value +
		//									" d:" + diff.ToString().PadLeft(digits) +
		//									" t:" + decisionTracker.ToString()
		//								);
		//								break;
		//							}
		//						}
		//						
		//						break;
		//					}
		//				}
		//			}
		//			
		//			prev = current;
		//		}
		//	}
		//	
		//	void ValidateBinaryDecisionSequence_validation2(int max)
		//	{
		//		//Validates that after every double-as-much-as-possible step,
		//		//the result, when doubled, overshoots max.
		//		
		//		var decisionTracker = new BinaryDecisionTracker(max: max);
		//		
		//		int digits = max.ToString().Length;
		//		
		//		var prev = new KeyValuePair<long, char>(decisionTracker.Current, '-');
		//		long i = 0;
		//		foreach (KeyValuePair<long, char> current in EnumerateBinaryDecisionSequenceResults_validatable(max, decisionTracker))
		//		{
		//			if (i % 50000000 == 0) {
		//				Console.WriteLine("@" + i + ", " + current.Key);
		//			}
		//			i++;
		//			
		//			if (current.Value == 'd' || current.Value == 'a') {
		//				long doubled = current.Key * 2;
		//				long overshoot = doubled - max;
		//				if (overshoot <= 0) {
		//					Console.WriteLine(
		//						"E3: c:" + current.ToString().PadLeft(digits) +
		//						" p:" + prev.ToString().PadLeft(digits) +
		//						" d:" + (doubled - max).ToString().PadLeft(digits) +
		//						" t:" + decisionTracker.ToString()
		//					);
		//				}
		//			}
		//			
		//			prev = current;
		//		}
		//	}

		//	static void EvaluateDecisionList(params bool[] addThreeDecisions)
		//	{
		//		int current = 1;
		//		int power = 1;
		//		for (int i = 0; i < addThreeDecisions.Length; i++) {
		//			if (addThreeDecisions[i]) {
		//				current += (int)Math.Pow(3, power);
		//				power += 1;
		//			} else  {
		//				current *= 2;
		//			}
		//			Console.Write(current + " ");
		//		}
		//		Console.WriteLine();
		//	}

		//	//Binary decision between doubling and adding the next sequential power of 3
		//	//Starts from 1, first power of 3 added is 3^1 = 3
		//	static IEnumerable<int> EnumerateBinaryDecisionSequenceResults(int max) //capToMaximum = true
		//	{
		//		if (max > int.MaxValue) throw new ArgumentOutOfRangeException(
		//			nameof(max),
		//			max,
		//			nameof(max) + " is greater than int.MaxValue, so not all summed paths would be possible to return."
		//		);
		//		if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), max, "Cannot be less than 1.");
		//		
		//		var stack = new Stack<int>(); //each entry is the number of doublings before/since a power of 3 was added, Count is the number of powers of 3 added
		//		int current = 1;
		//		
		//		//while (current < max) {
		//		//	current *= 2;
		//		//	stack.Push(stack.Pop() + 1);
		//		//}
		//		// ^ This can be made more efficient (& fewer branches):
		//		//   The last doubling that is valid must yield a value lower than max, and x doublings
		//		//   give a value equal to 2^x * current, so solving the following for x:
		//		//   	2^x * current = max
		//		//   	2^x = max/current
		//		//   	x = log2(max/current)
		//		//   The number of times we need to double is floor(x), which is equal to floor(log2(max/current)).
		//		//   log(z) always increases with an increase in z, and above z=1 (inclusive), log(z) always has a
		//		//   gradient less than 1, so this is equal to floor(log2(floor(max/current))):
		//		{		
		//			byte doublings = MathUtils.FloorLog2((ulong)(max/current)); //positive integer division = floor(float division)
		//			//byte doublings = (byte)Math.Log(max/(double)current, 2); //positive integer division = floor(float division)
		//			
		//			current *= MathUtils.IntTwoToThe(doublings);
		//			stack.Push(doublings);
		//			
		//			yield return current;
		//		}
		//		
		//		while (stack.Count > 0)
		//		{
		//			int doublingsSinceLastThree = stack.Pop();
		//			if (doublingsSinceLastThree > 0)
		//			{
		//				stack.Push(doublingsSinceLastThree - 1);
		//				current /= 2;
		//				
		//				var next = current + MathUtils.LongThreeToThe(stack.Count);
		//				if (next <= max) {
		//					stack.Push(0);
		//					current = (int)next;
		//				} else {
		//					//Adding this power of 3 makes it too big, loop around to pop again
		//					continue;
		//				}
		//				
		//				byte doublings = MathUtils.FloorLog2((ulong)(max/current)); //Doublings could well be 0, I think that works fine
		//				//byte doublings = (byte)Math.Log(max/(double)current, 2);
		//				current *= MathUtils.IntTwoToThe(doublings);
		//				stack.Push(stack.Pop() + doublings);
		//				
		//				yield return (int)current; //Reached maximum length of this branch; yield the branch
		//			}
		//			else
		//			{
		//				//Fixed later, after DecisionTracker was written:
		//				//current -= MathUtils.IntThreeToThe(stack.Count + 1);
		//				current -= MathUtils.IntThreeToThe(stack.Count);
		//				
		//				yield return current; //Completed that branch; yield it (when we first go into the branch, we're sometimes using the log shortcut so can't yield)
		//				
		//				continue; //Loop around and pop again
		//			}
		//			
		//			//	if (current < max)
		//			//	{
		//			//		yield return current;
		//			//		
		//			//		stack.Push(false);
		//			//		current *= 2;
		//			//		
		//			//		continue;
		//			//	}
		//			//	else
		//			//	{
		//			//		var lastActionWasThree = stack.Pop();
		//			//		if (lastActionWasThree) {
		//			//			current -= MathUtils.LongThreeToThe(numThreesAdded);
		//			//			numThreesAdded -= 1;
		//			//			
		//			//		} else {
		//			//			current /= 2;
		//			//		}
		//			//	}
		//		}
		//		
		//		//	var stack = new Stack<bool>(); //true if power of three added
		//		//	int numThreesAdded = 0;
		//		//	long current = 1;
		//		//	
		//		//	while (stack.Count > 0)
		//		//	{
		//		//		if (current < max)
		//		//		{
		//		//			yield return current;
		//		//			
		//		//			stack.Push(false);
		//		//			current *= 2;
		//		//			
		//		//			continue;
		//		//		}
		//		//		else
		//		//		{
		//		//			var lastActionWasThree = stack.Pop();
		//		//			if (lastActionWasThree) {
		//		//				current -= MathUtils.LongThreeToThe(numThreesAdded);
		//		//				numThreesAdded -= 1;
		//		//				
		//		//			} else {
		//		//				current /= 2;
		//		//			}
		//		//		}
		//		//	}
		//		
		//		//	if (current < max)
		//		//	{
		//		//		yield return current;
		//		//		
		//		//		stack.Push(false);
		//		//		current *= 2;
		//		//		
		//		//		//same again
		//		//	}
		//		//	else
		//		//	{
		//		//		stack.Pop();
		//		//		current /= 2;
		//		//		
		//		//		stack.Push(true);
		//		//		numThreesAdded++;
		//		//		current += MathUtils.LongThreeToThe(numThreesAdded);
		//		//		
		//		//		if (current < max) {
		//		//			
		//		//		}
		//		//		
		//		//		//	var lastAction = stack.Pop();
		//		//		//	if (lastAction) {
		//		//		//		current -= MathUtils.LongThreeToThe(numThreesAdded);
		//		//		//		numThreesAdded -= 1;
		//		//		//	} else {
		//		//		//		current /= 2;
		//		//		//	}
		//		//	}
		//	}

		public static int[] GetExpansionCounts_twoThreeDecisions(int maxZ)
		{
			var expansionCounts = new int[maxZ + 1];
	
			foreach (long sum in EnumerateTwoThreeDecisionSequenceResults(maxZ)) {
				expansionCounts[sum]++;
			}
	
			return expansionCounts;
		}

		public static LongChunkedArray<int> GetExpansionCounts_twoThreeDecisions_chunkedArray(int maxZ)
		{
			var expansionCounts = new LongChunkedArray<int>(maxZ + 1, 16777216);
	
			foreach (long sum in EnumerateTwoThreeDecisionSequenceResults(maxZ)) {
				expansionCounts[sum]++;
			}
	
			return expansionCounts;
		}
	}
}

//*/
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
	public static class OptionsMatrixZFinder
	{
		///<summary>Enumerates through all possible term options in ascending order</summary>
		public static IEnumerable<ExpansionTerm> EnumerateSortedTermOptions()
		{
			var rowEnds = new List<ExpansionTerm>();
			ExpansionTerm lastReturned = new ExpansionTerm(0, 0);
	
			while (true)
			{
				//Note: There only ever needs to be one value for minNext (i.e. rather than
				//needing a list of ExpansionTerms that all evaluate to the same number), so just
				//finding a single minimum is ok.
				//This is because for the ExpansionTerms, each (ThreeExponent, TwoExponent) pair
				//leads to a unique value when the ExpansionTerm is evaluated (see ExpansionTerm remarks for proof).
				var minNext = (
					Enumerable.Concat(
						rowEnds.Select( //Each item one past the end of the current rows
							x => x.StepBy(threeExpStep: 0, twoExpStep: 1)
						),
						new[] { //The first item of a new row
							new ExpansionTerm(
								threeExponent: rowEnds.Count,
								twoExponent: 0
							)
						}
					)
					.MinBy(x => x.Evaluate())
				);
		
				if (minNext.ThreeExponent == rowEnds.Count) rowEnds.Add(minNext);
				else rowEnds[minNext.ThreeExponent] = minNext;
		
				yield return minNext;
			}
		}

		public static IEnumerable<BigInteger> IterateNonTrivialZeros(BigInteger maxZ, Action<ExpansionTerm> currentPathStartingPointPrinter = null)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			//	var sortedEliminatedRanges = new List<BigIntRange>();
			var eliminatedRanges = new RangeSet();
	
			BigInteger prevStartingPointEval = BigInteger.Zero; //Starts lower than any evaluated starting point
	
			foreach (ExpansionTerm pathStartingPoint in EnumerateSortedTermOptions())
			{
				currentPathStartingPointPrinter?.Invoke(pathStartingPoint);
				//	Console.WriteLine("#25 current starting point: " + pathStartingPoint);
		
				//	//Debugging:
				//	eliminatedRanges.DebugBounds();
				//	foreach (var range in eliminatedRanges.IncludedRanges) {
				//		Console.WriteLine("#40: " + range.Start + " to " + range.End);
				//	}
				//	Console.WriteLine("#41: " + eliminatedRanges.Count);
		
				BigInteger startingPointEval = pathStartingPoint.Evaluate();
		
				if (startingPointEval > maxZ) {
					//If gone past end of requested range, return all remaining stored values
					//up to end of requested range, then stop enumerating.
					var gaps = eliminatedRanges.EnumerateExcludedValues(
						searchRange: BigIntRange.CreateStartEnd(
							start: prevStartingPointEval + 1,
							end: maxZ
						)
					);
					foreach (BigInteger gap in gaps) {
						yield return gap;
					}
					//	foreach (var range in eliminatedRanges.IncludedRanges) {
					//		Console.WriteLine("#42: " + range.Start + " to " + range.End);
					//	}
					//	Console.WriteLine("#43: " + eliminatedRanges.Count);
					yield break;
				}
		
				foreach (BigInteger summedPath in termOptions.EnumerateSummedExpansionPaths(pathStartingPoint, capToMaximum: true))
				{
					//	Console.WriteLine("#44: " + summedPath + ", " + expansionPath.ToString(includeSpaces: false));
			
					BigInteger newRangeMax = summedPath;
					BigInteger newRangeMin = summedPath;
			
					//	Console.WriteLine("#44.1: " + newRangeMin + ", " + newRangeMax);
			
					//Exclude adjacent trivial zeros (multiples of 3) as well
					if ((newRangeMax + 1) % 3 == 0) { 
						newRangeMax += 1;
					}
					if ((newRangeMin - 1) % 3 == 0) {
						newRangeMin -= 1;
					}
			
					//	Console.WriteLine("#44.2: " + newRangeMin + ", " + newRangeMax);
			
					//	Console.WriteLine("#45: " + eliminatedRanges.Count);
					eliminatedRanges.AddRange(BigIntRange.CreateStartEnd(newRangeMin, newRangeMax));
					//	Console.WriteLine("#46: " + eliminatedRanges.Count);
				}
		
				{
					//	Console.WriteLine("#47: " + eliminatedRanges.Count);
					var gaps = eliminatedRanges.EnumerateExcludedValues(
						searchRange: BigIntRange.CreateStartEnd(
							start: prevStartingPointEval + 1,
							end: startingPointEval
						)
					);
					foreach (BigInteger gap in gaps) {
						yield return gap;
					}
					//	Console.WriteLine("#48");
				}
		
				prevStartingPointEval = startingPointEval;
			}
		}

		public static IEnumerable<BigInteger> IterateNonTrivialZeros_bitarray(int maxZ, Action<ExpansionTerm> currentPathStartingPointPrinter = null)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			var eliminatedPoints = new BitArray(length: maxZ + 1);
	
			int prevStartingPointEval = 0; //Starts lower than any evaluated starting point
	
			foreach (ExpansionTerm pathStartingPoint in EnumerateSortedTermOptions())
			{
				currentPathStartingPointPrinter?.Invoke(pathStartingPoint);
		
				int startingPointEval = (int)pathStartingPoint.Evaluate();
		
				if (startingPointEval > maxZ) {
					//If gone past end of requested range, return all remaining stored values
					//up to end of requested range, then stop enumerating.
					for (int i = prevStartingPointEval + 1; i <= maxZ; i++) {
						if (i % 3 != 0 && eliminatedPoints[i] == false) {
							yield return i;
						}
					}
					yield break;
				}
		
				foreach (BigInteger summedPath in termOptions.EnumerateSummedExpansionPaths(pathStartingPoint, capToMaximum: true))
				{
					int intSummedPath = (int)summedPath;
			
					eliminatedPoints[intSummedPath] = true;
				}
		
				for (int i = prevStartingPointEval + 1; i <= startingPointEval; i++) {
					if (i % 3 != 0 && eliminatedPoints[i] == false) {
						yield return i;
					}
				}
		
				prevStartingPointEval = startingPointEval;
			}
		}


		///<param name="onlyNonTrivialZeros">
		///If true, only empty expansion lists will be returned,
		///and of those none will correspond to z values that are divisible by 3.
		///</param>
		public static IEnumerable<KeyValuePair<BigInteger, List<Expansion>>> IterateExpansionLists(BigInteger maxZ, bool onlyNonTrivialZeros = false, Action<ExpansionTerm> currentPathStartingPointPrinter = null)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			var foundExpansions = new Dictionary<BigInteger, List<Expansion>>();
	
			BigInteger prevStartingPointEval = BigInteger.Zero; //Starts lower than any evaluated starting point
	
			foreach (ExpansionTerm pathStartingPoint in EnumerateSortedTermOptions())
			{
				currentPathStartingPointPrinter?.Invoke(pathStartingPoint);
		
				BigInteger startingPointEval = pathStartingPoint.Evaluate();
		
				if (startingPointEval > maxZ) {
					//If gone past end of requested range, return all remaining stored values
					//up to end of requested range, then stop enumerating.
					for (BigInteger i = prevStartingPointEval + 1; i <= maxZ; i++) {
						List<Expansion> listToYield;
						if (TryGetExpansionListToYield(foundExpansions, i, onlyNonTrivialZeros, out listToYield)) {
							yield return new KeyValuePair<BigInteger, List<Expansion>>(i, listToYield);
						}
					}
					yield break;
				}
		
				foreach (var expansionPath in termOptions.EnumerateExpansionPaths(pathStartingPoint, capToMaximum: true))
				{
					BigInteger summedPath = expansionPath.Sum;
			
					List<Expansion> existingList;
					if (foundExpansions.TryGetValue(summedPath, out existingList)) {
						if (!onlyNonTrivialZeros) existingList.Add(expansionPath);
					} else {
						foundExpansions.Add(summedPath, new List<Expansion>() { expansionPath });
					}
				}
		
				for (BigInteger i = prevStartingPointEval + 1; i <= startingPointEval; i++) {
					List<Expansion> listToYield;
					if (TryGetExpansionListToYield(foundExpansions, i, onlyNonTrivialZeros, out listToYield)) {
						yield return new KeyValuePair<BigInteger, List<Expansion>>(i, listToYield);
					}
				}
		
				prevStartingPointEval = startingPointEval;
			}
		}

		static bool TryGetExpansionListToYield(Dictionary<BigInteger, List<Expansion>> foundExpansions, BigInteger z, bool onlyNonTrivialZeros, out List<Expansion> list)
		{
			List<Expansion> existingList;
			if (foundExpansions.TryGetValue(z, out existingList)) {
				if (!onlyNonTrivialZeros) {
					list = existingList;
					return true;
				}
			} else {
				if (onlyNonTrivialZeros) {
					if (z % 3 != 0) {
						list = new List<Expansion>();
						return true;
					}
				} else {
					list = new List<Expansion>();
					return true;
				}
			}
	
			list = null;
			return false;
		}

		///<param name="onlyNonTrivialZeros">
		///If true, only empty expansion lists will be returned,
		///and of those none will correspond to z values that are divisible by 3.
		///</param>
		public static IEnumerable<KeyValuePair<BigInteger, BigInteger>> IterateExpansionCounts(BigInteger maxZ, bool onlyNonTrivialZeros = false, Action<ExpansionTerm> currentPathStartingPointPrinter = null)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			var foundExpansionCounts = new Dictionary<BigInteger, BigInteger>();
	
			BigInteger prevStartingPointEval = BigInteger.Zero; //Starts lower than any evaluated starting point
	
			foreach (ExpansionTerm pathStartingPoint in EnumerateSortedTermOptions())
			{
				currentPathStartingPointPrinter?.Invoke(pathStartingPoint);
		
				BigInteger startingPointEval = pathStartingPoint.Evaluate();
		
				if (startingPointEval > maxZ) {
					//If gone past end of requested range, return all remaining stored values
					//up to end of requested range, then stop enumerating.
					for (BigInteger i = prevStartingPointEval + 1; i <= maxZ; i++) {
						BigInteger countToYield;
						if (TryGetExpansionCountToYield(foundExpansionCounts, i, onlyNonTrivialZeros, out countToYield)) {
							yield return new KeyValuePair<BigInteger, BigInteger>(i, countToYield);
						}
					}
					yield break;
				}
		
				foreach (BigInteger summedPath in termOptions.EnumerateSummedExpansionPaths(pathStartingPoint, capToMaximum: true))
				{
					BigInteger existingCount;
					if (foundExpansionCounts.TryGetValue(summedPath, out existingCount)) {
						if (!onlyNonTrivialZeros) foundExpansionCounts[summedPath] = existingCount + 1;
					} else {
						foundExpansionCounts.Add(summedPath, 1);
					}
				}
		
				for (BigInteger i = prevStartingPointEval + 1; i <= startingPointEval; i++) {
					BigInteger countToYield;
					if (TryGetExpansionCountToYield(foundExpansionCounts, i, onlyNonTrivialZeros, out countToYield)) {
						yield return new KeyValuePair<BigInteger, BigInteger>(i, countToYield);
					}
				}
		
				prevStartingPointEval = startingPointEval;
			}
		}

		static bool TryGetExpansionCountToYield(Dictionary<BigInteger, BigInteger> foundExpansionCounts, BigInteger z, bool onlyNonTrivialZeros, out BigInteger count)
		{
			BigInteger existingCount;
			if (foundExpansionCounts.TryGetValue(z, out existingCount)) {
				if (onlyNonTrivialZeros) {
					count = -1; //i.e. it's not a zero
					return false;
				} else {
					count = existingCount;
					return true;
				}
			} else {
				if (onlyNonTrivialZeros) {
					if (z % 3 != 0) {
						count = 0;
						return true;
					} else {
						count = -1; //i.e. it's not a non-trivial zero
						return false;
					}
				} else {
					count = 0;
					return true;
				}
			}
		}



		///<param name="onlyNonTrivialZeros">
		///If true, only empty expansion lists will be returned,
		///and of those none will correspond to z values that are divisible by 3.
		///</param>
		public static IEnumerable<KeyValuePair<int, int>> IterateExpansionCounts_array(int maxZ, bool onlyNonTrivialZeros = false, Action<ExpansionTerm> currentPathStartingPointPrinter = null)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			var expansionCounts = new int[maxZ + 1];
	
			int prevStartingPointEval = 0; //Starts lower than any evaluated starting point
	
			foreach (ExpansionTerm pathStartingPoint in EnumerateSortedTermOptions())
			{
				currentPathStartingPointPrinter?.Invoke(pathStartingPoint);
		
				int startingPointEval = (int)pathStartingPoint.Evaluate();
		
				if (startingPointEval > maxZ) {
					//If gone past end of requested range, return all remaining stored values
					//up to end of requested range, then stop enumerating.
					for (int i = prevStartingPointEval + 1; i <= maxZ; i++) {
						int countToYield;
						if (TryGetExpansionCountToYield_array(expansionCounts, i, onlyNonTrivialZeros, out countToYield)) {
							yield return new KeyValuePair<int, int>(i, countToYield);
						}
					}
					yield break;
				}
		
				foreach (BigInteger summedPath in termOptions.EnumerateSummedExpansionPaths(pathStartingPoint, capToMaximum: true))
				//foreach (var expansionPath in termOptions.EnumerateExpansionPaths(pathStartingPoint, capToMaximum: true))
				{
					//int intSummedPath = (int)expansionPath.Sum;
					int intSummedPath = (int)summedPath;
			
					int existingCount = expansionCounts[intSummedPath];
					if (!onlyNonTrivialZeros || existingCount == 0) {
						expansionCounts[intSummedPath] = existingCount + 1;
					}
				}
		
				for (int i = prevStartingPointEval + 1; i <= startingPointEval; i++) {
					int countToYield;
					if (TryGetExpansionCountToYield_array(expansionCounts, i, onlyNonTrivialZeros, out countToYield)) {
						yield return new KeyValuePair<int, int>(i, countToYield);
					}
				}
		
				prevStartingPointEval = startingPointEval;
			}
		}

		static bool TryGetExpansionCountToYield_array(int[] expansionCounts, int z, bool onlyNonTrivialZeros, out int count)
		{
			int existingCount = expansionCounts[z];
			if (onlyNonTrivialZeros) {
				if (existingCount == 0 && z % 3 != 0) {
					count = 0;
					return true;
				} else {
					count = -1; //i.e. it's not a non-trivial zero
					return false;
				}
			} else {
				count = existingCount;
				return true;
			}
		}



		public static IEnumerable<KeyValuePair<int, int>> IterateExpansionCounts_arrayAsync(int maxZ)
		{
			var termOptions = new TermOptionsMatrix(maxZ);
	
			var expansionCounts = new int[maxZ + 1];
	
			KeyValuePair<ExpansionTerm, Task>[] startingPointTasks = (
				EnumerateSortedTermOptions()
				.TakeWhile(startingPoint => startingPoint.Evaluate() <= maxZ)
				.Select(
					startingPoint => new KeyValuePair<ExpansionTerm, Task>(	
						key: startingPoint,
						value: Task.Run(
							delegate {
								foreach (int summedPath in termOptions.EnumerateIntSummedExpansionPaths(startingPoint))
								{
									Interlocked.Increment(ref expansionCounts[summedPath]);
								}
								//	foreach (BigInteger summedPath in termOptions.EnumerateSummedExpansionPaths(startingPoint, capToMaximum: true))
								//	{
								//		Interlocked.Increment(ref expansionCounts[(int)summedPath]);
								//	}
							}
						)
					)
				)
				.OrderBy(kvp => kvp.Key)
				.ToArray()
			);
	
			int taskIndex = 0; //Index of first task in startingPointTasks
			int expTargetIndex = 1; //Index of first valid expansion-target in expansionCounts (zero is ignored)
	
			while (taskIndex < startingPointTasks.Length)
			{
				startingPointTasks[taskIndex].Value.Wait();
		
				while (taskIndex < startingPointTasks.Length && startingPointTasks[taskIndex].Value.IsCompleted)
				{
					if (startingPointTasks[taskIndex].Value.Status == TaskStatus.RanToCompletion)
					{
						while (expTargetIndex < startingPointTasks[taskIndex].Key.Evaluate()) {
							yield return new KeyValuePair<int, int>(expTargetIndex, expansionCounts[expTargetIndex]);
							expTargetIndex++;
						}
					}
					else if (startingPointTasks[taskIndex].Value.Status == TaskStatus.Faulted)
					{
						throw new Exception(
							"An error occurred while finding expansion paths for starting point '" + startingPointTasks[taskIndex].Key + "' "
							+ "(at index '" + taskIndex + "' within the list of starting points to be checked asynchronously): ",
							startingPointTasks[taskIndex].Value.Exception
						);
					}
					else //Cancelled, somehow
					{
						throw new Exception(
							"Error: The task used to find expansion paths for starting point '" + startingPointTasks[taskIndex].Key + "' "
							+ "(at index '" + taskIndex + "' within the list of starting points to be checked asynchronously) was cancelled."
						);
					}
			
					taskIndex++;
				}
			}
	
			while (expTargetIndex <= maxZ) {
				yield return new KeyValuePair<int, int>(expTargetIndex, expansionCounts[expTargetIndex]);
				expTargetIndex++;
			}
	
			//	//	var waitableTasksArray = startingPointTasks.Select(kvp => kvp.Value).ToArray();
			//	
			//	int prevTaskReturned = -1;
			//	int prevExpansionTargetReturned = 0; //Starts lower than any possible starting point
			//	
			//	Console.WriteLine("#99: " + expansionCounts.Length);
			//	
			//	while (startingPointTasks.Any(t => !t.Value.IsCompleted))
			//	{
			//		startingPointTasks[prevTaskReturned + 1].Value.Wait();
			//		//	Task.WaitAny(waitableTasksArray); //think this might continue immediately if one's already complete
			//		
			//		int i = prevTaskReturned + 1;
			//		Console.WriteLine("#100: " + i);
			//		while (i < startingPointTasks.Length && startingPointTasks[i].Value.IsCompleted)
			//		{
			//			if (startingPointTasks[i].Value.Status == TaskStatus.RanToCompletion)
			//			{
			//				int j = prevExpansionTargetReturned + 1;
			//				while (j < startingPointTasks[i].Key.Evaluate()) {
			//					Console.WriteLine("#101: " + j + ", " + startingPointTasks[i].Key.Evaluate());
			//					yield return new KeyValuePair<int, int>(j, expansionCounts[j]);
			//					Console.WriteLine("#102");
			//					prevExpansionTargetReturned = j;
			//					j++;
			//				}
			//			}
			//			else if (startingPointTasks[i].Value.Status == TaskStatus.Faulted)
			//			{
			//				Console.WriteLine("#105");
			//				throw new Exception(
			//					"An error occurred while finding expansion paths for starting point '" + startingPointTasks[i].Key + "' "
			//					+ "(at index '" + i + "' within the list of starting points to be checked asynchronously): ",
			//					startingPointTasks[i].Value.Exception
			//				);
			//			}
			//			else //Cancelled, somehow
			//			{
			//				Console.WriteLine("#106");
			//				throw new Exception(
			//					"Error: The task used to find expansion paths for starting point '" + startingPointTasks[i].Key + "' "
			//					+ "(at index '" + i + "' within the list of starting points to be checked asynchronously) was cancelled."
			//				);
			//			}
			//			
			//			prevTaskReturned = i;
			//			i++;
			//		}
			//	}
			//	
			//	Console.WriteLine("#103");
			//	for (int j = prevExpansionTargetReturned + 1; j <= maxZ; j++) {
			//		Console.WriteLine("#104: " + j);
			//		yield return new KeyValuePair<int, int>(j, expansionCounts[j]);
			//	}
		}
	}
}

//*/
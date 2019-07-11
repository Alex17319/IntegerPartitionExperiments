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
using CollatzExperiments;
using System.Numerics;

namespace Executor
{
	static class Printing
	{
		public static void PrintExpansionPaths(BigInteger maxZ, ExpansionTerm pathStartingPoint, bool capToMaximum = true)
		{
			Console.WriteLine("maxZ: " + maxZ + ", pathStartingPoint: " + pathStartingPoint);
	
			var termOptions = new TermOptionsMatrix(maxZ);
			//foreach (var path in EnumerateExpansionPaths(termOptions.TermOptions.Select(x => x.ToList()).ToList(), pathStartingPoint, maxZ: maxZ)) {
			foreach (var path in termOptions.EnumerateExpansionPaths(pathStartingPoint, capToMaximum: capToMaximum)) {
				Console.WriteLine(
					path.ToString(Expansion.Format.EvaluateFully) + " = "
					+ path.ToString(Expansion.Format.ThreeTwoCoordinates, includeSpaces: true)
				);
			}
		}

		public static void PrintExpansionTermOptions(BigInteger z)
		{
			var termOptions = new TermOptionsMatrix(z);
			for (int i = 0; i < termOptions.TermOptions.Count(); i++) {
				for (int j = 0; j < termOptions[i].Count(); j++) {
					Console.Write(termOptions[i][j].Evaluate() + " ");
				}
				Console.WriteLine();
			}
		}

		public static void PrintSortedTermOptions(BigInteger max)
		{
			foreach (var opt in OptionsMatrixZFinder.EnumerateSortedTermOptions()) {
				if (opt.Evaluate() > max) return;
				else Console.WriteLine(
					opt.ToString(ExpansionTerm.Format.Evaluate, includeSpaces: true)
					+ " = "
					+ opt.ToString(ExpansionTerm.Format.FullFormula, includeSpaces: true)
				);
			}
		}

		public static void PrintNonTrivialZeros(BigInteger maxZ, bool toFile)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			const char backspaceChar = '\u0008';
	
			Action<ExpansionTerm> currentPathStartingPointPrinter;
			if (toFile) {
				currentPathStartingPointPrinter = null;		
			} else {		
				currentPathStartingPointPrinter = pathStartingPoint => {
					Console.Write(">" + pathStartingPoint.Evaluate().ToString().PadLeft(digits) + "...");
					Console.Write(new string(backspaceChar, 1 + digits + 3)); //Move back but don't actually clear - it will be overwritten
				};
			}
	
			foreach (BigInteger nonTrivialZero in OptionsMatrixZFinder.IterateNonTrivialZeros(maxZ, currentPathStartingPointPrinter))
			{
				if (toFile) {
					Console.WriteLine(nonTrivialZero.ToString().PadLeft(digits));
				} else {
					Console.WriteLine(" " + nonTrivialZero.ToString().PadLeft(digits) + "   ");
				}
			}
			//	foreach (var kvp in IterateExpansionLists(maxZ, onlyNonTrivialZeros: true, currentPathStartingPointPrinter: currentPathStartingPointPrinter))
			//	{
			//		//	Console.WriteLine(kvp.Key.ToString().PadLeft(digits));
			//		PrintExpansionList(
			//			kvp.Value,
			//			z: kvp.Key,
			//			digits: digits,
			//			expansionFormat: Expansion.Format.FullFormula,
			//			includeSpaces: false
			//		);
			//	}
	
			Console.WriteLine(("Done from 1 to " + maxZ + ".").PadRight(digits)); //include PadRight(digits) to guarantee that the ongoing number is fully overwritten
		}

		public static void PrintNonTrivialZeros_bitarray(int maxZ, bool toFile)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			const char backspaceChar = '\u0008';
	
			Action<ExpansionTerm> currentPathStartingPointPrinter;
			if (toFile) {
				currentPathStartingPointPrinter = null;		
			} else {		
				currentPathStartingPointPrinter = pathStartingPoint => {
					Console.Write(">" + pathStartingPoint.Evaluate().ToString().PadLeft(digits) + "...");
					Console.Write(new string(backspaceChar, 1 + digits + 3)); //Move back but don't actually clear - it will be overwritten
				};
			}
	
			foreach (BigInteger nonTrivialZero in OptionsMatrixZFinder.IterateNonTrivialZeros_bitarray(maxZ, currentPathStartingPointPrinter))
			{
				if (toFile) {
					Console.WriteLine(nonTrivialZero.ToString().PadLeft(digits));
				} else {
					Console.WriteLine(" " + nonTrivialZero.ToString().PadLeft(digits) + "   ");
				}
			}
	
			Console.WriteLine(("Done from 1 to " + maxZ + ".").PadRight(digits)); //include PadRight(digits) to guarantee that the ongoing number is fully overwritten
		}

		public static void PrintNonTrivialZeros_VisibleProgress(BigInteger maxZ)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			const char backspaceChar = '\u0008';
			string deleteLastNumberStr = new string(backspaceChar, digits);
	
			const int speed = 500; //Number used to modify estimates, for printing somewhat more efficiently
	
			Console.Write((1).ToString().PadLeft(digits)); //Initial search-ongoing number, to be cleared & printed over
	
			foreach (var kvp in OptionsMatrixZFinder.IterateExpansionLists(maxZ))
			{
				BigInteger z = kvp.Key;
				List<Expansion> expList = kvp.Value;
		
				if (z % 3 != 0 && expList.Count == 0) {
					Console.Write(deleteLastNumberStr); //Clear last number printed
					Console.Write(z.ToString().PadLeft(digits)); //Print found number
					Console.WriteLine();
					Console.Write(z.ToString().PadLeft(digits)); //Print search-ongoing number
				} else {
					//Frequency is an estimate for how often the display should be updated,
					//to keep it relevant without causing additional lag
					//speed*2^(-z/1000) gives a function that starts at speed, and halves every 1000 units.
					//This is then shifted up by 1, so it approaches 1 instead of 0
					int frequency = (int)Math.Floor(speed * Math.Pow(2, -(double)z/(double)1000)) + 1;
			
					if (z % frequency == 0 || z == maxZ) //Update every [frequency] cycles, and on last cycle of loop
					{
						Console.Write(deleteLastNumberStr);
						Console.Write(z.ToString().PadLeft(digits));
					}
				}
			}
	
			Console.Write(deleteLastNumberStr);
			Console.WriteLine("Done from 1 to " + maxZ + ".");
		}

		public static void PrintNonTrivialZeros_twoThreeDecisions(int maxZ)
		{
			int digits = maxZ > 1000000 ? 0 : maxZ.ToString().Length + 1; //Add 1 to be safe
	
			foreach (long nonTrivialZero in TwoThreeDecisionZFinder.GetNonTrivialZeros(max: maxZ))
			{
				Console.WriteLine(nonTrivialZero.ToString().PadLeft(digits));
			}
	
			Console.WriteLine("Done from 1 to " + maxZ + ".");
		}

		public static void PrintExpansionCounts(BigInteger maxZ, bool toFile)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			const char backspaceChar = '\u0008';
	
			Action<ExpansionTerm> currentPathStartingPointPrinter;
			if (toFile) {
				currentPathStartingPointPrinter = null;		
			} else {		
				currentPathStartingPointPrinter = pathStartingPoint => {
					Console.Write(">" + pathStartingPoint.Evaluate().ToString().PadLeft(digits) + "...");
					Console.Write(new string(backspaceChar, 1 + digits + 3)); //Move back but don't actually clear - it will be overwritten
				};
			}
	
			foreach (var expList in OptionsMatrixZFinder.IterateExpansionCounts(maxZ, onlyNonTrivialZeros: false, currentPathStartingPointPrinter: currentPathStartingPointPrinter)) {
				Console.WriteLine(
					expList.Key.ToString().PadLeft(digits) + ": "
					+ expList.Value.ToString().PadLeft(digits)
				);
			}
		}

		public static void PrintExpansionCounts_array(int maxZ, bool toFile, bool printInChunks)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			const char backspaceChar = '\u0008';
	
			Action<ExpansionTerm> currentPathStartingPointPrinter;
			if (toFile) {
				currentPathStartingPointPrinter = null;		
			} else {		
				currentPathStartingPointPrinter = pathStartingPoint => {
					Console.Write(">" + pathStartingPoint.Evaluate().ToString().PadLeft(digits) + "...");
					Console.Write(new string(backspaceChar, 1 + digits + 3)); //Move back but don't actually clear - it will be overwritten
				};
			}
	
			StringBuilder sb = null;
			if (printInChunks) {
				sb = new StringBuilder(capacity: 8192);
			}
	
			foreach (var expList in OptionsMatrixZFinder.IterateExpansionCounts_array(maxZ, onlyNonTrivialZeros: false, currentPathStartingPointPrinter: currentPathStartingPointPrinter)) {
				if (printInChunks) {
					sb.Append(expList.Key.ToString().PadLeft(digits));
					sb.Append(": ");
					sb.AppendLine(expList.Value.ToString().PadLeft(digits));
			
					if (sb.Length > 8000) {
						Console.Write(sb.ToString());
						sb.Clear();
					}
				} else {
					Console.WriteLine(
						expList.Key.ToString().PadLeft(digits) + ": "
						+ expList.Value.ToString().PadLeft(digits)
					);
				}
			}
	
			if (printInChunks && sb.Length > 0) {
				Console.Write(sb.ToString());
			}
		}

		public static void PrintExpansionCounts_arrayAsync(int maxZ, bool printInChunks)
		{
			int digits = maxZ > 1000000 ? 0 : maxZ.ToString().Length + 1; //Add 1 to be safe
	
			StringBuilder sb = new StringBuilder(capacity: printInChunks ? 8192 : digits*2 + 3);
	
			foreach (var expList in OptionsMatrixZFinder.IterateExpansionCounts_arrayAsync(maxZ))
			{
				sb.Append(expList.Key.ToString().PadLeft(digits));
				sb.Append(":");
				sb.AppendLine(expList.Value.ToString().PadLeft(digits));
		
				if (sb.Length > 8000 || !printInChunks) {
					Console.Write(sb.ToString());
					sb.Clear();
				}
			}
	
			Console.Write(sb.ToString());
		}

		public static void PrintExpansionCounts_twoThreeDecisions(int maxZ)
		{
			int digits = maxZ > 1000000 ? 0 : maxZ.ToString().Length + 1; //Add 1 to be safe
	
			var monitorer = new System.Threading.Timer(
				callback: (e) => {
					Console.Error.Write(TwoThreeDecisionTracker.GlobalRepeatedDoublingOpsDone + " ");
				},
				state: null,
				dueTime: TimeSpan.Zero,
				period: TimeSpan.FromSeconds(10)
			);
	
			var expansionCounts = (
				maxZ < 100000001
				? TwoThreeDecisionZFinder.GetExpansionCounts_twoThreeDecisions(maxZ).AsEnumerable()
				: TwoThreeDecisionZFinder.GetExpansionCounts_twoThreeDecisions_chunkedArray(maxZ).AsEnumerable()
			);
	
			monitorer.Dispose();
	
			StringBuilder sb = new StringBuilder(capacity: 8192);
	
			if (maxZ < 10000000)
			{		
				int i = 1;
				foreach (int expCount in expansionCounts.Skip(1)) {
					sb.Append(i.ToString().PadLeft(digits));
					sb.Append(":");
					sb.AppendLine(expCount.ToString().PadLeft(digits));
			
					if (sb.Length > 8000) {
						Console.Write(sb.ToString());
						sb.Clear();
					}
			
					i++;
				}
			}
			else
			{
				foreach (int expCount in expansionCounts.Skip(1)) {
					sb.AppendLine(expCount.ToString());
			
					if (sb.Length > 8000) {
						Console.Write(sb.ToString());
						sb.Clear();
					}
				}
			}
	
			//	int[] expansionCounts = GetExpansionCounts_binaryDecisions(maxZ);
			//	
			//	StringBuilder sb = new StringBuilder(capacity: 8192);
			//	
			//	for (int i = 1; i < expansionCounts.Length; i++) {
			//		sb.Append(i.ToString().PadLeft(digits));
			//		sb.Append(":");
			//		sb.AppendLine(expansionCounts[i].ToString().PadLeft(digits));
			//		//sb.Append(i);
			//		//sb.Append(":");
			//		//sb.Append(expansionCounts[i]);
			//		//sb.AppendLine();
			//		
			//		if (sb.Length > 8000) {
			//			Console.Write(sb.ToString());
			//			sb.Clear();
			//		}
			//	}
	
			Console.Write(sb.ToString());
		}

		public static void PrintExpansions(BigInteger maxZ, Expansion.Format expansionFormat = Expansion.Format.FullFormula, bool includeSpaces = false)
		{
			int digits = maxZ.ToString().Length + 1; //Add 1 to be safe
	
			foreach (var expList in OptionsMatrixZFinder.IterateExpansionLists(maxZ)) {
				PrintExpansionList(
					expList.Value,
					z: expList.Key,
					digits: digits,
					expansionFormat: expansionFormat,
					includeSpaces: includeSpaces
				);
			}
		}

		public static void PrintExpansionList(List<Expansion> expansionList, BigInteger z, int digits, Expansion.Format expansionFormat = Expansion.Format.FullFormula, bool includeSpaces = false)
		{
			Console.WriteLine(
				z.ToString().PadLeft(digits) + ","
				+ expansionList.Count.ToString().PadLeft(digits) + ","
				+ string.Join(
					",",
					expansionList.Select(exp => "=" + exp.ToString(expansionFormat, includeSpaces))
				)
			);
		}

		public static void PrintSpeedTests<T>(Action<T> action, IEnumerable<T> inputs, int trials, int simulatedIterationsPerTrial, TimeSpan targetTrialTime)
		{
			foreach (var kvp in SpeedTests.SpeedTestAscendingInputs<T>(action, inputs, trials, simulatedIterationsPerTrial, targetTrialTime, new Stopwatch())) {
				Console.WriteLine(
					kvp.Key + " "
					+ kvp.Value.Select(x => x.TotalMilliseconds).Average() + " "
					+ kvp.Value.Select(x => x.TotalMilliseconds).StdDevSample()
				);
			}
		}

		public static void PrintBinaryDecisionZMSFinderResults(int maxZ, int maxMultiplier, int maxSummand)
		{
			for (int m = 0; m <= maxMultiplier; m++)
			{
				for (int s = 0; s <= maxSummand; s++)
				{
					Console.WriteLine(
						"multiplier: " + m + ", summand: " + s + Environment.NewLine
						+ string.Join(
							Environment.NewLine,
							BinaryDecisionZMSFinder.GetExpansionCounts(max: maxZ, multiplier: m, summand: s)
							.Select((x, i) => i + ":" + x)
						)
					);
				}
			}
		}
	}
}

//*/
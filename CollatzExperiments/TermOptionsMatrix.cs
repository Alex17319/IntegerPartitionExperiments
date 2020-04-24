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
using System.Numerics;

namespace CollatzExperiments
{
	//For description purposes, the contents of this class should be through of as written something like:
	// Given m = Maximum,
	//		2^0	2^1	2^2	2^3	2^4
	//	3^0	a	b	c	d   >m
	//	3^1	e	f	g	h   >m
	//	3^2	i	j   >m	>m
	//	3^3	k   >m
	//	3^4	>m
	public class TermOptionsMatrix
	{
		public ReadOnlyCollection<ReadOnlyCollection<ExpansionTerm>> TermOptions { get; }
		public BigInteger Maximum { get; }
	
		public TermOptionsMatrix(BigInteger maximum) {
			this.Maximum = maximum;
			this.TermOptions = GetExpansionTermOptions(maximum);
		}
	
		public int RowsCount => TermOptions.Count;
		public int ColumnsCount(int threeExponent) => TermOptions[threeExponent].Count;
	
		public ReadOnlyCollection<ExpansionTerm> this[int threeExponent] => GetRow(threeExponent);
		public ExpansionTerm this[int threeExponent, int twoExponent] => GetRow(threeExponent)[twoExponent];
	
		public ReadOnlyCollection<ExpansionTerm> GetRow(int threeExponent) => TermOptions[threeExponent];
		public IEnumerable<ExpansionTerm> GetColumn(int twoExponent) => TermOptions.TakeWhile(row => row.Count > twoExponent).Select(row => row[twoExponent]);
	
		public bool Contains(ExpansionTerm option)
			=> option.ThreeExponent < TermOptions.Count
			&& option.TwoExponent < TermOptions[option.ThreeExponent].Count;
	
		public static BigInteger SumColumnTo(ExpansionTerm option) => MathUtils.BigIntTwoToThe  (option.TwoExponent  ) * MathUtils.BigIntSumPowersOfThreeTo(option.ThreeExponent);
		public static BigInteger SumRowTo   (ExpansionTerm option) => MathUtils.BigIntThreeToThe(option.ThreeExponent) * MathUtils.BigIntSumPowersOfTwoTo  (option.TwoExponent  );
	
		public static BigInteger MinimumPathSum(ExpansionTerm startingPoint) => SumColumnTo(startingPoint);
	
		public bool RowHasItemsAfter(ExpansionTerm option)
			=> option.ThreeExponent < TermOptions.Count //If the row isn't inside the matrix, it definitely doesn't have more items
			&& option.TwoExponent + 1 < TermOptions[option.ThreeExponent].Count; //If the next item is within the row, return true
	
		public bool ColumnHasItemsAfter(ExpansionTerm option)
			=> option.ThreeExponent + 1 < TermOptions.Count //If the entire next row isn't inside the matrix, the column definitely doesn't have more items
			&& TermOptions[option.ThreeExponent + 1].Count > option.TwoExponent; //If the next row is long enough to include the relevant column, return true
	
		public bool TryGetNextInRow(ExpansionTerm option, out ExpansionTerm next) {
			if (RowHasItemsAfter(option)) {
				next = option.StepBy(threeExpStep: 0, twoExpStep: 1);
				return true;
			} else {
				next = default(ExpansionTerm);
				return false;
			}
		}
	
		public bool TryGetNextInColumn(ExpansionTerm option, out ExpansionTerm next) {
			if (ColumnHasItemsAfter(option)) {
				next = option.StepBy(threeExpStep: 1, twoExpStep: 0);
				return true;
			} else {
				next = default(ExpansionTerm);
				return false;
			}
		}
	
		public IEnumerable<Expansion> EnumerateExpansionPaths(ExpansionTerm pathStartingPoint, bool capToMaximum)
		{
			if (!this.Contains(pathStartingPoint)) {
				throw new ArgumentException(
					"The " + nameof(TermOptionsMatrix) + " '" + this + "' "
					+ "does not contain provided " + nameof(pathStartingPoint) + " '" + pathStartingPoint + "'."
				);
			}
		
			var stack = new ExpansionTermStack();
			stack.Push(pathStartingPoint);
		
			//To start with, keep moving up, adding each element to the stack, until the top row is reached
			while (stack.Peek().ThreeExponent > 0) {
				var termAbove = stack.Peek().StepBy(threeExpStep: -1, twoExpStep: 0);
				stack.Push(termAbove);
			}
		
			//Then check if that resulted in a path that isn't too big
			if (!capToMaximum || stack.Sum <= this.Maximum) {
				yield return new Expansion(stack.ToList().AsReadOnly());
			} else {
				yield break; //The stack currently holds the lowest possible path; if that doesn't fit in Maximum, no paths will
			}
		
			while (true)
			{
				if (DoneEnumeratingExpansionPaths(stack)) yield break;
			
				ExpansionTerm termToRight;
				if (this.TryGetNextInRow(stack.Peek(), out termToRight) && (!capToMaximum || stack.SumWithAlternateHead(termToRight) <= this.Maximum))
				{
					stack.Pop();
					stack.Push(termToRight);
				
					if (capToMaximum && stack.SumWithoutHead + MinimumPathSum(stack.Peek()) > this.Maximum) {
						//If true, the shortest path from the current position is too big. As moving to the right always
						//results in a higher term, all other paths from the current position, and from all positions to the right
						//of the current one, will also be too big. This is only true given the *current* path stored in the stack,
						//so to try different paths, pop() to move back down (and probably to the left) by one, and then loop around
						//to try moving to the right from there
						stack.Pop();
						continue;
					}
					else
					{
						//The first route, which goes straight to the top, is the path with the minimum
						//sum, which we've already checked is be less than this.Maximum, so take that route
						while (stack.Peek().ThreeExponent > 0) {
							stack.Push(stack.Peek().StepBy(threeExpStep: -1, twoExpStep: 0));
						}
					
						//	Console.WriteLine("#12: " + stack.Peek() + ", " + stack.SumWithoutHead + ", " + MinimumPathSum(stack.Peek()));
						yield return new Expansion(stack.ToList().AsReadOnly());
					
						//Now loop around again to try moving to the right
						continue;
					}
				}
				else
				{
					//Can't move to the right, so move back down by one instead & loop around to try again
					stack.Pop();
					continue;
				}
			}
		}
	
		private bool DoneEnumeratingExpansionPaths(ExpansionTermStack stack) {
			//If there's only one item left on the stack, we're back at the starting point
			//(or the starting point is in the top row, and we never moved anywhere).
			//Either way, we are therefore done.
			return stack.Count <= 1;
		}
	
		public IEnumerable<BigInteger> EnumerateSummedExpansionPaths(ExpansionTerm pathStartingPoint, bool capToMaximum)
		{
			if (!this.Contains(pathStartingPoint)) {
				throw new ArgumentException(
					"The " + nameof(TermOptionsMatrix) + " '" + this + "' "
					+ "does not contain provided " + nameof(pathStartingPoint) + " '" + pathStartingPoint + "'."
				);
			}
		
			var stack = new ExpansionTermStack();
			stack.Push(pathStartingPoint);
		
			//To start with, keep moving up, adding each element to the stack, until the top row is reached
			while (stack.Peek().ThreeExponent > 0) {
				var termAbove = stack.Peek().StepBy(threeExpStep: -1, twoExpStep: 0);
				stack.Push(termAbove);
			}
		
			//Then check if that resulted in a path that isn't too big
			if (!capToMaximum || stack.Sum <= this.Maximum) {
				yield return stack.Sum;
			} else {
				yield break; //The stack currently holds the lowest possible path; if that doesn't fit in Maximum, no paths will
			}
		
			while (true)
			{
				if (DoneEnumeratingSummedExpansionPaths(stack)) yield break;
			
				ExpansionTerm termToRight;
				if (this.TryGetNextInRow(stack.Peek(), out termToRight) && (!capToMaximum || stack.SumWithAlternateHead(termToRight) <= this.Maximum))
				{
					stack.Pop();
					stack.Push(termToRight);
				
					if (capToMaximum && stack.SumWithoutHead + MinimumPathSum(stack.Peek()) > this.Maximum) {
						//If true, the shortest path from the current position is too big. As moving to the right always
						//results in a higher term, all other paths from the current position, and from all positions to the right
						//of the current one, will also be too big. This is only true given the *current* path stored in the stack,
						//so to try different paths, pop() to move back down (and probably to the left) by one, and then loop around
						//to try moving to the right from there
						stack.Pop();
						continue;
					}
					else
					{
						//The first route, which goes straight to the top, is the path with the minimum
						//sum, which we've already checked is be less than this.Maximum, so take that route
						while (stack.Peek().ThreeExponent > 0) {
							stack.Push(stack.Peek().StepBy(threeExpStep: -1, twoExpStep: 0));
						}
					
						//	Console.WriteLine("#12: " + stack.Peek() + ", " + stack.SumWithoutHead + ", " + MinimumPathSum(stack.Peek()));
						yield return stack.Sum;
					
						//Now loop around again to try moving to the right
						continue;
					}
				}
				else
				{
					//Can't move to the right, so move back down by one instead & loop around to try again
					stack.Pop();
					continue;
				}
			}
		}
	
		private bool DoneEnumeratingSummedExpansionPaths(ExpansionTermStack stack) {
			//If there's only one item left on the stack, we're back at the starting point
			//(or the starting point is in the top row, and we never moved anywhere).
			//Either way, we are therefore done.
			return stack.Count <= 1;
		}
	
		public IEnumerable<int> EnumerateIntSummedExpansionPaths(ExpansionTerm pathStartingPoint) //capToMaximum = true
		{
			if (this.Maximum > int.MaxValue) throw new InvalidOperationException(
				"Maximum is greater than int.MaxValue, so not all summed paths would be possible to return."
			);
		
			if (!this.Contains(pathStartingPoint)) throw new ArgumentException(
				"The " + nameof(TermOptionsMatrix) + " '" + this + "' "
				+ "does not contain provided " + nameof(pathStartingPoint) + " '" + pathStartingPoint + "'."
			);
		
			var stack = new LongExpansionTermStack();
			stack.Push(pathStartingPoint);
		
			//To start with, keep moving up, adding each element to the stack, until the top row is reached
			while (stack.Peek().ThreeExponent > 0) {
				var termAbove = stack.Peek().StepBy(threeExpStep: -1, twoExpStep: 0);
				stack.Push(termAbove);
			}
		
			//Then check if that resulted in a path that isn't too big
			if (stack.Sum <= this.Maximum) {
				yield return (int)stack.Sum; //stack.Sum <= this.Maximum <= int.MaxValue
			} else {
				yield break; //The stack currently holds the lowest possible path; if that doesn't fit in Maximum, no paths will
			}
		
			ExpansionTerm currentTerm = stack.Pop();
		
			while (true)
			{
				//If there's no items left on the stack, then the current item (which isn't on the stack)
				//is all that's left, and we're back at the starting point (or the starting point is in
				//the top row, and we never moved anywhere). Either way, we are therefore done.
				if (stack.Count <= 0) yield break;
			
				ExpansionTerm termToRight = currentTerm.StepBy(threeExpStep: 0, twoExpStep: 1);
				// ^ But this might not actually be in the matrix (ie. less than or equal to this.Maximum) so need to check that next.
				//   But then, we don't actually need to check that, as we're already need to check if it, plus the stack's sum,
				//   is less than or equal to this.Maximum - so just need to do that check.
				//   But in fact, we also then need to check whether all of that, plus the lowest path from the new term,
				//   is less than or equal to this.Maximum...so just do *that* check to start with.
			
				if (stack.Sum + MinimumPathSum(termToRight) <= this.Maximum)
				{
					//The first route from termToRight, which goes straight to the top, is the path with the
					//minimum sum, which we've already checked is be less than this.Maximum, so take that route
					//(first pushing termToRight). However, don't push the final item (hence "> 0"), just add
					//that onto the sum manually - as it'd just be guaranteed to be popped straight after
					//anyway, so there's no point.
					for (int i = termToRight.ThreeExponent; i > 0; i--) {
						stack.Push(new ExpansionTerm(threeExponent: i, twoExponent: termToRight.TwoExponent));
					}
				
					//Set up currentTerm for next iteration (and for yield statement)
					currentTerm = new ExpansionTerm(threeExponent: 0, twoExponent: termToRight.TwoExponent);
				
					yield return (int)(stack.Sum) + currentTerm.EvaluateInt();
					// ^ Only need the TwoExponent as the ThreeExponent is zero so it's just "1 * 2^whatever"
					//   Also, ok to cast as (stack.Sum + currentTerm.TwoExponent <= this.Maximum <= int.MaxValue) (based on prior checks)
				
					//Now loop around again to try moving to the right again
					continue;
				}
				else
				{
					//If false, then either the term to the right is itself bigger than this.Maximum (so it isn't inside the matrix);
					//or it, plus the current stack's sum, is bigger than this.Maximum (so adding anything to it will always be outside
					//the range of desired results); or finally it, plus the current stack's sum, plus the sum of the smallest path
					//continuing from it, is bigger than this.Maximum. This last case means that all paths from it, and from all positions
					//further to the right, will be too big (but this position may be valid, when reached from a different stack, that
					//goes to the left more). In all three cases, we need move back down from the current term - which we've already done
					//by popping earlier - and then loop back around to continue trying from there. What we do need to do, though, is set
					//up currentTerm ready for the next loop, by popping the top value into it.
					currentTerm = stack.Pop();
					continue;
				}
			}
		
			//	while (true)
			//	{
			//		//If there's only one item left on the stack, we're back at the starting point
			//		//(or the starting point is in the top row, and we never moved anywhere).
			//		//Either way, we are therefore done.
			//		if (stack.Count <= 1) yield break;
			//		
			//		ExpansionTerm currentTerm = stack.Pop();
			//		ExpansionTerm termToRight = currentTerm.StepBy(threeExpStep: 0, twoExpStep: 1);
			//		// ^ But this might not actually be in the matrix, ie. no greater than this.Maximum, so need to check that next.
			//		//   But then, we don't actually need to check that, as we're already need to check if it, plus the stack's sum,
			//		//   is no greater than this.Maximum - so just need to do that check.
			//		//   But in fact, we also then need to check whether all of that, plus the lowest path from the new term,
			//		//   is no greater than this.Maximum...so just do *that* check to start with.
			//		
			//		//long termToRightEval = termToRight.EvaluateLong();
			//		//if (this.TryGetNextInRow(currentTerm, out termToRight) && stack.SumWithAlternateHead(termToRight) <= this.Maximum)
			//		//if (termToRightEval <= this.Maximum && termToRightEval + stack.Sum <= this.Maximum)
			//		//if (termToRightEval + stack.Sum <= this.Maximum)
			//		if (stack.Sum + MinimumPathSum(termToRight) <= this.Maximum)
			//		{
			//			//The first route from termToRight, which goes straight to the top, is the path with
			//			//the minimum sum, which we've already checked is be less than this.Maximum, so take
			//			//that route (first pushing termToRight)
			//			for (int i = termToRight.ThreeExponent; i >= 0; i--) {
			//				stack.Push(new ExpansionTerm(threeExponent: i, twoExponent: termToRight.TwoExponent));
			//			}
			//			
			//			//	Console.WriteLine("#12: " + stack.Peek() + ", " + stack.SumWithoutHead + ", " + MinimumPathSum(stack.Peek()));
			//			yield return (int)stack.Sum; //Safe to cast based on prior checks (remember this.Maximum < int.MaxValue)
			//			
			//			//Now loop around again to try moving to the right
			//			continue;
			//		}
			//		else
			//		{
			//			//If false, then either the term to the right is itself bigger than this.Maximum (so it isn't inside the matrix);
			//			//or it, plus the current stack's sum, is bigger than this.Maximum (so adding anything to it will always be outside
			//			//the range of desired results); or finally it, plus the current stack's sum, plus the sum of the smallest path
			//			//continuing from it, is bigger than this.Maximum. This last case means that all paths from it, and from all positions
			//			//further to the right, will be too big (but this position may be valid, when reached from a different stack, that
			//			//goes to the left more). In all three cases, we need move back down from the current term - which we've already done
			//			//by popping earlier - and then loop back around to continue trying from there.
			//			continue;
			//		}
			//	}
		}
	
		private bool DoneEnumeratingIntSummedExpansionPaths(LongExpansionTermStack stack) {
			//If there's only one item left on the stack, we're back at the starting point
			//(or the starting point is in the top row, and we never moved anywhere).
			//Either way, we are therefore done.
			return stack.Count <= 1;
		}
	
		private static ReadOnlyCollection<ReadOnlyCollection<ExpansionTerm>> GetExpansionTermOptions(BigInteger z)
		{
			List<List<ExpansionTerm>> terms = new List<List<ExpansionTerm>>();
		
			var option = new ExpansionTerm(0, 0);
			while (option.Evaluate() <= z)
			{
				var optionsForTerm = new List<ExpansionTerm>();
			
				while (option.Evaluate() <= z) {
					optionsForTerm.Add(option);
					option = option.StepBy(0, 1);
				}
			
				terms.Add(optionsForTerm);
				option = new ExpansionTerm(option.ThreeExponent + 1, 0);
			}
		
			return terms.Select(row => row.AsReadOnly()).ToList().AsReadOnly();
		}
	}
}

//*/
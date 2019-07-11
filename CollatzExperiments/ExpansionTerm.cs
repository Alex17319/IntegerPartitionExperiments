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
	/// <summary>
	/// Note: Every (ThreeExponent, TwoExponent) pair results in a unique number when Evaluate() is called. See proof in remarks
	/// </summary>
	/// <remarks>
	/// Every ExpansionTerm with a unique (ThreeExponent, TwoExponent) pair has a unique result when Evaluate() is called.
	///	Proof:
	///		An evaluated ExpansionTerm is of the form 3^a * 2^b, where a and b are positive integers
	/// 	Therefore it is a product of 2*2*...*2 (repeated a times) and 3*3*...*3 (repeated b times)
	/// 	As 2 and 3 are prime, this is the prime factorisation of the evaluated ExpansionTerm
	/// 	Therefore this (unordered) sequence of 2s and 3s is the only one that multiplies out to the evaluated ExpansionTerm
	/// 	Therefore no other a and b can produce the same evaluated ExpansionTerm
	/// </remarks>
	public struct ExpansionTerm : IComparable<ExpansionTerm> //	: IExpansionTerm
	{
		public readonly int ThreeExponent;
		public readonly int TwoExponent;
	
		//	int IExpansionTerm.GetThreeExponent() => ThreeExponent;
		//	int IExpansionTerm.GetTwoExponent() => TwoExponent;
	
		public ExpansionTerm(int threeExponent, int twoExponent) {
			if (threeExponent < 0) throw new ArgumentOutOfRangeException(nameof(threeExponent), threeExponent, "Cannot be negative.");
			if (twoExponent < 0) throw new ArgumentOutOfRangeException(nameof(twoExponent), twoExponent, "Cannot be negative.");
			this.ThreeExponent = threeExponent;
			this.TwoExponent = twoExponent;
		}
	
		public BigInteger Evaluate() => EvaluateAt(threeExponent: ThreeExponent, twoExponent: TwoExponent);
		public static BigInteger EvaluateAt(int threeExponent, int twoExponent) => BigInteger.Pow(3, threeExponent) * BigInteger.Pow(2, twoExponent);
	
		public int EvaluateInt() => EvaluateIntAt(threeExponent: ThreeExponent, twoExponent: TwoExponent);
		public static int EvaluateIntAt(int threeExponent, int twoExponent) => MathUtils.IntThreeToThe(threeExponent) * MathUtils.IntTwoToThe(twoExponent);
	
		public long EvaluateLong() => EvaluateLongAt(threeExponent: ThreeExponent, twoExponent: TwoExponent);
		public static long EvaluateLongAt(int threeExponent, int twoExponent) => MathUtils.LongThreeToThe(threeExponent) * MathUtils.LongTwoToThe(twoExponent);
	
		public ExpansionTerm StepBy(int threeExpStep, int twoExpStep)
			=> new ExpansionTerm(this.ThreeExponent + threeExpStep, this.TwoExponent + twoExpStep);
	
		public static IEnumerable<ExpansionTerm> EnumerateThreeExponentsTo(ExpansionTerm last) {
			for (int i = 0; i <= last.ThreeExponent; i++) {
				yield return new ExpansionTerm(threeExponent: i, twoExponent: last.TwoExponent);
			}
		}
		public static IEnumerable<ExpansionTerm> EnumerateTwoExponentsTo(ExpansionTerm last) {
			for (int i = 0; i <= last.TwoExponent; i++) {
				yield return new ExpansionTerm(threeExponent: last.ThreeExponent, twoExponent: i);
			}
		}
	
		public IEnumerable<ExpansionTerm> EnumerateStepsUpTo(int threeExpStep, int twoExpStep, BigInteger max) {
			var term = this;
			while (term.Evaluate() <= max) {
				yield return term;
				term = term.StepBy(threeExpStep: threeExpStep, twoExpStep: twoExpStep);
			}
		}
	
		public override string ToString() => this.ToString(Format.FullFormula, includeSpaces: false);
		public string ToString(bool includeSpaces) => this.ToString(Format.FullFormula, includeSpaces);
		public string ToString(Format format, bool includeSpaces = true)
		{
			string sp = includeSpaces ? " " : "";
			switch (format) {
				case Format.FullFormula: return "3^" + ThreeExponent + sp + "*" + sp + "2^" + TwoExponent;
				case Format.Evaluate: return Evaluate().ToString();
				case Format.Json: return (
					"{[" + nameof(ExpansionTerm) + "]" + sp
					+ nameof(ThreeExponent) + ":" + sp + ThreeExponent + ","
					+ sp + nameof(TwoExponent) + ":" + sp + TwoExponent + "}"
				);
				case Format.ThreeTwoCoordinate: return "(" + ThreeExponent + "," + sp + TwoExponent + ")";
				default: throw new ArgumentException("Invalid enum value '" + format + "'.", nameof(format));
			}
		}
		public enum Format { FullFormula, Evaluate, Json, ThreeTwoCoordinate }
	
	
		public static bool Equals(ExpansionTerm a, ExpansionTerm b) => a.ThreeExponent == b.ThreeExponent && a.TwoExponent == b.TwoExponent;
		public override bool Equals(object other) {
			var exTerm = other as ExpansionTerm?;
			return exTerm != null && Equals(this, exTerm.GetValueOrDefault());
		}
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = hash * 23 + this.ThreeExponent.GetHashCode();
				hash = hash * 23 + this.TwoExponent.GetHashCode();
				return hash;
			}
		}
	
		public int CompareTo(ExpansionTerm other) => this.Evaluate().CompareTo(other.Evaluate());
	}
}

//*/
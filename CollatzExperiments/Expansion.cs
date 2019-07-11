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
	public class Expansion
	{
		public ReadOnlyCollection<ExpansionTerm> Terms { get; }
	
		public Expansion(ReadOnlyCollection<ExpansionTerm> terms) {
			if (terms == null) throw new ArgumentNullException(nameof(terms));
			this.Terms = terms;
		}
	
		public BigInteger Sum { //Avoid using linq wherever possible, for efficiency & memory use (not really premature)
			get {
				BigInteger sum = 0;
				for (int i = 0; i < this.Terms.Count; i++) {
					sum += this.Terms[i].Evaluate();
				}
				return sum;
			}
		}
	
		public override string ToString() => this.ToString(Format.FullFormula, true);
		public string ToString(bool includeSpaces) => this.ToString(Format.FullFormula, includeSpaces);
		public string ToString(Format format, bool includeSpaces = true)
		{
			string sp = includeSpaces ? " " : "";
			switch (format) {
				case Format.FullFormula: return string.Join(sp + "+" + sp, Terms.Select(t => t.ToString(ExpansionTerm.Format.FullFormula, includeSpaces)));
				case Format.EvaluateTerms: return string.Join(sp + "+" + sp, Terms.Select(t => t.Evaluate()));
				case Format.EvaluateFully: return Terms.Select(t => t.Evaluate()).Aggregate(BigInteger.Add).ToString();
				case Format.Json: return (
					"{[" + nameof(Expansion) + "]" + sp
					+ "Terms:" + sp + "[" + string.Join("," + sp, Terms.Select(
						t => t.ToString(ExpansionTerm.Format.Json, includeSpaces)
					)) + "]}"
				);
				case Format.ThreeTwoCoordinates: return string.Join(
					"," + sp,
					Terms.Select(t => t.ToString(ExpansionTerm.Format.ThreeTwoCoordinate, includeSpaces))
				);
				case Format.TwoExponentsList: return string.Join("," + sp, Terms.Select(t => t.TwoExponent));
				default: throw new ArgumentException("Invalid enum value '" + format + "'.", nameof(format));
			}
		}
		public enum Format { FullFormula, EvaluateTerms, EvaluateFully, Json, ThreeTwoCoordinates, TwoExponentsList }
	}
}

//*/
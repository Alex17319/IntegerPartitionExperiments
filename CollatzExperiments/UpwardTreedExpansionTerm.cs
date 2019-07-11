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
	//TODO: Maybe have another go at this (or some similar sub-sum-storing strategy) later. Need to fix
	//the problem where it would need to be re-run for every different starting point (it doesn't handle
	//lower-down starting points), which is a bit of a waste of the potential performance improvement
	//this strategy provides. But it's still probably pretty high memory, so try other ideas first.
	//	class UpwardTreedExpansionTerm {
	//		public readonly ExpansionTerm Term;
	//		public readonly int Max;
	//		public readonly ReadOnlyCollection<UpwardTreedExpansionTerm> SubTerms;
	//		public readonly ReadOnlyCollection<int> SubSumCounts;
	//		
	//		public UpwardTreedExpansionTerm(ExpansionTerm term, int max) {
	//			this.Term = term;
	//			this.Max = max;
	//			
	//			var subTerms = Array.AsReadOnly(new UpwardTreedExpansionTerm[0]);
	//			int subSumMax = max - (
	//				new ExpansionTerm(0, 0)
	//				.EnumerateStepsUpTo(threeExpStep: 1, twoExpStep: 0, max)
	//				.Sum(x => x.Evaluate())
	//			);
	//			for (int i = 0; i < term.ThreeExponent; i++) {
	//				subTerms = (
	//					new ExpansionTerm(threeExponent: i, twoExponent: 0)
	//					.EnumerateStepsUpTo(threeExpStep: 0, twoExpStep: 1, max: max)
	//					.Select(
	//						t => new UpwardTreedExpansionTerm(t, max, subSumMax, subTerms)
	//					)
	//					.ToList()
	//					.AsReadOnly()
	//				);
	//				subSumMax += new ExpansionTerm(threeExponent: i, twoExponent: 0).Evaluate();
	//			}
	//			this.SubTerms = subTerms;
	//			this.SubSumCounts = null;
	//			
	//			//this.SubTerms = new ExpansionTerm(0, 0).EnumerateStepsUpTo(
	//			//	threeExpStep: 0,
	//			//	twoExpStep: 1,
	//			//	max: max
	//			//).Select(t => new UpwardTreedExpansionTerm(t, max, parent: this));
	//		}
	//		
	//		private UpwardTreedExpansionTerm(ExpansionTerm term, int max, int subSumMax, ReadOnlyCollection<UpwardTreedExpansionTerm> subTerms) {
	//			this.Term = term;
	//			this.Max = max;
	//			this.SubTerms = subTerms;
	//			
	//			if (this.SubTerms.Count == 0) {
	//				var subSumCounts = new int[max];
	//				subSumCounts[this.Term.Evaluate()] = 1;
	//				this.SubSumCounts = Array.AsReadOnly(subSumCounts);
	//			} else {
	//				for (int i = 0; i < this.SubTerms.Count; i++) {
	//					for (int j = 0; j < t.SubSumCounts.Length; j++) {
	//						if 
	//					}
	//				}
	//			}
	//		}
	//	}
}

//*/
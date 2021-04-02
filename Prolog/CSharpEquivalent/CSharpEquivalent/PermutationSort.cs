using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CSharpEquivalent
{
	public static class PermutationSort
	{
		public static bool ordered(ImmutableStack<int> list)
			=> list.IsEmpty
			|| !list.IsEmpty
			&& list.Pop().IsEmpty
			|| !list.IsEmpty
			&& !list.Pop().IsEmpty
			&& Logic.Succeed(out var t1, list.Pop(out var headA))
			&& Logic.Succeed(out var t2, t1.Pop(out var headB))
			&& headA <= headB
			&& ordered(t2.Push(headB));

		public static bool insert<T>(ImmutableStack<T> without, T element, out IEnumerable<ImmutableStack<T>> with)
			=> Enumerable.Empty<ImmutableStack<T>>()
			.AppendIf(
				without.IsEmpty
				&& Logic.Succeed(out var soln_1, ImmutableStack.Create(element))
				|| Logic.Fail(out soln_1),
				soln_1
			)
			.AppendIf(
				!without.IsEmpty
				&& Logic.Succeed(out var soln_2, without.Push(element))
				|| Logic.Fail(out soln_2),
				soln_2
			)
			.ConcatIf(
				!without.IsEmpty
				&& Logic.Succeed(out var tail, without.Pop(out var head))
				&& insert(tail, element, out var tailSolutions)
				&& Logic.Succeed(out var soln_3s, tailSolutions.Select(x => x.Push(head)))
				|| Logic.Fail(out soln_3s),
				soln_3s
			)
			.SucceedIfAny(out with);

		public static bool insert<T>(ImmutableStack<T> with, out IEnumerable<(ImmutableStack<T> without, T element)> solutions)
			=> Enumerable.Empty<(ImmutableStack<T> without, T element)>()
			.AppendIf(
				!with.IsEmpty
				&& Logic.Succeed(out var tail_1, with.Pop(out var head_1))
				&& tail_1.IsEmpty
				&& Logic.Succeed(out var soln_1, (without: tail_1, element: head_1))
				|| Logic.Fail(out soln_1),
				soln_1
			)
			.AppendIf(
				!with.IsEmpty
				&& Logic.Succeed(out var tail_2, with.Pop(out var head_2))
				&& !tail_2.IsEmpty
				&& Logic.Succeed(out var soln_2, (without: tail_2, element: head_2))
				|| Logic.Fail(out soln_2),
				soln_2
			)
			.ConcatIf(
				!with.IsEmpty
				&& Logic.Succeed(out var tail_3, with.Pop(out var head_3))
				&& !tail_3.IsEmpty
				&& insert(with: tail_3, solutions: out var tailSolutions)
				&& Logic.Succeed(out var soln_3s, tailSolutions.Select(x => (without: x.without.Push(head_3), x.element)))
				|| Logic.Fail(out soln_3s),
				soln_3s
			)
			.SucceedIfAny(out solutions);

		public static bool permutation<T>(ImmutableStack<T> original, out IEnumerable<ImmutableStack<T>> permuted)
			=> Enumerable.Empty<ImmutableStack<T>>()
			.AppendIf(
				original.IsEmpty
				&& Logic.Succeed(out var soln_1, original)
				|| Logic.Fail(out soln_1),
				soln_1
			)
			.ConcatIf(
				!original.IsEmpty
				&& insert(with: original, out var removedSolns)
				&& Logic.Succeed(
					out var soln_2s_,
					removedSolns.SelectManyIf(
						x => (
							permutation(x.without, out var withoutPermutations)
							&& Logic.Succeed(out var soln_2s, withoutPermutations.Select(
								y => y.Push(x.element)
							))
							|| Logic.Fail(out soln_2s),
							soln_2s
						)
					)
				)
				|| Logic.Fail(out soln_2s_),
				soln_2s_
			)
			.SucceedIfAny(out permuted);

		public static bool psort(ImmutableStack<int> list, out IEnumerable<ImmutableStack<int>> sorted)
			=> permutation(list, out var permutations)
			&& permutations.SelectIf(
				x => (
					ordered(x),
					x
				)
			).SucceedIfAny(out sorted)
			|| Logic.Fail(out sorted);
	}
}

//*/
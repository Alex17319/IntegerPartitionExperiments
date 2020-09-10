% Can a given number be reached by:
% - Start at 1
% - Double zero or more times, then add 3
% - Double zero or more times, then add 9
% - Double zero or more times, then add 27
% - Continue doubling and adding further powers of 3
% - Stop at any point

reachable(Num) :-
	integer(Num), Num > 0,
	% MaxPow is ceiling(log(Num)/log(3)),
	% between(0, MaxPow, LastAddedPow),
	% lastAddedPowCandidate(Num, LastAddedPow),
	% reachable(Num, LastAddedPow)
	reachable(Num, LastAddedPow).

lastAddedPowCandidate(Num, LastPowCandidate) :-
	MaxPow is ceiling(log(Num)/log(3)),
	between(0, MaxPow, LastPowCandidate).

baseCandidate(Num, BaseCandidate) :-
	between(1, Num, BaseCandidate).

basePowCandidate(LastAddedPow, BasePowCandidate) :-
	between(0, LastAddedPow, BasePowCandidate).

% reachable(1, 0).
% reachable(Num, LastAddedPow) :-
% 	integer(Num), Num > 0,
% 	integer(LastAddedPow), LastAddedPow >= 0,
% 	Halved is Num/2,
% 	reachable(Halved, LastAddedPow).
% reachable(Num, LastAddedPow) :-
% 	integer(Num), Num > 0,
% 	integer(LastAddedPow), LastAddedPow >= 0,
% 	Subtracted is Num - (3 ^ LastAddedPow),
% 	PrevPow is LastAddedPow - 1,
% 	reachable(Subtracted, PrevPow).

reachable(Num, LastAddedPow) :-
	lastAddedPowCandidate(Num, LastAddedPow),
	reachableFrom(Num, LastAddedPow, 1, 0).

reachPath(Num, LastAddedPow, Path) :-
	lastAddedPowCandidate(Num, LastAddedPow),
	reachableFromVia(Num, LastAddedPow, 1, 0, Path).

% reachableFrom(Num, LastAddedPow, Start, StartPrevPow).
reachableFrom(Start, StartPrevPow, Start, StartPrevPow) :-
	integer(Start), Start > 0,
	integer(StartPrevPow), StartPrevPow >= 0.
reachableFrom(Num, LastAddedPow, Start, StartPrevPow) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	halvingStep(Num, Halved),
	reachableFrom(Halved, LastAddedPow, Start, StartPrevPow).
reachableFrom(Num, LastAddedPow, Start, StartPrevPow) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	subtractingStep(Num, LastAddedPow, Subtracted, PrevPow),
	reachableFrom(Subtracted, PrevPow, Start, StartPrevPow).

% reachableFromVia(Num, LastAddedPow, Start, StartPrevPow, Path).
reachableFromVia(Start, StartPrevPow, Start, StartPrevPow, [Start]) :-
	integer(Start), Start > 0,
	integer(StartPrevPow), StartPrevPow >= 0.
reachableFromVia(Num, LastAddedPow, Start, StartPrevPow, [Num|Path]) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	halvingStep(Num, Halved),
	reachableFromVia(Halved, LastAddedPow, Start, StartPrevPow, Path).
reachableFromVia(Num, LastAddedPow, Start, StartPrevPow, [Num|Path]) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	subtractingStep(Num, LastAddedPow, Subtracted, PrevPow),
	reachableFromVia(Subtracted, PrevPow, Start, StartPrevPow, Path).

inrange(Num, LastAddedPow, Start, StartPrevPow) :-
	integer(Start), Start > 0,
	integer(StartPrevPow), StartPrevPow >= 0,
	integer(Num), Num > 0,
	integer(LastAddedPow), LastAddedPow >= 0,
	Num > Start.

halvingStep(Num, NewNum) :-
	NewNum is Num / 2.

subtractingStep(Num, LastAddedPow, NewNum, NewLastAddedPow) :-
	NewNum is Num - (3 ^ LastAddedPow),
	NewLastAddedPow is LastAddedPow - 1.

% connection(Start, _, Start, [Start]).
% connection(Start, LastAddedPow, Dest, Path) :-

connected(A, B) :- connected(A, B, _, _).

connected(A, B, Base, BasePow) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	baseCandidate(A, Base),
	baseCandidate(B, Base),
	basePowCandidate(APow, BasePow),
	basePowCandidate(BPow, BasePow),
	reachableFrom(A, APow, Base, BasePow),
	reachableFrom(B, BPow, Base, BasePow).

connectedVia(A, B, Base, BasePow, Path) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	baseCandidate(A, Base),
	baseCandidate(B, Base),
	basePowCandidate(APow, BasePow),
	basePowCandidate(BPow, BasePow),
	reachableFromVia(A, APow, Base, BasePow, Path1),
	reachableFromVia(B, BPow, Base, BasePow, Path2),
	%append(Path1, Path2, Path).
	Path = [Path1, Path2].

scan(IncludeTrivial) :-
	between(1, infinite, Num),
	Mod3 is Num mod 3,
	% ((Mod3 \= 0), IncludeTrivial; true),
	(IncludeTrivial; Mod3 \= 0),
	write(Num),
	write(": "),
	testNum(Num),
	fail.
testNum(Num) :-
	write(Num),
	write(': '),
	Mod3 is Num mod 3,
	(Mod3 = 0, !, write('trivial'); write('non-trivial')),
	write(', '),
	(reachable(Num), !, write('reachable'); write("unreachable")),
	nl.
testNums([Num|Rest]) :-
	testNum(Num),
	testNums(Rest).

findZeros() :-
	between(1, infinite, Num),
	Mod3 is Num mod 3,
	Mod3 \= 0,
	testZero(Num).
testZero(Num) :-
	(reachable(Num), !, fail; write(Num), nl),
	fail.

findPaths(Num) :-
	write(Num),
	write(':'),
	nl,
	reachPath(Num, LastAddedPow, Path),
	reverse(Path, RPath),
	digits(Num, MaxDigits),
	writePath(RPath, MaxDigits),
	% write(' (last pow = 3^'),
	% write(LastAddedPow),
	% write(' = '),
	% EvalPow is 3 ^ LastAddedPow,
	% write(EvalPow),
	% write(')'),
	nl,
	fail.

findConnections(A, B) :-
	connected(A, B, Base, BasePow),
	write('Base: '),
	write(Base),
	write(', BasePow: '),
	write(BasePow),
	nl,
	fail.

writePath(Path) :-
	maxDigits(Path, MaxDigits),
	writePath(Path, MaxDigits).

writePath([], _).
writePath([X], MaxDigits) :-
	number_chars(X, XChars),
	writeLeftPadded(XChars, MaxDigits, ' ').
writePath([H1,H2|TPath], MaxDigits) :-
	number_chars(H1, H1Chars),
	writeLeftPadded(H1Chars, MaxDigits, ' '),
	write(' '),
	writeStepSymbol(H1, H2),
	write(' '),
	writePath([H2|TPath]).

writeStepSymbol(Num, Next) :-
	minmax(Num, Next, Smaller, Larger),
	Doubled is Smaller * 2,
	Diff is Larger - Smaller,
	writeStepSymbol_(Larger, Doubled, Diff).

writeStepSymbol_(Larger, Doubled, Diff) :-
	Larger = Doubled, not( (powOf3(Diff), Diff > 1) ),
	ansi_format([hfg(green)], '*->', []).
writeStepSymbol_(Larger, Doubled, Diff) :-
	not(Larger = Doubled), (powOf3(Diff), Diff > 1),
	ansi_format([hfg(red)], '+->', []).
writeStepSymbol_(Larger, Doubled, Diff) :-
	Larger = Doubled, (powOf3(Diff), Diff > 1),
	ansi_format([hfg(blue)], '?->', []).
writeStepSymbol_(Larger, Doubled, Diff) :-
	not(Larger = Doubled), not( (powOf3(Diff), Diff > 1) ),
	ansi_format([hfg(blue)], '-->', []).


powOf3(1).
powOf3(Num) :-
	integer(Num),
	Num > 0,
	Divided is Num / 3,
	powOf3(Divided).

maxDigits([X], D) :- digits(X, D).
maxDigits([H1,H2|TNums], Max) :-
	digits(H1, D1),
	maxDigits([H2|TNums], DRest),
	minmax(D1, DRest, _, Max).

% minmax(A, B, Min, Max).
minmax(X, X, X, X).
minmax(A, B, A, B) :- A < B.
minmax(A, B, B, A) :- A > B.
	
digits(Num, Digits) :-
	Num > 0,
	number_chars(Num, NChars),
	length(NChars, Digits).

writeLeftPadded([], Count, PadChar) :-
	Count <= 0.
writeLeftPadded([], Count, PadChar) :-
	Count > 0, write(PadChar),
	Remaining is Count - 1,
	writeLeftPadded([], Remaining, PadChar).
writeLeftPadded([H|TString], Count, PadChar) :-
	length([H|TString], Len),
	(Count > Len, write(PadChar), RemStr = [H|TString];
	Count <= Len, write(H), RemStr = TString),
	Remaining is Count - 1,
	writeLeftPadded(RemStr, Remaining, PadChar).	

% Sources:
% https://www.swi-prolog.org/pldoc/man?predicate=integer/1
% https://stackoverflow.com/questions/33063693/best-way-to-generate-integer-numbers-in-prolog
% https://www.swi-prolog.org/pldoc/doc_for?object=f((%5E)/2)
% https://www.swi-prolog.org/pldoc/doc_for?object=write/1
% https://stackoverflow.com/questions/24367985/how-to-turn-off-true-and-false-outputs-in-prolog
% https://www.swi-prolog.org/pldoc/doc_for?object=f(ceiling/1)
% http://www.cse.unsw.edu.au/~billw/dictionaries/prolog/comparison.html
% https://www.swi-prolog.org/pldoc/doc_for?object=f((mod)/2)
% https://www.swi-prolog.org/pldoc/man?predicate=between/3


% Confirming zeros:
% testNums([113, 226, 985, 1970, 3211, 6422, 27875, 55750, 242683, 485366, 793585, 1587170, 6880121, 13760242, 59823937, 119647874, 521638217, 1043276434, 1699132379, 3398264758, 14755320499, 29510640998, 128502917195, 257005834390, 419868489953, 839736979906])

% testNum(113         ),
% testNum(226         ),
% testNum(985         ),
% testNum(1970        ),
% testNum(3211        ),
% testNum(6422        ),
% testNum(27875       ),
% testNum(55750       ),
% testNum(242683      ),
% testNum(485366      ),
% testNum(793585      ),
% testNum(1587170     ),
% testNum(6880121     ),
% testNum(13760242    ),
% testNum(59823937    ),
% testNum(119647874   ),
% testNum(521638217   ),
% testNum(1043276434  ),
% testNum(1699132379  ),
% testNum(3398264758  ),
% testNum(14755320499 ),
% testNum(29510640998 ),
% testNum(128502917195),
% testNum(257005834390),
% testNum(419868489953),
% testNum(839736979906).
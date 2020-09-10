% Can a given number be reached by:
% - Start at 1
% - Double zero or more times, then add 3
% - Double zero or more times, then add 9
% - Double zero or more times, then add 27
% - Continue doubling and adding further powers of 3
% - Stop at any point



% ---------------- finding numbers reachable from 1, with 3^0 as the intial last added power ----------------
% i.e. finding numbers reachable from (1, 0).
% Also allows finding numebrs reachable from other starting points.

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

peakCandidate(Num, Max, BaseCandidate) :-
	between(Num, Max, BaseCandidate).

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



% ---------------- finding paths down from two numbers to some common point, without requiring that common point to be (1, 0). ----------------

% finds a path from one number down to some common base,
% then back up to the other number. If the base found (or
% one of the bases found, if multiple solutions) is (1, 0),
% then the number is considered reachable above.
connected(A, B, Base, BasePow) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	baseCandidate(A, Base),
	baseCandidate(B, Base),
	basePowCandidate(APow, BasePow),
	basePowCandidate(BPow, BasePow),
	reachableFrom(A, APow, Base, BasePow),
	reachableFrom(B, BPow, Base, BasePow).

connectedVia(A, B, Base, BasePow, PathFromA, PathFromB) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	baseCandidate(A, Base),
	baseCandidate(B, Base),
	basePowCandidate(APow, BasePow),
	basePowCandidate(BPow, BasePow),
	reachableFromVia(A, APow, Base, BasePow, PathFromA),
	reachableFromVia(B, BPow, Base, BasePow, PathFromB).

interestingConnectionVia(A, B, Base, BasePow, PathFromA, PathFromB) :-
	connectedVia(A, B, Base, BasePow, PathFromA, PathFromB),
	reverse(PathFromA, [_,AStep1|_]),
	reverse(PathFromB, [_,BStep1|_]),
	dif(AStep1, BStep1).

% simplifiedConnectionVia(A, B, Base, BasePow, SimplePathToA, SimplePathToB) :-
% 	connectedVia(A, B, Base, BasePow, PathFromA, PathFromB),
% 	reverse(PathFromA, RevA),
% 	reverse(PathFromB, RevB),
% 	omitEqualPrefixes(RevA, RevB, SimplePathToA, SimplePathToB).
% 
% omitEqualPrefixes([], [], [], []).
% omitEqualPrefixes([], [H|T], [], [H|T]).
% omitEqualPrefixes([H|T], [], [H|T], []).
% omitEqualPrefixes([H|T1], [H|T2], T1Res, T2Res) :-
% 	omitEqualPrefixes(T1, T2, T1Res, T2Res).
% omitEqualPrefixes([H1|T1], [H2|T2], [H1|T1], [H2|T2]) :-
% 	dif(H1, H2).



% ---------------- finding paths UP from two numbers to some common point ----------------

% finds a path from one number UP to some common peak,
% then back up to the other number.
invConnected(A, B, Max, Peak, PeakPow) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	peakCandidate(A, Max, Peak),
	peakCandidate(B, Max, Peak),
	lastAddedPowCandidate(Peak, PeakPow),
	reachableFrom(Peak, PeakPow, A, APow),
	reachableFrom(Peak, PeakPow, B, BPow).

invConnectedVia(A, B, Max, Peak, PeakPow, PathToA, PathToB) :-
	lastAddedPowCandidate(A, APow),
	lastAddedPowCandidate(B, BPow),
	peakCandidate(A, Max, Peak),
	peakCandidate(B, Max, Peak),
	basePowCandidate(BPow, PeakPow),
	basePowCandidate(APow, PeakPow),
	reachableFromVia(Peak, PeakPow, A, APow, PathToA),
	reachableFromVia(Peak, PeakPow, B, BPow, PathToB).

invInterestingConnectionVia(A, B, Peak, PeakPow, PathToA, PathToB) :-
	invConnectedVia(A, B, Peak, PeakPow, PathToA, PathToB),
	reverse(PathToA, [_,AStep1|_]),
	reverse(PathToB, [_,BStep1|_]),
	dif(AStep1, BStep1).



% ---------------- mapping out graphs of possible steps between numbers ----------------

% step((A, APow), (B, BPow)) :- step_(A, APow, B, BPow).
% step((A, APow), (B, BPow)) :- step_(B, BPow, A, APow).
step(A:APow, B:BPow, Max) :- stepup(A:APow, B:BPow, Max).
step(A:APow, B:BPow, Max) :- stepdown(A:APow, B:BPow, Max).

step((A:APow)-(B:BPow), Max) :- step(A:APow, B:BPow, Max).

stepup(A:Pow, B:Pow, Max) :-
	node(A, Pow, Max),
	B is 2 * A,
	nodeVal(B, Max).
stepup(A:APow, B:BPow, Max) :-
	node(A, APow, Max),
	BPow is APow + 1,
	B is A + (3 ^ BPow),
	nodeVal(B, Max).
	
stepdown(A:Pow, B:Pow, Max) :-
	node(A, Pow, Max),
	B is A / 2,
	nodeVal(B, Max).
stepdown(A:APow, B:BPow, Max) :-
	node(A, APow, Max),
	BPow is APow - 1,
	B is A - (3 ^ APow),
	node(B, BPow, Max).

node(A, Pow, Max) :- nodeVal(A, Max), nodePow(Pow).
nodeVal(X, Max) :- integer(X), X >= 1, not( (ground(Max), X > Max) ).
nodePow(X) :- integer(X), X >= 0.

bfIshSearch(Start, Max, Dest) :-
	bfIshSearch_([Start], [], Max, Dest).

bfIshSearch_([Head|_], _, Max, Head).
bfIshSearch_([Head|Queue], Done, Max, Dest) :-
	NowDone = [Head|Done],
	findall(C, (step(Head, C, Max), not(member(C, NowDone))), Connected),
	append(Queue, Connected, NewQueue),
	sort(NewQueue, DuplicateFree),
	bfIshSearch_(DuplicateFree, NowDone, Max, Dest).

% bfIshSearch(Start, Max, Dest, DPrev) :-
% 	NowDone = [Start],
% 	findall((C, Start), (step(Start, C, Max), not(member(C, NowDone))), Connected),
% 	sort(1, @<, Connected, InitialQueue),
% 	bfIshSearch_(InitialQueue, NowDone, Max, Dest, DPrev).
% 
% bfIshSearch_([(Head, HPrev)|_], _, Max, Head, HPrev).
% bfIshSearch_([(Head, _)|Queue], Done, Max, Dest, DPrev) :-
% 	NowDone = [Head|Done],
% 	findall((C, Head), (step(Head, C, Max), not(member(C, NowDone))), Connected),
% 	append(Queue, Connected, NewQueue),
% 	sort(1, @<, NewQueue, DuplicateFree),
% 	bfIshSearch_(DuplicateFree, NowDone, Max, Dest, DPrev).

% finds connections A-B within the graph, in a breadth-first manner
connectionSearch(Start, Max, A-B) :-
	Start < Max,
	NowDone = [Start],
	findall(C-Start, (step(Start, C, Max), not(member(C, NowDone))), Connected),
	connectionSearch_processConnected(Start, Connected, [], NowDone, Max, A-B).
	%sort(1, @<, Connected, InitialQueue),
	%connectionSearch_(InitialQueue, NowDone, Max, A-B).

% connectionSearch_(Queue, Done, Max, A-B).
% connectionSearch_([(A-B)|_], _, Max, A-B).
connectionSearch_([(Head-HPrev)|Queue], Done, Max, A-B) :-
	NowDone = [Head|Done],
	findall(C-Head, (step(Head, C, Max), not(member(C, NowDone))), Connected),
	connectionSearch_processConnected(Head, Connected, Queue, NowDone, Max, A-B).

% connectionSearch_processConnected(Head, Connected, Queue, Done, Max, Dest, DPrev).
connectionSearch_processConnected(Head, Connected, Queue, Done, Max, C-Head) :-
	member(C-Head, Connected).
connectionSearch_processConnected(Head, Connected, Queue, Done, Max, A-B) :-
	append(Queue, Connected, NewQueue),
	sort(1, @<, NewQueue, DuplicateFree),
	connectionSearch_(DuplicateFree, Done, Max, A-B).

% pathSearch(Start, End, Max, Path)
pathSearch(Start, End, Max, Path) :-
	Start < Max,
	pathSearch_(Start, End, Max, Path).

pathSearch_(Start, Start, Max, PathSoFar [Start|PathSoFar]).
pathSearch_(Start, End, Max, PathSoFar, Path) :-
	.



% ---------------- nice output ----------------

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

findConnectionPaths(A, B, AlignAt, IncludeBoring, Sort) :-
	(IncludeBoring == true -> Search = connectedVia; Search = interestingConnectionVia),
	Goal =.. [Search, A, B, _, _, PFA, PFB],
	findall((PFA, PFB), Goal, PathPairsBag),
	(Sort = true -> sort(PathPairsBag, PathPairs); PathPairs = PathPairsBag),
	member((PathFromA, PathFromB), PathPairs),
	PathDown = PathFromA,
	reverse(PathFromB, [H|PathUp]),
	append(PathDown, PathUp, Path),
	% writeln(PathFromA),
	% writeln(PathFromB),
	% writeln(Path),
	digits(A, AD),
	digits(B, BD),
	minmax(AD, BD, _, MaxDigits),
	length(PathDown, PathDownLen),
	Padding is AlignAt - (MaxDigits * PathDownLen + 5 * (PathDownLen - 1)), 
	writeLeftPadded([], Padding, ' '),
	writePath(Path),
	nl,
	fail.

runFindallSearch(Search, Start) :-
	Goal =.. [Search, Start, _, D],
	call(Goal),
	%(g(D) -> Col = green; Col = white),
	%ansi_format([hfg(white)], D, []),
	write(D),
	write(' '),
	fail.

makeGraphUpTo(Start, Max) :-
	write('graph g {'),
	not( (
		connectionSearch(Start, Max, (A:APow)-(B:BPow)),
		write('"'),
		write(B:BPow),
		write('" -- "'),
		write(A:APow),
		write('"; '),
		fail
	) ),
	write('}').


% ---------------- formatting utilities ----------------

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
	minmax(Num, Next, Smaller, Larger, Reversed),
	Doubled is Smaller * 2,
	Diff is Larger - Smaller,
	writeStepSymbol_(Larger, Reversed, Doubled, Diff).

writeStepSymbol_(Larger, Reversed, Doubled, Diff) :-
	Larger = Doubled,
	not( (powOf3(Diff), Diff > 1) ),
	writeStepSymbol__(green, '*', Reversed).
	% ansi_format([hfg(green)], '*->', []).
writeStepSymbol_(Larger, Reversed, Doubled, Diff) :-
	not(Larger = Doubled),
	(powOf3(Diff), Diff > 1),
	writeStepSymbol__(red, '+', Reversed).
	% ansi_format([hfg(red)], '+->', []).
writeStepSymbol_(Larger, Reversed, Doubled, Diff) :-
	Larger = Doubled,
	(powOf3(Diff), Diff > 1),
	writeStepSymbol__(blue, '?', Reversed).
	% ansi_format([hfg(blue)], '?->', []).
writeStepSymbol_(Larger, Reversed, Doubled, Diff) :-
	not(Larger = Doubled),
	not( (powOf3(Diff), Diff > 1) ),
	writeStepSymbol__(blue, '-', Reversed).
	% ansi_format([hfg(blue)], '-->', []).

% writeStepSymbol__(Colour, Str, Reversed)
writeStepSymbol__(Colour, Symbol, false) :-
	ansi_format([hfg(Colour)], Symbol, []),
	ansi_format([hfg(Colour)], '->', []).
writeStepSymbol__(Colour, Symbol, true) :-
	ansi_format([bg(Colour)], '<-', []),
	ansi_format([bg(Colour)], Symbol, []).



% ---------------- utilities ----------------

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
% minmax(A, B, Min, Max) :- minmax(A, B, Min, Max, _).
minmax(X, X, X, X).
minmax(A, B, A, B) :- A < B.
minmax(A, B, B, A) :- A > B.

% minmax(A, B, Min, Max, Swapped (true/false)).
minmax(X, X, X, X, false).
minmax(A, B, A, B, false) :- A < B.
minmax(A, B, B, A, true) :- A > B.
	
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

intlist([]).
intlist([H|T]) :- integer(H), intlist(T).

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

% Some odd non-multiples of 3: 1,5,7,11,13,17,19,23,25,29,31,35,37,41,43,47,49,53,55,59,61,65,67,71,73,77,79,83,85,89,91,95,97,101,103,107,109,113,115,119,121,125,127,131,133,137,139,143,145,149,151,155,157,161,163,167,169,173,175,179,181,185,187,191,193,197,199,203,205,209,211,215,217,221,223,227,229,233,235,239,241,245,247,251,253,257,259,263,265,269,271,275,277,281,283,287,289,293,295,299,301,305,307,311,313,317,319,323,325,329,331,335,337,341
% "subtract -> halve -> halve -> subtract -> double -> double -> add -> double -> add -> add" works to get from an odd-non-multiple-of-3 x to 2*x,
% for all x from 61 to at least 341 (results checked by hand though so should double check).
% Doesn't work for 55 or various other lower numbers.
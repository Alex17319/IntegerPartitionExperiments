% Can a given number be reached, for some positive integers M and S, by:
% - Start at 1
% - Multiply by M zero or more times, then add S^1
% - Multiply by M zero or more times, then add S^2
% - Multiply by M zero or more times, then add S^3
% - Continue multipling by M and adding further powers of S
% - Stop at any point



% ---------------- finding numbers reachable from 1, with S^0 as the intial last added power ----------------
% i.e. finding numbers reachable from (1, 0).
% Also allows finding numbers reachable from other starting points.

reachable(M, S, Num) :-
	integer(Num), Num > 0,
	integer(M), M > 0,
	integer(S), S > 0,
	reachable(M, S, Num, LastAddedPow).

lastAddedPowCandidate(M, S, Num, LastPowCandidate) :-
	MaxPow is ceiling(log(Num)/log(S)),
	between(0, MaxPow, LastPowCandidate).

baseCandidate(Num, BaseCandidate) :-
	between(1, Num, BaseCandidate).

peakCandidate(Num, Max, BaseCandidate) :-
	between(Num, Max, BaseCandidate).

basePowCandidate(LastAddedPow, BasePowCandidate) :-
	between(0, LastAddedPow, BasePowCandidate).

reachable(M, S, Num, LastAddedPow) :-
	lastAddedPowCandidate(M, S, Num, LastAddedPow),
	reachableFrom(M, S, Num, LastAddedPow, 1, 0).

% reachableFrom(Num, LastAddedPow, Start, StartPrevPow).
reachableFrom(M, S, Start, StartPrevPow, Start, StartPrevPow) :-
	integer(Start), Start > 0,
	integer(StartPrevPow), StartPrevPow >= 0.
reachableFrom(M, S, Num, LastAddedPow, Start, StartPrevPow) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	dividingStep(M, S, Num, Divided),
	reachableFrom(M, S, Divided, LastAddedPow, Start, StartPrevPow).
reachableFrom(M, S, Num, LastAddedPow, Start, StartPrevPow) :-
	inrange(Num, LastAddedPow, Start, StartPrevPow),
	subtractingStep(M, S, Num, LastAddedPow, Subtracted, PrevPow),
	reachableFrom(M, S, Subtracted, PrevPow, Start, StartPrevPow).

inrange(Num, LastAddedPow, Start, StartPrevPow) :-
	integer(Start), Start > 0,
	integer(StartPrevPow), StartPrevPow >= 0,
	integer(Num), Num > 0,
	integer(LastAddedPow), LastAddedPow >= 0,
	Num > Start.

dividingStep(M, S, Num, NewNum) :-
	NewNum is Num / M.

subtractingStep(M, S, Num, LastAddedPow, NewNum, NewLastAddedPow) :-
	NewNum is Num - (S ^ LastAddedPow),
	NewLastAddedPow is LastAddedPow - 1.


% ---------------- nice output ----------------

scan(M, S, IgnoreRule) :-
	between(1, infinite, Num),
	not(call(IgnoreRule, Num)),
	write(Num),
	write(": "),
	testNum(M, S, Num),
	fail.
testNum(M, S, Num) :-
	write(Num),
	write(': '),
	(reachable(M, S, Num), !, write('reachable'); write("unreachable")),
	nl.
testNums(M, S, [Num|Rest]) :-
	testNum(M, S, Num),
	testNums(M, S, Rest).

findZeros(M, S, IgnoreRule) :-
	between(1, infinite, Num),
	not(call(IgnoreRule, Num)),
	testZero(M, S, Num).
testZero(M, S, Num) :-
	(reachable(M, S, Num), !, fail; write(Num), nl),
	fail.



% ---------------- utilities ----------------

% Simple lambda functions, based on https://stackoverflow.com/a/56673667/4149474
% Currying etc. isn't supported
% Example usage:
%   ?- F = {X :- write(X), nl}, call(F, hello).
%   hello
%   
%   ?- F = {X :- Mod3 is X mod 3, Mod3 = 0}, call(F, 1).
%   false.
%   
%   ?- F = {X,Y :- Y is X+1}, call(F, 1, R).
%   R = 2
%   
%   ?- F = {X,Y,Z :- Z is X+Y}, call(F, 1, 2, R).
%   R = 3
runLambda(Formals, Body, Actuals) :-
	copy_term([Body|Formals], [Goal|Actuals]),
	Goal.
'{}'((F1                             :- Body), A1                            ) :- runLambda([F1                            ], Body, [A1                            ]).
'{}'((F1, F2                         :- Body), A1, A2                        ) :- runLambda([F1, F2                        ], Body, [A1, A2                        ]).
'{}'((F1, F2, F3                     :- Body), A1, A2, A3                    ) :- runLambda([F1, F2, F3                    ], Body, [A1, A2, A3                    ]).
'{}'((F1, F2, F3, F4                 :- Body), A1, A2, A3, A4                ) :- runLambda([F1, F2, F3, F4                ], Body, [A1, A2, A3, A4                ]).
'{}'((F1, F2, F3, F4, F5             :- Body), A1, A2, A3, A4, A5            ) :- runLambda([F1, F2, F3, F4, F5            ], Body, [A1, A2, A3, A4, A5            ]).
'{}'((F1, F2, F3, F4, F5, F6         :- Body), A1, A2, A3, A4, A5, A6        ) :- runLambda([F1, F2, F3, F4, F5, F6        ], Body, [A1, A2, A3, A4, A5, A6        ]).
'{}'((F1, F2, F3, F4, F5, F6, F7     :- Body), A1, A2, A3, A4, A5, A6, A7    ) :- runLambda([F1, F2, F3, F4, F5, F6, F7    ], Body, [A1, A2, A3, A4, A5, A6, A7    ]).
'{}'((F1, F2, F3, F4, F5, F6, F7, F8 :- Body), A1, A2, A3, A4, A5, A6, A7, A8) :- runLambda([F1, F2, F3, F4, F5, F6, F7, F8], Body, [A1, A2, A3, A4, A5, A6, A7, A8]).

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
% https://www.swi-prolog.org/pldoc/man?predicate=call/2
% https://www.swi-prolog.org/pldoc/man?section=yall
% https://www.eecs.yorku.ca/course_archive/2008-09/S/3401/calendar/24%20prolog%20-%20operators%20.pdf
% https://www.swi-prolog.org/pldoc/man?predicate=op/3
% https://stackoverflow.com/questions/56662067/is-there-something-like-anonymous-predicates-in-swi-prolog
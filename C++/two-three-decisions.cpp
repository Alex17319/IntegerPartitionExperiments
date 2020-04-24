// C# version has some comments & explanation; this is just the same thing

#include "math-utils.h"
#include "two-three-decision-tracker.h"
#include <cstdlib>
#include <iostream>
#include <cmath>
#include <climits>
#include <vector>
#include <chrono>

using namespace std;

unsigned long long *getExpansionRegister(unsigned long long max) {
	if (max > ULLONG_MAX) {
		cout << "Error: max value too big to be handled by an array of unsigned long long ints" << endl;
		return nullptr;
	}
	if (max < 1) {
		cout << "Error: max < 1" << endl;
		return nullptr;
	}
	
	initDecisionTracker(max);
	unsigned long long *expansionRegister = new unsigned long long[max + 1]();
	
	#define markVisited() expansionRegister[tracker_current] |= 1 << lastAddedThreeExponent()
	#define hasVisited() (expansionRegister[tracker_current] & (1 << lastAddedThreeExponent())) > 0
	
	doubleRepeatedlyUpToMax();
	while (tryAddNextPowerOf3()) { }
	
	markVisited();
	
	while (!trackerAtRoot()) {
		if (backtrackAndCheckIfWasDoublingOp()) {
			if (hasVisited()) continue;
			
			if (tryAddNextPowerOf3()) {
				if (hasVisited()) continue;
				
				doubleRepeatedlyUpToMax();
				
				if (hasVisited()) continue;
				
				while (tryAddNextPowerOf3()) {
					if (hasVisited()) continue;
				}
				
				markVisited();
			}
			else
			{
				markVisited();
				continue;
			}
		}
		else
		{
			markVisited();
		}
	}
	
	destructDecisionTracker();
	
	return expansionRegister;
}

vector<unsigned long long> *getNonTrivialZeros(unsigned long long max) {
	unsigned long long *expansionRegister = getExpansionRegister(max);
	
	vector<unsigned long long> *nonTrivialZeros = new vector<unsigned long long>();
	for (unsigned long long i = 0; i < max + 1; i++) {
		if (i % 3 != 0 && expansionRegister[i] == 0) nonTrivialZeros->push_back(i);
	}
	
	delete[] expansionRegister;
	
	return nonTrivialZeros;
}

int main(int argc, char *argv[]) {
	
	if (sizeof(unsigned long long) != 8) {
		cout << "Error: unexpected unsigned long long size '" << sizeof(unsigned long long) << "', must be 8 bytes" << endl;
		return -1;
	}
	
	if (argc < 2) return -1;
	
	int max = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	vector<unsigned long long> *zeroes = getNonTrivialZeros(max);
	auto end = chrono::system_clock::now();
	
	for (int i = 0; i < zeroes->size(); i++) {
		cout << zeroes->at(i) << endl;
	}
	
	std::chrono::duration<double> elapsed_seconds = end - start;
	cout << "elapsed: " << elapsed_seconds.count() << "." << endl;
	
	return 0;
}
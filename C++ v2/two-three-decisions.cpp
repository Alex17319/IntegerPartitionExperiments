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

struct chunk64bytes {
	char contents[64];
};

unsigned long *getExpansionRegister(unsigned long long max) {
	if (max > ULONG_MAX) {
		cout << "Error: max value too big to be handled by an array of unsigned long ints" << endl;
		return nullptr;
	}
	if (max < 1) {
		cout << "Error: max < 1" << endl;
		return nullptr;
	}
	
	cout << "#1: " << max << ", " << ((max + 1) * sizeof(unsigned long) / 64 + 1) << endl;
	
	initDecisionTracker(max);
	// c++/g++/wsl/windows 10 seems to prevent allocating an array with more than 2^31 elements
	// for some reason, so instead allocate an array of larger elements & then treat it as if it
	// were made up of more elements of the correct size.
	unsigned long *expansionRegister = (unsigned long*)(void*)new struct chunk64bytes[(max + 1) * sizeof(unsigned long) / 64 + 1]();
	
	cout << "#2: " << sizeof(*expansionRegister) << endl;
	
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
	
	#undef markVisited
	#undef hasVisited
	
	destructDecisionTracker();
	
	return expansionRegister;
}

vector<unsigned long long> *getNonTrivialZeros(unsigned long long max) {
	unsigned long *expansionRegister = getExpansionRegister(max);
	
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
	
	cout << "#3: " << argv[1] << endl;
	
	unsigned long long max = strtoull(argv[1], nullptr, 10);
	
	cout << "#4: " << argv[1] << ", " << max << endl;
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "started at: " << ctime(&start_time) << "." << endl;
	
	vector<unsigned long long> *zeroes = getNonTrivialZeros(max);
	
	auto end = chrono::system_clock::now();
	
	for (int i = 0; i < zeroes->size(); i++) {
		cout << zeroes->at(i) << endl;
	}
	
	std::chrono::duration<double> elapsed_seconds = end - start;
	cout << "elapsed: " << elapsed_seconds.count() << "." << endl;
	
	return 0;
}
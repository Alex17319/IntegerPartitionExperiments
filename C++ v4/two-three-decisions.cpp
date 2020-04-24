// C# version has more comments & explanation; this is just the same thing with some further optimisations

#include "math-utils.h"
#include "two-three-decision-tracker.h"
#include <atomic>
#include <bitset>
#include <chrono>
#include <climits>
#include <cmath>
#include <cstdlib>
#include <iomanip>
#include <iostream>
#include <string>
#include <thread>
#include <vector>

using namespace std;

atomic_bool run_poll;
atomic_bool ready_for_poll;

// 3 bytes is enough to go up to 3^1 + 3^2 + 3^3 + ... + 3^24 = 423,644,304,721
// hopefully __attribute__((__packed__)) is enough to make this work in arrays, it seems to be so far
struct __attribute__((__packed__)) tripleByte {
	char bytes[3];
};

struct tripleByte *getExpansionRegister(unsigned long long max) {
	if (max > 423644304721ULL) {
		cout << "Error: max value too big to be handled by an array of struct tripleBytes" << endl;
		return nullptr;
	}
	if (max < 1) {
		cout << "Error: max < 1" << endl;
		return nullptr;
	}
	
	initDecisionTracker(max);
	
	cout << "Allocating array of size " << (mapToAvoidMult3s(max) + 1) << " for max = " << max << endl;
	auto alloc_start = chrono::system_clock::now();
	
	struct tripleByte *expansionRegister = new struct tripleByte[mapToAvoidMult3s(max) + 1]();
	
	auto alloc_end = chrono::system_clock::now();
	std::chrono::duration<double> elapsed_seconds = alloc_end - alloc_start;
	cout << "Allocation done in " << elapsed_seconds.count() << "s" << endl;
	
	ready_for_poll = true;
	
	// access the array using tripleByte size for indexing, then treat the resulting location as an unsigned int
	#define curExpansionRegElement() ( \
		*((unsigned int *)(expansionRegister + mapToAvoidMult3s(tracker_current))) \
	)
	
	#define markVisited() curExpansionRegElement() |= 1 << lastAddedThreeExponent()
	#define hasVisited() (curExpansionRegElement() & (1 << lastAddedThreeExponent())) > 0
	
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
	struct tripleByte *expansionRegister = getExpansionRegister(max);
	
	vector<unsigned long long> *nonTrivialZeros = new vector<unsigned long long>();
	for (unsigned long long i = 0; i < max + 1; i++) {
		if (i % 3 != 0) {
			struct tripleByte val = expansionRegister[mapToAvoidMult3s(i)];
			if (val.bytes[0] == 0 && val.bytes[1] == 0 && val.bytes[2] == 0) {				
				nonTrivialZeros->push_back(i);
			}
		}
	}
	
	delete[] expansionRegister;
	expansionRegister = nullptr;
	
	return nonTrivialZeros;
}

void poll() {
	while (run_poll && !ready_for_poll) { }
	if (!run_poll) return;
	
	int prev_first = -1;
	
	auto poll_start = chrono::system_clock::now();
	time_t poll_start_time = chrono::system_clock::to_time_t(poll_start);
	string poll_start_str = string(ctime(&poll_start_time));
	poll_start_str.pop_back(); //remove newline from end
	cout << endl << "Progress poll (started at " << poll_start_str << "):" << endl;
	
	string header1("x2 since last +3^n");
	int leftover_length = max((int)(tracker_capacity * 3 - header1.length()), 0);
	cout << string((int)floor(leftover_length/2.0), ' ')
		<< header1
		<< string((int)ceil(leftover_length/2.0), ' ')
		<< " |    rep x2 ops  |  sec elapsed"
		<< endl;
	
	auto prev_time = poll_start;
	
	while (run_poll && doublingsSinceLastPowerOfThree != nullptr) {
		//	unsigned long long min_count = ULLONG_MAX;
		//	unsigned long long min_current = ULLONG_MAX;
		//	for (int i = 0; i < 1000000; i++) {
		//		unsigned long long temp_count = tracker_count;
		//		unsigned long long temp_current = tracker_current;
		//		
		//		if (temp_count < min_count) min_count = temp_count;
		//		if (temp_current < min_current) min_current = temp_current;
		//	}
		//	
		//	unsigned long long count_display = (unsigned long long)(pow(10, min_count + 1) - 1)/9;
		//	
		//	printf("%20llu %20llu\r", count_display, min_current);
		
		this_thread::sleep_for(1ms);
		
		bool first_changed;
		for (int i = 0; i < tracker_capacity; i++) {
			int* temp = doublingsSinceLastPowerOfThree;
			if (doublingsSinceLastPowerOfThree == nullptr) goto stop_poll;
			
			int val = temp[i];
			if (doublingsSinceLastPowerOfThree == nullptr) goto stop_poll;
			
			if (i == 0) {
				first_changed = prev_first >= 0 && val != prev_first;
				prev_first = val;
				
				if (first_changed) {
					auto time_now = chrono::system_clock::now();
					std::chrono::duration<double> elapsed_seconds = time_now - prev_time;
					prev_time = time_now;
					repeated_doubling_ops = 0;
					cout << " |  " << elapsed_seconds.count() << "s" << endl;
				}
				printf("\r");
			}
			printf("%2i ", val);
		}
		printf(" | %13llu ", repeated_doubling_ops);
	}
	
stop_poll:
	
	auto time_now = chrono::system_clock::now();
	std::chrono::duration<double> elapsed_seconds = time_now - prev_time;
	cout << " |  " << elapsed_seconds.count() << endl;
	
	cout << endl;
}

void printNonTrivialZeros(unsigned long long max) {
	run_poll = true;
	ready_for_poll = false;
	thread poller(poll);
	
	vector<unsigned long long> *zeroes = getNonTrivialZeros(max);
	
	run_poll = false;
	poller.join();
	
	cout << "Non-trivial zeros up to " << max << ":" << endl;
	
	for (int i = 0; i < zeroes->size(); i++) {
		cout << zeroes->at(i) << endl;
	}
}

void printExpansionRegister(unsigned long long max) {
	run_poll = true;
	ready_for_poll = false;
	thread poller(poll);
	
	struct tripleByte *expansionRegister = getExpansionRegister(max);
	
	run_poll = false;
	poller.join();
	
	cout << "Expansion register up to " << max << ":" << endl;
	
	//for (unsigned long long i = max - 1000 > 0 ? max - 10000 : 0; i < max + 1; i++) {
	for (unsigned long long i = 0; i < max + 1; i += (max / 1000 > 1 ? max / 1000 : 1)) {
		if (i % 3 != 0) {
			printf("%12llu: ", i);
			struct tripleByte val = expansionRegister[mapToAvoidMult3s(i)];
			cout << bitset<8*sizeof(struct tripleByte)>(*((unsigned int *)(&val))) << endl;
		}
	}
	
	delete[] expansionRegister;
	expansionRegister = nullptr;
}

int main(int argc, char *argv[]) {
	
	struct tripleByte testArr[10];
	if (sizeof(testArr) != 30) {
		cout << "Error: unexpected alignment/packing, array of 10 tripleBytes has length '"
			<< sizeof(testArr)
			<< "' instead of length 30" << endl;
		return -1;
	}
	
	if (sizeof(unsigned long long) != 8) {
		cout << "Error: unexpected unsigned long long size '" << sizeof(unsigned long long) << "', must be 8 bytes" << endl;
		return -1;
	}
	
	if (argc < 2) return -1;
	
	unsigned long long max = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time) << flush; // ctime() adds a newline
	cout << endl;
	
	printNonTrivialZeros(max);
	//printExpansionRegister(max);
	
	auto end = chrono::system_clock::now();
	
	cout << endl;
	
	std::chrono::duration<double> elapsed_seconds = end - start;
	cout << "Time elapsed: " << elapsed_seconds.count() << "s" << endl;
	
	return 0;
}
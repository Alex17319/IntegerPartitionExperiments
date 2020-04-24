#include "math-utils.h"
#include <atomic>
#include <bitset>
#include <chrono>
#include <climits>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <string>
#include <thread>
#include <vector>

using namespace std;

//	union __attribute__((__packed__)) tripleByteContainer {
//		struct __attribute__((__packed__)) {
//			struct tripleByte firstThreeBytes;
//			char fourthByte;
//		};
//		unsigned int intView;
//	};

struct expansionRegisterColumn {
	unsigned long long *array;
	unsigned long long capacity; // measured in sizeof(unsigned long long) chunks
	unsigned long long currentArrayPos; // measured in bits
	unsigned long long currentVal;
	unsigned long long size; // measured in sizeof(unsigned long long) chunks
};

void printIntBits(unsigned int x) {
	char bytes[sizeof(unsigned int)];
	*((unsigned int *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]);
	}
}

void printLLongBits(unsigned long long x) {
	char bytes[sizeof(unsigned long long)];
	*((unsigned long long *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]);
	}
}

// can give false negatives, will not give false positives
bool hasONbitsAtMultiplesOf3(unsigned long long x) {
	unsigned long long mask = 0x9249249249249249; // 1001001...1001001
	//	cout << "#60:" << bitset<64>(mask) << endl;
	return ((x & mask) != 0) && ((x & (mask << 1)) != 0) && ((x & (mask << 2)) != 0);
	//	return ((~x & mask) != mask) && ((~x & (mask << 1)) != (mask << 1)) && ((~x & (mask << 2)) != (mask << 2));
}

//	struct expansionRegisterColumn createInitialExpansionRegCol(unsigned long long capacity) {
//		struct expansionRegisterColumn result;
//		result.array = new unsigned long long[capacity]();
//		
//		result.capacity = capacity;
//		result.currentArrayPos = 1;
//		result.currentVal = 1;
//		
//		result.array[0] = (1ULL << 1);
//		result.size = 1;
//		
//		return result;
//	}

struct expansionRegisterColumn createExpansionRegCol(
	unsigned long long capacity,
	unsigned long long currentArrayPos,
	unsigned long long currentVal,
	unsigned long long firstChunkContents
) {
	struct expansionRegisterColumn result;
	result.array = new unsigned long long[capacity]();
	
	result.capacity = capacity;
	result.currentArrayPos = currentArrayPos;
	result.currentVal = currentVal;
	
	result.array[0] = firstChunkContents;
	
	result.size = 1;
	
	return result;
}

void stepForwardBit(struct expansionRegisterColumn *expReg) {
	//	// clear bit we've left (and any other earlier bits in the same chunk)
	//	expReg->array[expReg->currentArrayPos] &= (~0ULL) << (expReg->currentArrayPos % sizeof(unsigned long long));
	
	// If about to leave the current chunk
	if (expReg->currentArrayPos % (8 * sizeof(unsigned long long)) == 8 * sizeof(unsigned long long) - 1) {
		// Reduce size of chunks we've written to
		expReg->size--;
		// Clear chunk we're leaving. Only clear the whole chunk now, rather than each bit as we go, as
		// (1) It means we only do it once for each chunk
		// (2) We don't need bit shifting stuff to make it work
		// (3) We already need this if statement (i.e. branch) to decrement the size, so no performance hit
		// (4) It'll probably be easier to implement copying etc further down if we don't need to worry about
		//     re-filling bits we've just cleared in the chunk we're half way through.
		expReg->array[expReg->currentArrayPos / ( 8 * sizeof(unsigned long long))] = 0;
	}
	
	expReg->currentVal++;
	expReg->currentArrayPos = (expReg->currentArrayPos + 1) % (expReg->capacity * 8 * sizeof(unsigned long long));
}

void stepForwardChunk(struct expansionRegisterColumn *expReg) {
	
	expReg->size--;
	
	// clear whole previous chunk (including bits that were already behind us),
	// and don't clear skipped bits of the newly entered chunk
	expReg->array[expReg->currentArrayPos / (8 * sizeof(unsigned long long))] = 0;
	
	unsigned long long step = 8 * sizeof(unsigned long long);
	expReg->currentVal += step;
	expReg->currentArrayPos = (expReg->currentArrayPos + step) % (expReg->capacity * 8 * sizeof(unsigned long long));
}

bool valueInWriteableRange(struct expansionRegisterColumn *expReg, unsigned long long value) {
	// check if value is not stored/storable any more as its too small
	if (value < expReg->currentVal) return false;
	
	unsigned long long distToVal = value - expReg->currentVal;
	
	// bits we've gone past, but that are in the chunk we're still in,
	// so can't wrap around and write in them again yet
	unsigned long long unusableBits = expReg->currentArrayPos % (8 * sizeof(unsigned long long));
	
	return distToVal < (expReg->capacity * 8 * sizeof(unsigned long long) - unusableBits);
}

void expand(struct expansionRegisterColumn *expReg) {
	struct expansionRegisterColumn result;
	result.array = new unsigned long long[expReg->capacity * 2]();
	
	result.capacity = expReg->capacity * 2;
	result.currentArrayPos = expReg->currentArrayPos % (8 * sizeof(unsigned long long));
	result.currentVal = expReg->currentVal;
	result.size = expReg->size;
	
	unsigned long long copyStartPos = expReg->currentArrayPos / (8 * sizeof(unsigned long long));
	for (unsigned long long i = 0; i < expReg->size; i++) {
		result.array[i] = expReg->array[(copyStartPos + i) % expReg->capacity];
	}
	
	delete[] expReg->array;
	*expReg = result;
}

unsigned long long *getChunkHolding(
	struct expansionRegisterColumn *expReg,
	unsigned long long value,
	int *offsetIntoChunk,
	unsigned long long *minSizeIfWriteHere
) {
	if (value < expReg->currentVal) {
		throw out_of_range(
			string("Error: flag at value '")
			+ to_string(value)
			+ "' not stored any more in expansionRegisterColumn, as value is smaller than currentVal '"
			+ to_string(expReg->currentVal)
			+ "'."
		);
	}
	
	while (!valueInWriteableRange(expReg, value)) {
		expand(expReg);
	}
	
	unsigned long long distToVal = value - expReg->currentVal;
	unsigned long long valPos = expReg->currentArrayPos + distToVal;
	
	// we've already ensured that value is in the writeable range, so its safe to just wrap valPos around.
	unsigned long long *valChunk = expReg->array + ((valPos / (8 * sizeof(unsigned long long))) % expReg->capacity);
	
	*offsetIntoChunk = valPos % (8 * sizeof(unsigned long long));
	*minSizeIfWriteHere = valPos/(8 * sizeof(unsigned long long)) - expReg->currentArrayPos/(8 * sizeof(unsigned long long)) + 1;
	
	return valChunk;
}

void addFlagAt(struct expansionRegisterColumn *expReg, unsigned long long value, bool flag) {
	if (!flag) return;
	
	unsigned long long minSizeAfterWrite;
	int offsetIntoChunk;
	unsigned long long *destChunk = getChunkHolding(expReg, value, &offsetIntoChunk, &minSizeAfterWrite);
	
	*((unsigned int *)destChunk) |= (1ULL << offsetIntoChunk);
	
	expReg->size = max(expReg->size, minSizeAfterWrite);
}

void findAndPrintZeros(unsigned long long startSize) {
	if (startSize < 1) {
		throw out_of_range("startSize < 1");
	}
	
	//	cout << "Allocating initial array of size " << startSize << " for startSize = " << startSize << endl;
	//	auto alloc_start = chrono::system_clock::now();
	
	vector<struct expansionRegisterColumn> expReg;
	
	//	auto alloc_end = chrono::system_clock::now();
	//	std::chrono::duration<double> elapsed_seconds = alloc_end - alloc_start;
	//	cout << "Allocation done in " << elapsed_seconds.count() << "s" << endl;
	
	// initialisation - the process is more complex for first chunk (as doubling
	// can leave you in the same chunk; in later chunks it never does)
	expReg.push_back(createExpansionRegCol(startSize, 0, 0, 1ULL << 1));
	for (int i = 0; threeToThe(i) <= 8 * sizeof(unsigned long long); i++) {
		// for each ON bit at some position j, turn the bit at 2*j ON
		for (int j = 1; j * 2 < 8 * sizeof(unsigned long long); j++) {
			// must not run this for j * 2 = 64 or above, otherwise we'll be bitshifting by 64 bits,
			// which is undefined behaviour (and on my machine seems to wrap around sometimes)
			//	cout << "#10"
			//		<< ", " << j
			//		<< ", " << bitset<64>(expReg[i].array[0])
			//		<< ", " << bitset<64>(1ULL << j)
			//		<< ", " << bitset<64>(expReg[i].array[0] & (1ULL << j))
			//		<< ", " << (j * 2)
			//		<< ", " << bitset<64>(1ULL << (j * 2))
			//		<< "." << endl;
			if ((expReg[i].array[0] & (1ULL << j)) != 0) {
				expReg[i].array[0] |= (1ULL << (j * 2));
			}
		}
		
		if (threeToThe(i) >= 8 * sizeof(unsigned long long)) break; // don't do the next part on the last iteration
		
		expReg.push_back(createExpansionRegCol(startSize, 0, 0, 0));
		
		// in expansion-register-column i (which represents 3^i being the last added power of 3),
		// for each ON bit at some position j, turn the bit at j + 3^(i+1) ON in expansion-register-column i+1
		for (int j = 1; (j + threeToThe(i + 1)) < 8 * sizeof(unsigned long long); j++) {
			if ((expReg[i].array[0] & (1ULL << j)) != 0) {
				expReg[i + 1].array[0] |= (1ULL << (j + threeToThe(i + 1)));
			}
		}
	}
	
	//	for (int i = 0; i < expReg.size(); i++) {
	//		cout << "#1:";
	//		printLLongBits(expReg[i].array[0]);
	//		cout << endl;
	//	}
	for (int i = 0; i < expReg.size(); i++) {
		cout << "#2: " << bitset<8*sizeof(unsigned long long)>(expReg[i].array[0]) << endl;
	}
	
	for (unsigned long long curValue = 0; true; curValue += 8 * sizeof(unsigned long long)) {
		
		unsigned long long chunkAggregate = 0x9249249249249249 >> (curValue % 3);
		// ^ Hex constant is 1001001...1001001
		// The shift moves it so that the ON bits are the bits which, in the
		// current chunk, correspond to values that are multiples of 3.
		
		//	vector<unsigned long long> expansionRegPrint;
		
		//if (curValue > 8000) exit(0);
		
		//	cout << "#3: " << curValue << endl;
		//	for (int i = 0; i < expReg.size(); i++) {
		//		cout << "#4: "
		//			<< bitset<8*sizeof(unsigned long long)>(
		//				expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long)) + 1]
		//			)
		//			<< ", "
		//			<< bitset<8*sizeof(unsigned long long)>(
		//				expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long))]
		//			)
		//			<< ", " << expReg[i].currentArrayPos
		//			<< ", " << expReg[i].currentVal
		//			<< endl;
		//	}
		
		for (int i = 0; i < expReg.size(); i++) {
			if (expReg[i].currentVal / (8 * sizeof(unsigned long long)) > curValue / (8 * sizeof(unsigned long long))) {
				//	expansionRegPrint.push_back(0);
				
				continue; // column doesn't start yet
			}
			if (expReg[i].currentVal / (8 * sizeof(unsigned long long)) < curValue / (8 * sizeof(unsigned long long))) {
				throw logic_error("Assertion failed: missed some chunks");
			}
			
			unsigned long long chunkContents = expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long))];
			
			if (hasONbitsAtMultiplesOf3(chunkContents)) {
				cout << "#40"
					<< ", " << bitset<64>(chunkContents)
					<< endl;
			}
			
			if (chunkContents == 0) {
				// no need to do any copying
				
				//	expansionRegPrint.push_back(expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long))]);
				
				stepForwardChunk(&expReg[i]);
				continue;
			}
			
			chunkAggregate |= chunkContents;
			
			unsigned long long pow = threeToThe(i + 1);
			
			if (i == expReg.size() - 1) {
				expReg.push_back(
					createExpansionRegCol(
						startSize, // capacity
						0, //(expReg[i].currentArrayPos + pow) % (8 * sizeof(unsigned long long)), // currentArrayPos
						expReg[i].currentVal + pow - (expReg[i].currentVal + pow) % (8 * sizeof(unsigned long long)), // currentVal
						0 //firstChunkContents
					)
				);
				//	cout << "#10: "
				//		<< i
				//		<< ", " << expReg.size()
				//		<< ", " << bitset<64>(chunkContents)
				//		<< ", " << pow
				//		<< ", " << expReg[i].currentArrayPos
				//		<< ", " << expReg[i].currentVal
				//		<< endl;
			}
			
			{
				//	unsigned long long minSizeAfterWrite_one;
				//	unsigned long long minSizeAfterWrite_two;
				//	unsigned long long *destChunkOne = getChunkHolding(&expReg[i + 1], expReg[i + 1].currentVal + pow, &minSizeAfterWrite_one);
				//	unsigned long long *destChunkTwo = getChunkHolding(&expReg[i + 1], expReg[i + 1].currentVal + pow + sizeof(unsigned long long), &minSizeAfterWrite_two);
				//	
				//	unsigned long long minSizeAfterWrite = max(minSizeAfterWrite_one, minSizeAfterWrite_two);
				
				unsigned long long minSizeAfterWrite;
				int offsetIntoDestChunk;
				unsigned long long *destChunkTwo = getChunkHolding(
					&expReg[i + 1],
					expReg[i].currentVal + pow + 8 * sizeof(unsigned long long),
					&offsetIntoDestChunk, // will be the same regardless of adding chunk-length
					&minSizeAfterWrite
				);
				unsigned long long offsetToTwo = destChunkTwo - expReg[i + 1].array;
				unsigned long long *destChunkOne = expReg[i + 1].array + (offsetToTwo + expReg[i + 1].capacity - 1) % expReg[i + 1].capacity;
				
				int offsetIntoCurrentChunk = expReg[i].currentArrayPos % (8 * sizeof(unsigned long long));
				
				unsigned long long origDestChunkOne = *destChunkOne;
				unsigned long long origDestChunkTwo = *destChunkTwo;
				
				*destChunkOne |= ((chunkContents >> offsetIntoCurrentChunk) << offsetIntoDestChunk);
				*destChunkTwo |= ((chunkContents >> offsetIntoCurrentChunk) >> (8 * sizeof(unsigned long long) - offsetIntoDestChunk));
				
				if (hasONbitsAtMultiplesOf3(*destChunkOne) || hasONbitsAtMultiplesOf3(*destChunkTwo) /*|| *destChunkTwo == 0x5145105145000000*/) {
					cout << "#10"
						<< ", " << hasONbitsAtMultiplesOf3(*destChunkTwo)
						<< ", " << hasONbitsAtMultiplesOf3(*destChunkOne)
						<< ", " << (*destChunkTwo == 0x5145105145000000)
						<< endl;
					cout << "#11"
						<< ", " << bitset<64>(*destChunkTwo)
						<< ", " << bitset<64>(*destChunkOne)
						<< ", " << (destChunkTwo - expReg[i + 1].array)
						<< ", " << (destChunkOne - expReg[i + 1].array)
						<< ", " << offsetIntoDestChunk
						<< ", " << offsetIntoCurrentChunk
						<< endl;
					cout << "#12"
						<< ", " << bitset<64>(origDestChunkTwo)
						<< ", " << bitset<64>(origDestChunkOne)
						<< endl;
					cout << "#13"
						<< ", " << bitset<64>(chunkContents)
						<< ", " << bitset<64>(chunkContents >> offsetIntoCurrentChunk)
						<< endl;
					cout << "#14"
						<< ", " << bitset<64>((chunkContents >> offsetIntoCurrentChunk) << offsetIntoDestChunk)
						<< ", " << bitset<64>((chunkContents >> offsetIntoCurrentChunk) >> (8 * sizeof(unsigned long long) - offsetIntoDestChunk))
						<< endl;
				}
				
				expReg[i + 1].size = max(expReg[i + 1].size, minSizeAfterWrite);
			}
			
			{
				//	unsigned long long minSizeAfterWrite_one;
				//	unsigned long long minSizeAfterWrite_two;
				//	int ignored;
				//	unsigned long long *destChunkOne = getChunkHolding(&expReg[i], expReg[i].currentVal * 2, &ignored, &minSizeAfterWrite_one);
				//	unsigned long long *destChunkTwo = getChunkHolding(&expReg[i], expReg[i].currentVal * 2 + 8 * sizeof(unsigned long long), &ignored, &minSizeAfterWrite_two);
				//	
				//	unsigned long long minSizeAfterWrite = max(minSizeAfterWrite_one, minSizeAfterWrite_two);
				
				unsigned long long minSizeAfterWrite;
				int ignored;
				unsigned long long *destChunkTwo = getChunkHolding(
					&expReg[i],
					expReg[i].currentVal * 2 + 8 * sizeof(unsigned long long),
					&ignored,
					&minSizeAfterWrite
				);
				unsigned long long offsetToTwo = destChunkTwo - expReg[i].array;
				unsigned long long *destChunkOne = expReg[i].array + (offsetToTwo + expReg[i].capacity - 1) % expReg[i].capacity;
				
				unsigned long long origDestChunkOne = *destChunkOne;
				unsigned long long origDestChunkTwo = *destChunkTwo;
				
				spreadAndOrBits(chunkContents, destChunkOne, destChunkTwo);
				
				if (hasONbitsAtMultiplesOf3(*destChunkOne) || hasONbitsAtMultiplesOf3(*destChunkTwo) /*|| *destChunkTwo == 0x5145105145000000*/) {
					cout << "#30"
						<< ", " << hasONbitsAtMultiplesOf3(*destChunkTwo)
						<< ", " << hasONbitsAtMultiplesOf3(*destChunkOne)
						<< ", " << (*destChunkTwo == 0x5145105145000000)
						<< endl;
					cout << "#31"
						<< ", " << bitset<64>(*destChunkTwo)
						<< ", " << bitset<64>(*destChunkOne)
						<< ", " << (destChunkTwo - expReg[i].array)
						<< ", " << (destChunkOne - expReg[i].array)
						<< endl;
					cout << "#32"
						<< ", " << bitset<64>(origDestChunkTwo)
						<< ", " << bitset<64>(origDestChunkOne)
						<< endl;
					cout << "#33"
						<< ", " << bitset<64>(chunkContents)
						<< endl;
				}
				
				expReg[i].size = max(expReg[i].size, minSizeAfterWrite);
			}
			
			//	expansionRegPrint.push_back(expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long))]);
			//cout << "#20: " << bitset<64>(expReg[i].array[expReg[i].currentArrayPos / (8 * sizeof(unsigned long long))]) << endl;
			
			stepForwardChunk(&expReg[i]);
		}
		
		for (int i = 0; i < expReg.size(); i++) {
			for (int j = 0; j < expReg[i].capacity; j++) {
				if (hasONbitsAtMultiplesOf3(expReg[i].array[j])) {
					cout << "#50: " << curValue << endl;
				}
			}
		}
		
		//	for (int i = 0; i < 8 * sizeof(unsigned long long); i++) {
		//		for (int j = expansionRegPrint.size() - 1; j >= 0; j--) {
		//			unsigned long long chunk = expansionRegPrint[j];
		//			//cout << "#21: " << bitset<64>(chunk) << ", " << bitset<64>(1ULL << i) << endl;
		//			unsigned long long bit = chunk & (1ULL << i);
		//			cout << (bit != 0 ? "1" : "0");
		//		}
		//		cout << endl;
		//	}
		
		if (~chunkAggregate != 0) { // if there are any bits that haven't been set to one - i.e. a value that wasn't reachable
			// then find those bits & print the values they correspond to
			for (int i = 0; i < 8 * sizeof(unsigned long long); i++) {
				if ((~chunkAggregate) & (1ULL << i)) {
					time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
					
					cout << /*"\r" <<*/ "found zero: " << (curValue + i) << " @ " << ctime(&time_now); // ctime() adds a newline
				}
			}
		}
		else if (curValue % 1000 == 0) {
			//cout << "\r" << "at: " << curValue << flush;
		}
	}
}

int main(int argc, char *argv[]) {
	
	if (sizeof(unsigned long long) != 8) {
		cout << "Error: unexpected unsigned long long size '" << sizeof(unsigned long long) << "', must be 8 bytes" << endl;
		return -1;
	}
	if (sizeof(unsigned int) != 4) {
		cout << "Error: unexpected unsigned int size '" << sizeof(unsigned int) << "', must be 4 bytes" << endl;
		return -1;
	}
	
	if (argc < 2) return -1;
	
	unsigned long long startSize = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time); // ctime() adds a newline
	cout << endl;
	
	findAndPrintZeros(startSize);
}
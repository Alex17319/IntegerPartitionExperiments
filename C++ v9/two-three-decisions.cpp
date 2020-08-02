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
#include <stdint.h>

using namespace std;

const int CHUNK_BITS = 64;

struct expansionRegisterColumn {
	uint64_t *array;
	uint64_t capacity; // measured in CHUNK_BITS-long chunks (i.e. the number of uint64_t's in the array)
	uint64_t currentArrayPos; // measured in bits
	uint64_t currentVal;
	uint64_t size; // measured in CHUNK_BITS-long chunks
};

// prints from bytes in memory, so affected by endianness
void printIntBits_memory(unsigned int x) {
	char bytes[sizeof(unsigned int)];
	*((unsigned int *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]).to_string('-', '#');
	}
}

// prints from bytes in memory, so affected by endianness
void printUInt64Bits_memory(uint64_t x) {
	char bytes[sizeof(uint64_t)];
	*((uint64_t *)(&(bytes[0]))) = x;
	for (int i = 0; i < sizeof(bytes); i++) {
		if (i > 0) cout << ":";
		cout << bitset<8>(bytes[i]).to_string('-', '#');
	}
}

// prints the number the way mathematical operations see it
void printUInt64Bits_cpu(uint64_t x) {
	string b = bitset<64>(x).to_string('-', '#');
	for (int i = 0; i < 8; i++) {
		if (i > 0) cout << ":";
		cout << b.substr(i * 8, 8);
	}
}

// prints the number the way mathematical operations see it, but reversed
void printUInt64Bits_cpu_reverse(uint64_t x) {
	string b = bitset<64>(x).to_string('-', '#');
	for (int i = 63; i >= 0; i--) {
		cout << b[i];
		//if (i > 0 && i % 8 == 0) cout << ":";
	}
}

// can give false negatives, will not give false positives
bool hasONbitsAtMultiplesOf3(uint64_t x) {
	uint64_t mask = 0x9249249249249249; // 1001001...1001001
	return ((x & mask) != 0) && ((x & (mask << 1)) != 0) && ((x & (mask << 2)) != 0);
	//	return ((~x & mask) != mask) && ((~x & (mask << 1)) != (mask << 1)) && ((~x & (mask << 2)) != (mask << 2));
}

struct expansionRegisterColumn createExpansionRegCol(
	uint64_t capacity,
	uint64_t currentArrayPos,
	uint64_t currentVal,
	uint64_t firstChunkContents
) {
	struct expansionRegisterColumn result;
	result.array = new uint64_t[capacity]();
	
	result.capacity = capacity;
	result.currentArrayPos = currentArrayPos;
	result.currentVal = currentVal;
	
	result.array[0] = firstChunkContents;
	
	result.size = 1;
	
	return result;
}

void stepForwardChunk(struct expansionRegisterColumn *expRegCol) {
	
	expRegCol->size--;
	
	// clear whole previous chunk (including bits that were already behind us),
	// and don't clear skipped bits of the newly entered chunk
	expRegCol->array[expRegCol->currentArrayPos / CHUNK_BITS] = 0;
	
	uint64_t step = CHUNK_BITS;
	expRegCol->currentVal += step;
	expRegCol->currentArrayPos = (expRegCol->currentArrayPos + step) % (expRegCol->capacity * CHUNK_BITS);
}

bool valueInWriteableRange(struct expansionRegisterColumn *expRegCol, uint64_t value) {
	// check if value is not stored/storable any more as its too small
	if (value < expRegCol->currentVal) return false;
	
	uint64_t distToVal = value - expRegCol->currentVal;
	
	// bits we've gone past, but that are in the chunk we're still in,
	// so can't wrap around and write in them again yet
	uint64_t unusableBits = expRegCol->currentArrayPos % CHUNK_BITS;
	
	return distToVal < (expRegCol->capacity * CHUNK_BITS - unusableBits);
}

void expand(struct expansionRegisterColumn *expRegCol) {
	struct expansionRegisterColumn result;
	result.array = new uint64_t[expRegCol->capacity * 2]();
	
	result.capacity = expRegCol->capacity * 2;
	result.currentArrayPos = expRegCol->currentArrayPos % CHUNK_BITS;
	result.currentVal = expRegCol->currentVal;
	result.size = expRegCol->size;
	
	uint64_t copyStartPos = expRegCol->currentArrayPos / CHUNK_BITS;
	for (uint64_t i = 0; i < expRegCol->size; i++) {
		result.array[i] = expRegCol->array[(copyStartPos + i) % expRegCol->capacity];
	}
	
	delete[] expRegCol->array;
	*expRegCol = result;
}

uint64_t *getChunkHolding(
	struct expansionRegisterColumn *expRegCol,
	uint64_t value,
	int *offsetIntoChunk,
	uint64_t *minSizeIfWriteHere
) {
	if (value < expRegCol->currentVal) {
		throw out_of_range(
			string("Error: flag at value '")
			+ to_string(value)
			+ "' not stored any more in expansionRegisterColumn, as value is smaller than currentVal '"
			+ to_string(expRegCol->currentVal)
			+ "'."
		);
	}
	
	while (!valueInWriteableRange(expRegCol, value)) {
		expand(expRegCol);
	}
	
	uint64_t distToVal = value - expRegCol->currentVal;
	uint64_t valPos = expRegCol->currentArrayPos + distToVal;
	
	// we've already ensured that value is in the writeable range, so its safe to just wrap valPos around.
	uint64_t *valChunk = expRegCol->array + ((valPos / CHUNK_BITS) % expRegCol->capacity);
	
	*offsetIntoChunk = valPos % CHUNK_BITS;
	*minSizeIfWriteHere = valPos/CHUNK_BITS - expRegCol->currentArrayPos/CHUNK_BITS + 1;
	
	return valChunk;
}

// the process is more complex for first chunk (as doubling
// can leave you in the same chunk; in later chunks it never does)
void initialiseExpRegFirstChunk(vector<struct expansionRegisterColumn> *expReg, uint64_t startSize) {
	expReg->push_back(createExpansionRegCol(startSize, 0, 0, 1ULL << 1));
	for (int i = 0; threeToThe(i) <= CHUNK_BITS; i++) {
		// for each ON bit at some position j, turn the bit at 2*j ON
		for (int j = 1; j * 2 < CHUNK_BITS; j++) {
			// must not run this for j * 2 = 64 or above, otherwise we'll be bitshifting by >=64 bits,
			// which is undefined behaviour (and on my machine seems to wrap around sometimes)
			
			if (((*expReg)[i].array[0] & (1ULL << j)) != 0) {
				(*expReg)[i].array[0] |= (1ULL << (j * 2));
			}
		}
		
		if (threeToThe(i) >= CHUNK_BITS) break; // don't do the next part on the last iteration
		
		expReg->push_back(createExpansionRegCol(startSize, 0, 0, 0));
		
		// in expansion-register-column i (which represents 3^i being the last added power of 3),
		// for each ON bit at some position j, turn the bit at j + 3^(i+1) ON in expansion-register-column i+1
		for (int j = 1; (j + threeToThe(i + 1)) < CHUNK_BITS; j++) {
			if (((*expReg)[i].array[0] & (1ULL << j)) != 0) {
				(*expReg)[i + 1].array[0] |= (1ULL << (j + threeToThe(i + 1)));
			}
		}
	}
}

bool columnHasStarted(struct expansionRegisterColumn *expRegCol, int colNum, uint64_t curValue) {
	if (expRegCol->currentVal / CHUNK_BITS < curValue / CHUNK_BITS) {
		throw logic_error("Assertion failed: missed some chunks");
	}
	
	return expRegCol->currentVal / CHUNK_BITS <= curValue / CHUNK_BITS;
}

uint64_t getCurrentChunkContents(struct expansionRegisterColumn *expRegCol) {
	return expRegCol->array[expRegCol->currentArrayPos / CHUNK_BITS];
}

void growExpRegIfLastColumn(vector<struct expansionRegisterColumn> *expReg, int colNum, uint64_t pow, uint64_t startSize) {
	struct expansionRegisterColumn *currentCol = &((*expReg)[colNum]);
	
	if (colNum == expReg->size() - 1) {
		expReg->push_back(
			createExpansionRegCol(
				startSize, // capacity
				0, // currentArrayPos
				currentCol->currentVal + pow - (currentCol->currentVal + pow) % CHUNK_BITS, // currentVal
				0 //firstChunkContents
			)
		);
	}
}

void copyAlongByPowerOfThree(vector<struct expansionRegisterColumn> *expReg, int colNum, uint64_t chunkContents, uint64_t startSize) {
	uint64_t pow = threeToThe(colNum + 1);
	
	growExpRegIfLastColumn(expReg, colNum, pow, startSize);
	
	struct expansionRegisterColumn *currentCol = &((*expReg)[colNum]);
	struct expansionRegisterColumn *nextCol = &((*expReg)[colNum + 1]);
	
	uint64_t minSizeAfterWrite;
	int offsetIntoDestChunk;
	uint64_t *destChunkTwo = getChunkHolding(
		nextCol,
		currentCol->currentVal + pow + CHUNK_BITS,
		&offsetIntoDestChunk, // will be the same for first and second dest chunks, i.e. regardless of adding any multiple of CHUNK_BITS
		&minSizeAfterWrite
	);
	uint64_t offsetToTwo = destChunkTwo - nextCol->array;
	uint64_t *destChunkOne = nextCol->array + (offsetToTwo + nextCol->capacity - 1) % nextCol->capacity;
	
	int offsetIntoCurrentChunk = currentCol->currentArrayPos % CHUNK_BITS;
	
	*destChunkOne |= ((chunkContents >> offsetIntoCurrentChunk) << offsetIntoDestChunk);
	*destChunkTwo |= ((chunkContents >> offsetIntoCurrentChunk) >> (CHUNK_BITS - offsetIntoDestChunk));
	
	nextCol->size = max(nextCol->size, minSizeAfterWrite);
}

void copyAlongToDoubleCurrentPos(struct expansionRegisterColumn *expRegCol, uint64_t chunkContents) {
	uint64_t minSizeAfterWrite;
	int ignored;
	uint64_t *destChunkTwo = getChunkHolding(
		expRegCol,
		expRegCol->currentVal * 2 + CHUNK_BITS,
		&ignored,
		&minSizeAfterWrite
	);
	uint64_t offsetToTwo = destChunkTwo - expRegCol->array;
	uint64_t *destChunkOne = expRegCol->array + (offsetToTwo + expRegCol->capacity - 1) % expRegCol->capacity;
	
	spreadAndOrBits(chunkContents, destChunkOne, destChunkTwo);
	
	expRegCol->size = max(expRegCol->size, minSizeAfterWrite);
}

void findAndPrintZeros(uint64_t startSize) {
	if (startSize < 1) {
		throw out_of_range("startSize < 1");
	}
	
	vector<struct expansionRegisterColumn> expReg;
	
	initialiseExpRegFirstChunk(&expReg, startSize);
	
	for (uint64_t curValue = 0; true; curValue += CHUNK_BITS) {
		
		uint64_t chunkAggregate = 0x9249249249249249 >> (curValue % 3);
		// ^ Hex constant is 1001001...1001001
		// The shift moves it so that the ON bits are the bits which, in the
		// current chunk, correspond to values that are multiples of 3.
		
		//i == 0:
		if (curValue == 0) {
			chunkAggregate |= 0b00000000'00000000'00000000'00000001'00000000'00000001'00000001'00010110;
		} else if (isPowerOf2(curValue / CHUNK_BITS)) {
			chunkAggregate |= 1;
		}
		
		//i == 1:
		{
			uint64_t chunkContents = 0;
			if (curValue / CHUNK_BITS == 0) { // full value when chunk number == 0
				chunkContents = 0b00000001'00000000'00010001'01001001'00010000'01011001'01001101'10110000;
			} else {
				// when chunk number == 1
				if (curValue / CHUNK_BITS == 1) {
					chunkContents |= 0b00000000'00000000'00000000'00000000'00000000'00000001'00000000'00000000;
				}
				// whenever chunk number == 2^n for some non-negative integer n
				if (isPowerOf2(curValue / CHUNK_BITS)) {
					chunkContents |= 0b00000000'00000001'00000000'00000000'00000001'00000000'00010000'01001001;
				}
				// whenever chunk number == 2^n + 1 for some non-negative integer n
				if (isPowerOf2(curValue / CHUNK_BITS - 1)) {
					chunkContents |= 0b00000000'00000000'00000000'00000001'00000000'00000000'00000000'00000000;
				}
				// whenever chunk number == 2^n + 3*2^m where n >= m for some non-negative integers n and m
				if (TODO) {
					chunkContents |= 1;
				}
				
			}
			chunkAggregate |= chunkContents;
		}
		
		for (int i = 2; i < expReg.size(); i++) {
			#define currentCol (&(expReg[i])) //can't store a pointer as it'll be invalidated if the vector grows (in copyAlongByPowerOfThree)
			if (columnHasStarted(currentCol, i, curValue)) {
				uint64_t chunkContents = getCurrentChunkContents(currentCol);
				
				if (chunkContents != 0) { // otherwise no need to do any copying
					chunkAggregate |= chunkContents;
					
					copyAlongByPowerOfThree(&expReg, i, chunkContents, startSize);
					copyAlongToDoubleCurrentPos(currentCol, chunkContents);
				}
				
				stepForwardChunk(currentCol);
			}
			#undef currentCol
		}
		
		if (chunkAggregate != ~0) { // if there are any bits that haven't been set to one - i.e. a value that wasn't reachable
			// then find those bits & print the values they correspond to
			for (int i = 0; i < CHUNK_BITS; i++) {
				if ((~chunkAggregate) & (1ULL << i)) {
					time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
					
					cout << "\r" << "found zero: " << (curValue + i) << " @ " << ctime(&time_now); // ctime() adds a newline
				}
			}
		}
		else if (curValue % 5000 == 0) { // print progress every so often. Don't print too often or flush as either may slow things
			cout << "\r" << "at: " << curValue;
		}
	}
}

void printExpansionRegister(uint64_t startSize) {
	if (startSize < 1) {
		throw out_of_range("startSize < 1");
	}
	
	vector<struct expansionRegisterColumn> expReg;
	
	initialiseExpRegFirstChunk(&expReg, startSize);
	
	for (uint64_t curValue = 0; true; curValue += CHUNK_BITS) {
		
		vector<uint64_t> expansionRegPrint;
		
		for (int i = 0; i < expReg.size(); i++) {
			#define currentCol (&(expReg[i])) //can't store a pointer as it'll be invalidated if the vector grows (in copyAlongByPowerOfThree)
			if (columnHasStarted(currentCol, i, curValue)) {
				uint64_t chunkContents = getCurrentChunkContents(currentCol);
				
				if (chunkContents != 0) { // otherwise no need to do any copying
					copyAlongByPowerOfThree(&expReg, i, chunkContents, startSize);
					copyAlongToDoubleCurrentPos(currentCol, chunkContents);
				}
				
				expansionRegPrint.push_back(getCurrentChunkContents(currentCol));
				
				stepForwardChunk(currentCol);
			} else {
				expansionRegPrint.push_back(0);
			}
			#undef currentCol
		}
		
		for (int i = 0; i < CHUNK_BITS; i++) {
			for (int j = expansionRegPrint.size() - 1; j >= 0; j--) {
				uint64_t chunk = expansionRegPrint[j];
				uint64_t bit = chunk & (1ULL << i);
				cout << (bit != 0 ? "1" : "0");
			}
			cout << endl;
		}
	}
}

void printExpansionRegColumn(uint64_t startSize, int printColumn) {
	if (startSize < 1) {
		throw out_of_range("startSize < 1");
	}
	
	vector<struct expansionRegisterColumn> expReg;
	
	initialiseExpRegFirstChunk(&expReg, startSize);
	
	for (uint64_t curValue = 0; true; curValue += CHUNK_BITS) {
		for (int i = 0; i < expReg.size(); i++) {
			#define currentCol (&(expReg[i])) //can't store a pointer as it'll be invalidated if the vector grows (in copyAlongByPowerOfThree)
			if (columnHasStarted(currentCol, i, curValue)) {
				uint64_t chunkContents = getCurrentChunkContents(currentCol);
				
				if (chunkContents != 0) { // otherwise no need to do any copying
					copyAlongByPowerOfThree(&expReg, i, chunkContents, startSize);
					copyAlongToDoubleCurrentPos(currentCol, chunkContents);
				}
				
				if (i == printColumn) {
					printUInt64Bits_cpu_reverse(getCurrentChunkContents(currentCol));
					//cout << " @ " << (curValue / CHUNK_BITS) << endl;
					cout << endl;
				}
				
				stepForwardChunk(&expReg[i]);
			}
			#undef currentCol
		}
	}
}

int main(int argc, char *argv[]) {
	
	if (sizeof(uint64_t) != 8) {
		cout << "Error: unexpected uint64_t size '" << sizeof(uint64_t) << "', must be 8 bytes" << endl;
		return -1;
	}
	if (sizeof(unsigned int) != 4) {
		cout << "Error: unexpected unsigned int size '" << sizeof(unsigned int) << "', must be 4 bytes" << endl;
		return -1;
	}
	
	if (argc < 2) return -1;
	
	uint64_t startSize = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time); // ctime() adds a newline
	cout << endl;
	
	findAndPrintZeros(startSize);
	//printExpansionRegister(startSize);
	//printExpansionRegColumn(startSize, 6);
}
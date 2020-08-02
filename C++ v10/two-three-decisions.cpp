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
#include <new>

using namespace std;

const int CHUNK_BITS = 64;

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

// the process is more complex for first chunk (as doubling
// can leave you in the same chunk; in later chunks it never does)
uint64_t initialiseColFirstChunk(uint64_t prevColFirstChunk, int newColPowerOf3) {
	uint64_t newColFirstChunk = 0;
	
	// Note: must never shift too far (i.e. >= 64 bits), as that's undefined behaviour and e.g. may wrap around.
	// To avoid this, just make sure that the destination to copy to is inside the first chunk.
	
	// For each ON bit at some position j in the previous column's first chunk,
	// turn the bit at j + 3^newColPowerOf3 ON in the new column's first chunk.
	int shift = threeToThe(newColPowerOf3);
	for (int j = 1; (j + shift) < CHUNK_BITS; j++) {
		if ((prevColFirstChunk & (1ULL << j)) != 0) {
			newColFirstChunk |= (1ULL << (j + shift));
		}
	}
	
	// Then for each ON bit at some position j in the new column's first chunk,
	// turn the bit at 2*j ON in the same column's first chunk.
	for (int j = 1; j * 2 < CHUNK_BITS; j++) {
		if ((newColFirstChunk & (1ULL << j)) != 0) {
			newColFirstChunk |= (1ULL << (j * 2));
		}
	}
	
	return newColFirstChunk;
}

// void copyAlongByPowerOfThree(vector<struct expansionRegisterColumn> *expReg, int colNum, uint64_t chunkContents, uint64_t startSize) {
void copyAlongByPowerOfThree(uint64_t* prevExpRegCol, uint64_t* newExpRegCol, uint64_t sourceChunkNum, int powOf3, uint64_t colLength) {
	uint64_t pow = threeToThe(powOf3);
	uint64_t offset = pow % CHUNK_BITS;
	
	uint64_t destChunk1Num = sourceChunkNum + (pow - offset)/CHUNK_BITS;
	uint64_t destChunk2Num = destChunk1Num + 1;
	
	if (destChunk1Num < colLength) newExpRegCol[destChunk1Num] |= prevExpRegCol[sourceChunkNum] << offset;
	if (destChunk2Num < colLength) newExpRegCol[destChunk2Num] |= prevExpRegCol[sourceChunkNum] >> (CHUNK_BITS - offset);
}

void copyAlongToDoubleCurrentPos(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t colLength) {
	uint64_t dummyDest1 = 0;
	uint64_t dummyDest2 = 0;
	
	uint64_t* destChunk1 = sourceChunkNum * 2     < colLength ? (expRegCol + sourceChunkNum * 2    ) : &dummyDest1;
	uint64_t* destChunk2 = sourceChunkNum * 2 + 1 < colLength ? (expRegCol + sourceChunkNum * 2 + 1) : &dummyDest2;
	
	spreadAndOrBits(expRegCol[sourceChunkNum], destChunk1, destChunk2);
}

// Based on https://stackoverflow.com/a/26639774/4149474
// and https://stackoverflow.com/a/239307/4149474
// Easier to just do this than keep reallocating the arrays etc. That approach may also limit how much of the
// memory we can use (as we have to copy stuff, so need both the old and new array in memory at once).
uint64_t estimateMemAvailable()
{
	// Allocate chunks of memory, doubling in size each time
	// When that fails, halve once to get back to what was successful,
	// then start adding an extra block ontop of what was successful,
	// doubling the size of the extra block each time.
	// Keep repeating that until an extra block of size 1 fails.
	// That could be either due to the memory limit being reached,
	// or the memory limit shrinking during execution - either is fine.
	uint64_t successfulSize = 0;
	for (uint64_t extraSize = 1; ; extraSize *= 2) {
		uint64_t* arr = new (std::nothrow) uint64_t[successfulSize + extraSize]();
		
		if (arr == NULL) {
			if (extraSize == 1) return successfulSize;
			
			successfulSize += extraSize / 2;
			extraSize = 1;
		}
		
		delete[] arr;
	}
}

void findAndPrintZeros() {
	//uint64_t estimatedMem = estimateMemAvailable();
	uint64_t estimatedMem = 10;
	
	// Use 90% of the approx. available memory, rounded down to a multiple of 3
	// as there's 3 equal length arrays in use at any time.
	uint64_t memToUse = estimatedMem * 9 / 10;
	memToUse -= memToUse % 3;
	
	uint64_t colLength = memToUse / 3;
	
	uint64_t *prevExpRegCol = new uint64_t[colLength]();
	uint64_t *newExpRegCol  = new uint64_t[colLength]();
	uint64_t *colsAggregate = new uint64_t[colLength]();
	
	// zero the arrays
	for (int i = 0; i < colLength; i++) {
		prevExpRegCol[i] = 0;
		newExpRegCol[i] = 0;
		colsAggregate[i] = 0;
	}
	
	// First, run through the columns aggregate, setting every bit at a multiple of 3 to 1
	for (int i = 0; i < colLength; i++) {
		colsAggregate[i] = 0x9249249249249249 >> ((i * CHUNK_BITS) % 3);
		// ^ Hex constant is 1001001...1001001
		// The shift moves it so that the ON bits are the bits which
		// correspond to values that are multiples of 3.
	}
	
	// Set column 0's first chunk manually
	// Then for all the next columns, we can use initialiseColFirstChunk()
	prevExpRegCol[0] = 0b00000000'00000000'00000000'00000001'00000000'00000001'00000001'00010110;
	
	// Setup the rest of column 0, i.e. ON at every power of 2:
	for (int i = CHUNK_BITS; i < colLength; i *= 2) {
		prevExpRegCol[i] = 1;
	}
	
	// Fill in each next column, until doing so does nothing
	for (int powOf3 = 1; ; powOf3++) {
		// Mask to check if any bits were set to ON in the whole column.
		// If none were, we're done.
		uint64_t anyBitsSet = 0;
		
		newExpRegCol[0] = initialiseColFirstChunk(prevExpRegCol[powOf3 - 1], powOf3);
		
		for (int chunk = 0; chunk < colLength; chunk++) {
			colsAggregate[chunk] |= newExpRegCol[chunk];
			anyBitsSet |= newExpRegCol[chunk];
			
			copyAlongByPowerOfThree(prevExpRegCol, newExpRegCol, chunk, powOf3, colLength);
			copyAlongToDoubleCurrentPos(newExpRegCol, chunk, colLength);
			
			//if (chunk % 5000 == 0) { // print progress every so often. Don't print too often or flush as either may slow things
			//	cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64);
			//}
		}
		
		cout << "prevCol:\r\n";
		for (int i = 0; i < colLength; i++) {
			printUInt64Bits_cpu(prevExpRegCol[i]);
			cout << "\r\n";
		}
		cout << "\r\n\r\n";
		
		if (!anyBitsSet) {
			delete[] prevExpRegCol;
			delete[] newExpRegCol;
			break;
		}
		
		delete[] prevExpRegCol;
		prevExpRegCol = newExpRegCol;
		newExpRegCol = new uint64_t[colLength]();
	}
	
	// Go through the columns aggregate, checking for any chunks with any zero bits
	for (int chunk = 0; chunk < colLength; chunk++) {
		if (colsAggregate[chunk] != ~0) { // If any bits OFF
			// Then find & print the position of the OFF bits
			for (int i = 0; i < CHUNK_BITS; i++) {
				if ((~colsAggregate[chunk]) & (1ULL << i)) {
					time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
					
					cout << "\r" << "found zero: " << (chunk + i) << " @ " << ctime(&time_now); // ctime() adds a newline
				}
			}
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
	
	//if (argc < 2) return -1;
	
	//uint64_t startSize = strtoull(argv[1], nullptr, 10);
	
	auto start = chrono::system_clock::now();
	time_t start_time = chrono::system_clock::to_time_t(start);
	cout << "Started at: " << ctime(&start_time); // ctime() adds a newline
	cout << endl;
	
	findAndPrintZeros();
	//printExpansionRegister(startSize);
	//printExpansionRegColumn(startSize, 6);
}
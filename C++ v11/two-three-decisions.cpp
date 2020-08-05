#include "math-utils.h"
#include <atomic>
#include <algorithm>
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
		//if (i > 0) cout << ":";
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

uint64_t numToBitPos(uint64_t number) {
	return number - (uint64_t)(number/3) - 1;
}
uint64_t bitPosToNum(uint64_t bitPos) {
	return bitPos + (uint64_t)(bitPos/2) + 1;
}

// the process is more complex for first chunk (as doubling
// can leave you in the same chunk; in later chunks it never does)
uint64_t initialiseColFirstChunk(uint64_t prevColFirstChunk, int newColPowerOf3) {
	//	printUInt64Bits_cpu(prevColFirstChunk);
	//	cout << endl;
	
	uint64_t newColFirstChunk = 0;
	
	// Note: must never shift too far (i.e. >= 64 bits), as that's undefined behaviour and e.g. may wrap around.
	// To avoid this, just make sure that the destination to copy to is inside the first chunk.
	
	// For each ON bit at some position j in the previous column's first chunk,
	// turn the bit represeting j + 3^newColPowerOf3 ON in the new column's first chunk.
	int shift = threeToThe(newColPowerOf3);
	shift = shift / 3 * 2; // adjust for missing multiples of 3
	for (int j = 0; (j + shift) < CHUNK_BITS; j++) {
		if ((prevColFirstChunk & (1ULL << j)) != 0) {
			newColFirstChunk |= (1ULL << (j + shift));
		}
	}
	
	//	printUInt64Bits_cpu(newColFirstChunk);
	//	cout << endl;
	
	// Then for each ON bit at some position j, representing value n,
	// in the new column's first chunk, turn the bit representing 2*n ON
	// in the same column's first chunk.
	for (int j = 0; true; j++) {
		int n = bitPosToNum(j);
		
		int doubleN = n * 2; // number to mark as ON
		int doubleNBit = numToBitPos(doubleN); // position of bit representing that number
		
		//	cout << "j: " << j << ", n: " << n << ", doubleN: " << doubleN << ", doubleNBit: " << doubleNBit << ", " << ((newColFirstChunk & (1ULL << j)) != 0) << "." << endl;
		if (doubleNBit >= CHUNK_BITS) break;
		
		if ((newColFirstChunk & (1ULL << j)) != 0) {
			newColFirstChunk |= (1ULL << doubleNBit);
		}
	}
	//	for (int j = 0, n = 1; true; j++, n++) {
	//		if (n % 3 == 0) n++; // skip multiples of 3 for n, but not for j
	//		
	//		int doubleN = n * 2; // number to mark as ON
	//		int doubleNBit = numToBitPos(doubleN); // position of bit representing that number
	//		
	//		if (doubleNBit >= CHUNK_BITS) break;
	//		
	//		if ((newColFirstChunk & (1ULL << j)) != 0) {
	//			newColFirstChunk |= (1ULL << doubleNBit);
	//		}
	//	}
	
	return newColFirstChunk;
}

// void copyAlongByPowerOfThree(vector<struct expansionRegisterColumn> *expReg, int colNum, uint64_t chunkContents, uint64_t startSize) {
void copyAlongByPowerOfThree(uint64_t* prevExpRegCol, uint64_t* newExpRegCol, uint64_t sourceChunkNum, uint64_t computedPowOf3, uint64_t colLength) {
	uint64_t shift = computedPowOf3 / 3 * 2; // adjust for missing multiples of 3
	uint64_t offset = shift % CHUNK_BITS;
	
	uint64_t destChunk1Num = sourceChunkNum + (shift - offset)/CHUNK_BITS;
	uint64_t destChunk2Num = destChunk1Num + 1;
	
	if (destChunk1Num < colLength) newExpRegCol[destChunk1Num] |= prevExpRegCol[sourceChunkNum] << offset;
	if (destChunk2Num < colLength) newExpRegCol[destChunk2Num] |= prevExpRegCol[sourceChunkNum] >> (CHUNK_BITS - offset);
}

void copyAlongToDoubleCurrentPos(uint64_t* expRegCol, uint64_t sourceChunkNum, uint64_t colLength) {
	uint64_t dummyDest1 = 0;
	uint64_t dummyDest2 = 0;
	
	uint64_t* destChunk1 = sourceChunkNum * 2     < colLength ? (expRegCol + sourceChunkNum * 2    ) : &dummyDest1;
	uint64_t* destChunk2 = sourceChunkNum * 2 + 1 < colLength ? (expRegCol + sourceChunkNum * 2 + 1) : &dummyDest2;
	
	spreadAndOrBits_noMult3(expRegCol[sourceChunkNum], destChunk1, destChunk2);
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

//	void aggregateAndPrint(int powOf3, uint64_t chunkNum, uint64_t* newExpRegCol, uint64_t* colsAggregate, uint64_t* anyBitsSet) {
//		*anyBitsSet |= newExpRegCol[chunkNum];
//		colsAggregate[chunkNum] |= newExpRegCol[chunkNum];
//		
//		if (chunkNum % 50000 == 0) { // print progress every so often. Don't print too often or flush as either may slow things
//			cout << "\r" << "at: " << powOf3 << ", " << (chunkNum * 64);
//		}
//	}

void findAndPrintZeros() {
	//uint64_t estimatedMem = estimateMemAvailable();
	uint64_t estimatedMem = 200000000L;
	
	// Use 90% of the approx. available memory, rounded down to a multiple of 3
	// as there's 3 equal length arrays in use at any time.
	uint64_t memToUse = estimatedMem * 9 / 10;
	memToUse -= memToUse % 3;
	
	uint64_t colLength = memToUse / 3;
	//uint64_t colLength = 1;
	
	uint64_t *prevExpRegCol = new uint64_t[colLength]();
	uint64_t *newExpRegCol  = new uint64_t[colLength]();
	uint64_t *colsAggregate = new uint64_t[colLength]();
	
	// zero the arrays
	for (uint64_t i = 0; i < colLength; i++) {
		prevExpRegCol[i] = 0;
		newExpRegCol[i] = 0;
		colsAggregate[i] = 0;
	}
	
	//	// First, run through the columns aggregate, setting every bit at a multiple of 3 to 1
	//	for (uint64_t i = 0; i < colLength; i++) {
	//		colsAggregate[i] = 0x9249249249249249 >> (i % 3);
	//		// ^ Hex constant is 1001001...1001001
	//		// The shift moves it so that the ON bits are the bits which
	//		// correspond to values that are multiples of 3.
	//		// i should really first be multiplied by CHUNK_BITS, but for
	//		// large numbers that causes issues (compiler warned of underfined
	//		// behaviour) and when taking mod 3 it doesn't make a difference
	//	}
	
	// Set column 0's first chunk manually
	// Then for all the next columns, we can use initialiseColFirstChunk()
	// prevExpRegCol[0] = 0b00000000'00000000'00000000'00000001'00000000'00000001'00000001'00010110;
	// prevExpRegCol[0] = 0b00000000'00000000'00000100'00000000'00000000'00100000'00000100'00100111;
	
	// Setup column 0, i.e. ON at every power of 2, adjusted for missing multiples of 3:
	for (uint64_t i = 1; i < colLength * CHUNK_BITS; i *= 2) {
		uint64_t bitPos = numToBitPos(i);
		prevExpRegCol[bitPos / CHUNK_BITS] |= 1ULL << (bitPos % CHUNK_BITS);
	}
	
	// Overlay column 0 onto the aggregate
	for (uint64_t i = 0; i < colLength; i++) {
		colsAggregate[i] |= prevExpRegCol[i];
	}
	
	//	cout << "aggregate\r\n";
	//	for (int i = 0; i < colLength; i++) {
	//		printUInt64Bits_cpu(colsAggregate[i]);
	//		cout << "\r\n";
	//	}
	//	cout << "\r\n\r\n";
	
	// Fill in each next column, until doing so does nothing
	for (int powOf3 = 1; ; powOf3++) {
		// Mask to check if any bits were set to ON in the whole column.
		// If none were, we're done.
		uint64_t anyBitsSet = 0;
		
		newExpRegCol[0] = initialiseColFirstChunk(prevExpRegCol[0], powOf3);
		
		uint64_t computedPow = threeToThe(powOf3);
		
		uint64_t shiftAmount = (computedPow / 3 * 2) / 64;
		uint64_t lastChunkToShift = shiftAmount > colLength ? 0 : colLength - shiftAmount;
		// ^ handle large shiftAmount manually, as we're using unsigned ints.
		// Using 0 isn't technically perfect but has no impact
		
		uint64_t lastChunkToDouble = colLength / 2;
		uint64_t firstLimit = min(lastChunkToShift, lastChunkToDouble);
		
		// Note: The doubling-operation can't affect the current chunk (after the first chunk,
		// which we've done), but the add-power-of-3 operation can (eg. when shifting by 9),
		// so we must only double & aggregate after that.
		// Note: Don't print progress too often, or flush, as either may slow things
		// Chose a power of 2 as the interval to possibly be nice to the branch predictor etc.
		
		#define aggregateAndPrint() { \
			anyBitsSet |= newExpRegCol[chunk]; \
			colsAggregate[chunk] |= newExpRegCol[chunk]; \
			if (chunk % 65536 == 0) { \
				cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64); \
			} \
		}
		
		uint64_t chunk = 0;
		for (; chunk < colLength; chunk++) {
			copyAlongByPowerOfThree(prevExpRegCol, newExpRegCol, chunk, computedPow, colLength);
			copyAlongToDoubleCurrentPos(newExpRegCol, chunk, colLength);
			//aggregateAndPrint(powOf3, chunk, newExpRegCol, colsAggregate, &anyBitsSet);
			aggregateAndPrint();
		}
		
		for (; chunk <= lastChunkToDouble; chunk++) {
			copyAlongToDoubleCurrentPos(newExpRegCol, chunk, colLength);
			//aggregateAndPrint(powOf3, chunk, newExpRegCol, colsAggregate, &anyBitsSet);
			aggregateAndPrint();
		}
		
		//	cout << endl << "lastChunkToShift: " << lastChunkToShift << ", lastChunkToDouble: " << lastChunkToDouble << ", firstLimit: " << firstLimit << ", colLength: " << colLength << ", chunk: " << chunk << endl;
		
		for (; chunk <= lastChunkToShift; chunk++) {
			copyAlongByPowerOfThree(prevExpRegCol, newExpRegCol, chunk, computedPow, colLength);
			//aggregateAndPrint(powOf3, chunk, newExpRegCol, colsAggregate, &anyBitsSet);
			aggregateAndPrint();
		}
		
		#undef aggregateAndPrint
		
		//	for (uint64_t chunk = 0; chunk < colLength; chunk++) {
		//		if (chunk + (computedPow / 3 * 2) / 64 < colLength) //+10 to be safe (TODO: Improve)
		//			copyAlongByPowerOfThree(prevExpRegCol, newExpRegCol, chunk, computedPow, colLength);
		//		if (chunk * 2 < colLength)
		//			copyAlongToDoubleCurrentPos(newExpRegCol, chunk, colLength);
		//		
		//		// The doubling-operation can't affect the current chunk (after the first chunk, which we've done),
		//		// but the add-power-of-3 operation can (eg. when shifting by 9), so do these after (and double after).
		//		anyBitsSet |= newExpRegCol[chunk];
		//		colsAggregate[chunk] |= newExpRegCol[chunk];
		//		
		//		if (chunk % 50000 == 0) { // print progress every so often. Don't print too often or flush as either may slow things
		//			cout << "\r" << "at: " << powOf3 << ", " << (chunk * 64);
		//		}
		//	}
		
		//	cout << "prevCol:\r\n";
		//	for (int i = 0; i < colLength; i++) {
		//		printUInt64Bits_cpu(prevExpRegCol[i]);
		//		cout << "\r\n";
		//	}
		//	cout << "newCol:\r\n";
		//	for (int i = 0; i < colLength; i++) {
		//		printUInt64Bits_cpu(newExpRegCol[i]);
		//		cout << "\r\n";
		//	}
		//	cout << "aggregate\r\n";
		//	for (int i = 0; i < colLength; i++) {
		//		printUInt64Bits_cpu(colsAggregate[i]);
		//		cout << "\r\n";
		//	}
		//	cout << "\r\n\r\n";
		
		time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
		cout << "\rFinished column for shift of 3^" << powOf3 << " @ " << ctime(&time_now); // ctime() adds a newline
		//	for (uint64_t i = colLength - 500; i < colLength; i++) {
		//		printUInt64Bits_cpu(prevExpRegCol[i]);
		//		cout << "\r\n";
		//	}
		
		if (!anyBitsSet) {
			delete[] prevExpRegCol;
			delete[] newExpRegCol;
			break;
		}
		
		// Move newExpRegCol to be used as prevExpRegCol,
		// but reuse the old prev array by clearing it first,
		// rather than deleting & reallocating.
		uint64_t* tmp = prevExpRegCol;
		prevExpRegCol = newExpRegCol;
		newExpRegCol = tmp;
		for (int i = 0; i < colLength; i++) {
			newExpRegCol[i] = 0;
		}
	}
	
	// Go through the columns aggregate, checking for any chunks with any zero bits
	for (uint64_t chunk = 0; chunk < colLength; chunk++) {
		if (~colsAggregate[chunk] != 0) { // If any bits OFF
			// Then find & print the position of the OFF bits
			for (uint64_t i = 0; i < CHUNK_BITS; i++) {
				if ((~colsAggregate[chunk]) & (1ULL << i)) {
					time_t time_now = chrono::system_clock::to_time_t(chrono::system_clock::now());
					
					cout << "\r" << "found zero: " << bitPosToNum(chunk * 64 + i) << "\r\n";
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
	
	//	uint64_t a = 0;
	//	uint64_t b = 0;
	//	uint64_t c = 0b11111111;
	//	
	//	printUInt64Bits_cpu(spreadAndOrBits
	
	//	printUInt64Bits_cpu(initialiseColFirstChunk(0b00000000'00000000'00000100'00000000'00000000'00100000'00000100'00100111, 1));
	//	cout << endl;
	//	return 0;
	
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
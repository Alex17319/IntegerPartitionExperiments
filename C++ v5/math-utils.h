#ifndef MATH_UTILS_H
#define MATH_UTILS_H

extern int math_power_copy;
extern unsigned long long math_n_copy;

extern unsigned long long threePowers[40];
extern char floorLog2Lookup_64bit[64];

#define threeToThe(power) ( \
	math_power_copy = (power), \
	math_power_copy >= 40 \
	? (cout << "Error: overflow in 3^n function" << endl, exit(-1), 0) \
	: \
	threePowers[math_power_copy] \
)

#define floorLog2_64bit(n) ( \
	math_n_copy = (n), \
	math_n_copy |= math_n_copy >> 1, \
	math_n_copy |= math_n_copy >> 2, \
	math_n_copy |= math_n_copy >> 4, \
	math_n_copy |= math_n_copy >> 8, \
	math_n_copy |= math_n_copy >> 16, \
	math_n_copy |= math_n_copy >> 32, \
	floorLog2Lookup_64bit[(math_n_copy * 0x03f6eaf2cd271461) >> 58] \
)

// input:   1  2  4  5  7  8  10 11 13 14 16 17 19 20 22 23 25 26 28 29 31 ...
// maps to: 0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 ...
#define mapToAvoidMult3s(n) \
	((n) - (unsigned long long)((n) / 3) - 1)

#endif
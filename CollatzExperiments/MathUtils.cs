using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CollatzExperiments
{
	public static class MathUtils
	{
		public static BigInteger BigIntTwoToThe(int power) => BigInteger.One << power;
		public static int IntTwoToThe(int power) => ((int)1) << power;
		public static long LongTwoToThe(int power) => ((long)1) << power;

		public static BigInteger BigIntThreeToThe(int power) => power < someBigIntThreePowers.Length ? someBigIntThreePowers[power] : BigInteger.Pow(3, power);
		public static int IntThreeToThe(int power) => intThreePowers[power]; //out of bounds error if overflow, good enough
		public static long LongThreeToThe(int power) => longThreePowers[power]; //out of bounds error if overflow, good enough

		//Numbers from: http://www.quadibloc.com/crypto/t3.htm
		private readonly static int[] intThreePowers = {
			1, 3, 9, 27, 81, 243, 729, 2187, 6561, 19683, 59049, 177147, 531441, 1594323, 4782969, 14348907, 43046721, 129140163, 387420489, 1162261467
		};
		private readonly static long[] longThreePowers = {
			1L, 3L, 9L, 27L, 81L, 243L, 729L, 2187L, 6561L, 19683L, 59049L, 177147L, 531441L, 1594323L, 4782969L, 14348907L, 43046721L, 129140163L,
			387420489L, 1162261467L, 3486784401L, 10460353203L, 31381059609L, 94143178827L, 282429536481L, 847288609443L, 2541865828329L, 7625597484987L,
			22876792454961L, 68630377364883L, 205891132094649L, 617673396283947L, 1853020188851841L, 5559060566555523L, 16677181699666569L,
			50031545098999707L, 150094635296999121L, 450283905890997363L, 1350851717672992089L, 4052555153018976267L
		};
		private readonly static BigInteger[] someBigIntThreePowers = {
			BigInteger.Parse("1"), BigInteger.Parse("3"), BigInteger.Parse("9"), BigInteger.Parse("27"), BigInteger.Parse("81"), BigInteger.Parse("243"),
			BigInteger.Parse("729"), BigInteger.Parse("2187"), BigInteger.Parse("6561"), BigInteger.Parse("19683"), BigInteger.Parse("59049"),
			BigInteger.Parse("177147"), BigInteger.Parse("531441"), BigInteger.Parse("1594323"), BigInteger.Parse("4782969"), BigInteger.Parse("14348907"),
			BigInteger.Parse("43046721"), BigInteger.Parse("129140163"), BigInteger.Parse("387420489"), BigInteger.Parse("1162261467"),
			BigInteger.Parse("3486784401"), BigInteger.Parse("10460353203"), BigInteger.Parse("31381059609"), BigInteger.Parse("94143178827"),
			BigInteger.Parse("282429536481"), BigInteger.Parse("847288609443"), BigInteger.Parse("2541865828329"), BigInteger.Parse("7625597484987"),
			BigInteger.Parse("22876792454961"), BigInteger.Parse("68630377364883"), BigInteger.Parse("205891132094649"), BigInteger.Parse("617673396283947"),
			BigInteger.Parse("1853020188851841"), BigInteger.Parse("5559060566555523"), BigInteger.Parse("16677181699666569"),
			BigInteger.Parse("50031545098999707"), BigInteger.Parse("150094635296999121"), BigInteger.Parse("450283905890997363"),
			BigInteger.Parse("1350851717672992089"), BigInteger.Parse("4052555153018976267"), BigInteger.Parse("12157665459056928801"),
			BigInteger.Parse("36472996377170786403"), BigInteger.Parse("109418989131512359209"), BigInteger.Parse("328256967394537077627"),
			BigInteger.Parse("984770902183611232881"), BigInteger.Parse("2954312706550833698643"), BigInteger.Parse("8862938119652501095929"),
			BigInteger.Parse("26588814358957503287787"), BigInteger.Parse("79766443076872509863361"), BigInteger.Parse("239299329230617529590083"),
			BigInteger.Parse("717897987691852588770249"), BigInteger.Parse("2153693963075557766310747"), BigInteger.Parse("6461081889226673298932241"),
			BigInteger.Parse("19383245667680019896796723"), BigInteger.Parse("58149737003040059690390169"), BigInteger.Parse("174449211009120179071170507"),
			BigInteger.Parse("523347633027360537213511521"), BigInteger.Parse("1570042899082081611640534563"),
			BigInteger.Parse("4710128697246244834921603689"), BigInteger.Parse("14130386091738734504764811067"),
			BigInteger.Parse("42391158275216203514294433201"), BigInteger.Parse("127173474825648610542883299603"),
			BigInteger.Parse("381520424476945831628649898809"), BigInteger.Parse("1144561273430837494885949696427"),
			BigInteger.Parse("3433683820292512484657849089281"), BigInteger.Parse("10301051460877537453973547267843"),
			BigInteger.Parse("30903154382632612361920641803529"), BigInteger.Parse("92709463147897837085761925410587"),
			BigInteger.Parse("278128389443693511257285776231761"), BigInteger.Parse("834385168331080533771857328695283"),
			BigInteger.Parse("2503155504993241601315571986085849"), BigInteger.Parse("7509466514979724803946715958257547"),
			BigInteger.Parse("22528399544939174411840147874772641"), BigInteger.Parse("67585198634817523235520443624317923"),
			BigInteger.Parse("202755595904452569706561330872953769"), BigInteger.Parse("608266787713357709119683992618861307"),
			BigInteger.Parse("1824800363140073127359051977856583921"), BigInteger.Parse("5474401089420219382077155933569751763"),
			BigInteger.Parse("16423203268260658146231467800709255289"), BigInteger.Parse("49269609804781974438694403402127765867"),
			BigInteger.Parse("147808829414345923316083210206383297601"), BigInteger.Parse("443426488243037769948249630619149892803"),
			BigInteger.Parse("1330279464729113309844748891857449678409"), BigInteger.Parse("3990838394187339929534246675572349035227")
		};

		//Based on https://stackoverflow.com/a/23000588/4149474 which is based on https://stackoverflow.com/a/11398748/4149474
		//which is based on https://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
		//"It's correct for all inputs except 0. It returns 0 for 0 which may be valid for what you're using it for. The lines
		//with the shifts round n up to 1 less than the next power of 2. It basically sets all bits after the leading 1 bit to 1.
		//This reduces all possible inputs to 64 possible values: 0x0, 0x1, 0x3, 0x7, 0xf, 0x1f, 0x3f, etc. Multiplying those 64
		//values with the number 0x03f6eaf2cd271461 gives you another 64 unique values in the top 6 bits. The shift by 58 just
		//positions those 6 bits for use as an index into table."
		//Also, "0x03f6eaf2cd271461 is a De Bruijn sequence" (nwellnhof 2017)
		//TODO: Test if this is actually faster than just using floating point inbuilt methods
		public static byte FloorLog2(ulong n)
		{
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			n |= n >> 32;

			return longFloorLog2Lookup[(n * 0x03f6eaf2cd271461) >> 58];
		}
		private static readonly byte[] longFloorLog2Lookup = new byte[64] {
			0, 58, 1, 59, 47, 53, 2, 60, 39, 48, 27, 54, 33, 42, 3, 61,
			51, 37, 40, 49, 18, 28, 20, 55, 30, 34, 11, 43, 14, 22, 4, 62,
			57, 46, 52, 38, 26, 32, 41, 50, 36, 17, 19, 29, 10, 13, 21, 56,
			45, 25, 31, 35, 16, 9, 12, 44, 24, 15, 8, 23, 7, 6, 5, 63
		};
		//And similarly, this time more directly based on https://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
		public static byte FloorLog2(uint n)
		{
			// first round down to one less than a power of 2 
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;

			return intFloorLog2Lookup[(n * 0x07C4ACDDU) >> 27];
		}
		private static readonly byte[] intFloorLog2Lookup = new byte[32] {
		  0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
		  8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
		};

		//<summary>Returns 3^0 + 3^1 + 3^2 + ... + 3^maxPower</summary>
		public static BigInteger BigIntSumPowersOfThreeTo(int maxPower) => (BigIntThreeToThe(maxPower + 1) - 1)/2;
		//<summary>Returns 2^0 + 2^1 + 2^2 + ... + 2^maxPower</summary>
		public static BigInteger BigIntSumPowersOfTwoTo(int maxPower) => BigIntTwoToThe(maxPower + 1) - 1;

		//<summary>Returns 3^0 + 3^1 + 3^2 + ... + 3^maxPower</summary>
		public static long LongSumPowersOfThreeTo(int maxPower) => (LongThreeToThe(maxPower + 1) - 1)/2;
		//<summary>Returns 2^0 + 2^1 + 2^2 + ... + 2^maxPower</summary>
		public static long LongSumPowersOfTwoTo(int maxPower) => LongTwoToThe(maxPower + 1) - 1;

		//<summary>Returns 3^0 + 3^1 + 3^2 + ... + 3^maxPower</summary>
		public static int IntSumPowersOfThreeTo(int maxPower) => (IntThreeToThe(maxPower + 1) - 1)/2;
		//<summary>Returns 2^0 + 2^1 + 2^2 + ... + 2^maxPower</summary>
		public static int IntSumPowersOfTwoTo(int maxPower) => IntTwoToThe(maxPower + 1) - 1;

		//a^b for 0 <= a <= 20 and 0 <= b <= 20
		private static readonly long[][] _someLongPowers = {
			new long[] { int.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
			new long[] { 1 , 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
			new long[] { 1 , 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 },
			new long[] { 1, 3, 9, 27, 81, 243, 729, 2187, 6561, 19683, 59049, 177147, 531441, 1594323, 4782969, 14348907, 43046721, 129140163, 387420489, 1162261467, 3486784401 },
			new long[] { 1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144, 1048576, 4194304, 16777216, 67108864, 268435456, 1073741824, 4294967296, 17179869184, 68719476736, 274877906944, 1099511627776 },
			new long[] { 1, 5, 25, 125, 625, 3125, 15625, 78125, 390625, 1953125, 9765625, 48828125, 244140625, 1220703125, 6103515625, 30517578125, 152587890625, 762939453125, 3814697265625, 19073486328125, 95367431640625 },
			new long[] { 1, 6, 36, 216, 1296, 7776, 46656, 279936, 1679616, 10077696, 60466176, 362797056, 2176782336, 13060694016, 78364164096, 470184984576, 2821109907456, 16926659444736, 101559956668416, 609359740010496, 3656158440062976 },
			new long[] { 1, 7, 49, 343, 2401, 16807, 117649, 823543, 5764801, 40353607, 282475249, 1977326743, 13841287201, 96889010407, 678223072849, 4747561509943, 33232930569601, 232630513987207, 1628413597910449, 11398895185373143, 79792266297612001 },
			new long[] { 1, 8, 64, 512, 4096, 32768, 262144, 2097152, 16777216, 134217728, 1073741824, 8589934592, 68719476736, 549755813888, 4398046511104, 35184372088832, 281474976710656, 2251799813685248, 18014398509481984, 144115188075855872, 1152921504606846976 },
			new long[] { 1, 9, 81, 729, 6561, 59049, 531441, 4782969, 43046721, 387420489, 3486784401, 31381059609, 282429536481, 2541865828329, 22876792454961, 205891132094649, 1853020188851841, 16677181699666569, 150094635296999121, 1350851717672992089 },
			new long[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000, 10000000000, 100000000000, 1000000000000, 10000000000000, 100000000000000, 1000000000000000, 10000000000000000, 100000000000000000, 1000000000000000000 },
			new long[] { 1, 11, 121, 1331, 14641, 161051, 1771561, 19487171, 214358881, 2357947691, 25937424601, 285311670611, 3138428376721, 34522712143931, 379749833583241, 4177248169415651, 45949729863572161, 505447028499293771, 5559917313492231481 },
			new long[] { 1, 12, 144, 1728, 20736, 248832, 2985984, 35831808, 429981696, 5159780352, 61917364224, 743008370688, 8916100448256, 106993205379072, 1283918464548864, 15407021574586368, 184884258895036416, 2218611106740436992 },
			new long[] { 1, 13, 169, 2197, 28561, 371293, 4826809, 62748517, 815730721, 10604499373, 137858491849, 1792160394037, 23298085122481, 302875106592253, 3937376385699289, 51185893014090757, 665416609183179841, 8650415919381337933 },
			new long[] { 1, 14, 196, 2744, 38416, 537824, 7529536, 105413504, 1475789056, 20661046784, 289254654976, 4049565169664, 56693912375296, 793714773254144, 11112006825558016, 155568095557812224, 2177953337809371136 },
			new long[] { 1, 15, 225, 3375, 50625, 759375, 11390625, 170859375, 2562890625, 38443359375, 576650390625, 8649755859375, 129746337890625, 1946195068359375, 29192926025390625, 437893890380859375, 6568408355712890625 },
			new long[] { 1, 16, 256, 4096, 65536, 1048576, 16777216, 268435456, 4294967296, 68719476736, 1099511627776, 17592186044416, 281474976710656, 4503599627370496, 72057594037927936, 1152921504606846976 },
			new long[] { 1, 17, 289, 4913, 83521, 1419857, 24137569, 410338673, 6975757441, 118587876497, 2015993900449, 34271896307633, 582622237229761, 9904578032905937, 168377826559400929, 2862423051509815793 },
			new long[] { 1, 18, 324, 5832, 104976, 1889568, 34012224, 612220032, 11019960576, 198359290368, 3570467226624, 64268410079232, 1156831381426176, 20822964865671168, 374813367582081024, 6746640616477458432 },
			new long[] { 1, 19, 361, 6859, 130321, 2476099, 47045881, 893871739, 16983563041, 322687697779, 6131066257801, 116490258898219, 2213314919066161, 42052983462257059, 799006685782884121 },
			new long[] { 1, 20, 400, 8000, 160000, 3200000, 64000000, 1280000000, 25600000000, 512000000000, 10240000000000, 204800000000000, 4096000000000000, 81920000000000000, 1638400000000000000 }
		};
		private static readonly BigInteger[,] _someBigIntPowers = {
			{ -BigInteger.Parse("1"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0"), BigInteger.Parse("0") },
			{ BigInteger.Parse("1") , BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1"), BigInteger.Parse("1") },
			{ BigInteger.Parse("1") , BigInteger.Parse("2"), BigInteger.Parse("4"), BigInteger.Parse("8"), BigInteger.Parse("16"), BigInteger.Parse("32"), BigInteger.Parse("64"), BigInteger.Parse("128"), BigInteger.Parse("256"), BigInteger.Parse("512"), BigInteger.Parse("1024"), BigInteger.Parse("2048"), BigInteger.Parse("4096"), BigInteger.Parse("8192"), BigInteger.Parse("16384"), BigInteger.Parse("32768"), BigInteger.Parse("65536"), BigInteger.Parse("131072"), BigInteger.Parse("262144"), BigInteger.Parse("524288"), BigInteger.Parse("1048576") },
			{ BigInteger.Parse("1"), BigInteger.Parse("3"), BigInteger.Parse("9"), BigInteger.Parse("27"), BigInteger.Parse("81"), BigInteger.Parse("243"), BigInteger.Parse("729"), BigInteger.Parse("2187"), BigInteger.Parse("6561"), BigInteger.Parse("19683"), BigInteger.Parse("59049"), BigInteger.Parse("177147"), BigInteger.Parse("531441"), BigInteger.Parse("1594323"), BigInteger.Parse("4782969"), BigInteger.Parse("14348907"), BigInteger.Parse("43046721"), BigInteger.Parse("129140163"), BigInteger.Parse("387420489"), BigInteger.Parse("1162261467"), BigInteger.Parse("3486784401") },
			{ BigInteger.Parse("1"), BigInteger.Parse("4"), BigInteger.Parse("16"), BigInteger.Parse("64"), BigInteger.Parse("256"), BigInteger.Parse("1024"), BigInteger.Parse("4096"), BigInteger.Parse("16384"), BigInteger.Parse("65536"), BigInteger.Parse("262144"), BigInteger.Parse("1048576"), BigInteger.Parse("4194304"), BigInteger.Parse("16777216"), BigInteger.Parse("67108864"), BigInteger.Parse("268435456"), BigInteger.Parse("1073741824"), BigInteger.Parse("4294967296"), BigInteger.Parse("17179869184"), BigInteger.Parse("68719476736"), BigInteger.Parse("274877906944"), BigInteger.Parse("1099511627776") },
			{ BigInteger.Parse("1"), BigInteger.Parse("5"), BigInteger.Parse("25"), BigInteger.Parse("125"), BigInteger.Parse("625"), BigInteger.Parse("3125"), BigInteger.Parse("15625"), BigInteger.Parse("78125"), BigInteger.Parse("390625"), BigInteger.Parse("1953125"), BigInteger.Parse("9765625"), BigInteger.Parse("48828125"), BigInteger.Parse("244140625"), BigInteger.Parse("1220703125"), BigInteger.Parse("6103515625"), BigInteger.Parse("30517578125"), BigInteger.Parse("152587890625"), BigInteger.Parse("762939453125"), BigInteger.Parse("3814697265625"), BigInteger.Parse("19073486328125"), BigInteger.Parse("95367431640625") },
			{ BigInteger.Parse("1"), BigInteger.Parse("6"), BigInteger.Parse("36"), BigInteger.Parse("216"), BigInteger.Parse("1296"), BigInteger.Parse("7776"), BigInteger.Parse("46656"), BigInteger.Parse("279936"), BigInteger.Parse("1679616"), BigInteger.Parse("10077696"), BigInteger.Parse("60466176"), BigInteger.Parse("362797056"), BigInteger.Parse("2176782336"), BigInteger.Parse("13060694016"), BigInteger.Parse("78364164096"), BigInteger.Parse("470184984576"), BigInteger.Parse("2821109907456"), BigInteger.Parse("16926659444736"), BigInteger.Parse("101559956668416"), BigInteger.Parse("609359740010496"), BigInteger.Parse("3656158440062976") },
			{ BigInteger.Parse("1"), BigInteger.Parse("7"), BigInteger.Parse("49"), BigInteger.Parse("343"), BigInteger.Parse("2401"), BigInteger.Parse("16807"), BigInteger.Parse("117649"), BigInteger.Parse("823543"), BigInteger.Parse("5764801"), BigInteger.Parse("40353607"), BigInteger.Parse("282475249"), BigInteger.Parse("1977326743"), BigInteger.Parse("13841287201"), BigInteger.Parse("96889010407"), BigInteger.Parse("678223072849"), BigInteger.Parse("4747561509943"), BigInteger.Parse("33232930569601"), BigInteger.Parse("232630513987207"), BigInteger.Parse("1628413597910449"), BigInteger.Parse("11398895185373143"), BigInteger.Parse("79792266297612001") },
			{ BigInteger.Parse("1"), BigInteger.Parse("8"), BigInteger.Parse("64"), BigInteger.Parse("512"), BigInteger.Parse("4096"), BigInteger.Parse("32768"), BigInteger.Parse("262144"), BigInteger.Parse("2097152"), BigInteger.Parse("16777216"), BigInteger.Parse("134217728"), BigInteger.Parse("1073741824"), BigInteger.Parse("8589934592"), BigInteger.Parse("68719476736"), BigInteger.Parse("549755813888"), BigInteger.Parse("4398046511104"), BigInteger.Parse("35184372088832"), BigInteger.Parse("281474976710656"), BigInteger.Parse("2251799813685248"), BigInteger.Parse("18014398509481984"), BigInteger.Parse("144115188075855872"), BigInteger.Parse("1152921504606846976") },
			{ BigInteger.Parse("1"), BigInteger.Parse("9"), BigInteger.Parse("81"), BigInteger.Parse("729"), BigInteger.Parse("6561"), BigInteger.Parse("59049"), BigInteger.Parse("531441"), BigInteger.Parse("4782969"), BigInteger.Parse("43046721"), BigInteger.Parse("387420489"), BigInteger.Parse("3486784401"), BigInteger.Parse("31381059609"), BigInteger.Parse("282429536481"), BigInteger.Parse("2541865828329"), BigInteger.Parse("22876792454961"), BigInteger.Parse("205891132094649"), BigInteger.Parse("1853020188851841"), BigInteger.Parse("16677181699666569"), BigInteger.Parse("150094635296999121"), BigInteger.Parse("1350851717672992089"), BigInteger.Parse("12157665459056928801") },
			{ BigInteger.Parse("1"), BigInteger.Parse("10"), BigInteger.Parse("100"), BigInteger.Parse("1000"), BigInteger.Parse("10000"), BigInteger.Parse("100000"), BigInteger.Parse("1000000"), BigInteger.Parse("10000000"), BigInteger.Parse("100000000"), BigInteger.Parse("1000000000"), BigInteger.Parse("10000000000"), BigInteger.Parse("100000000000"), BigInteger.Parse("1000000000000"), BigInteger.Parse("10000000000000"), BigInteger.Parse("100000000000000"), BigInteger.Parse("1000000000000000"), BigInteger.Parse("10000000000000000"), BigInteger.Parse("100000000000000000"), BigInteger.Parse("1000000000000000000"), BigInteger.Parse("10000000000000000000"), BigInteger.Parse("100000000000000000000") },
			{ BigInteger.Parse("1"), BigInteger.Parse("11"), BigInteger.Parse("121"), BigInteger.Parse("1331"), BigInteger.Parse("14641"), BigInteger.Parse("161051"), BigInteger.Parse("1771561"), BigInteger.Parse("19487171"), BigInteger.Parse("214358881"), BigInteger.Parse("2357947691"), BigInteger.Parse("25937424601"), BigInteger.Parse("285311670611"), BigInteger.Parse("3138428376721"), BigInteger.Parse("34522712143931"), BigInteger.Parse("379749833583241"), BigInteger.Parse("4177248169415651"), BigInteger.Parse("45949729863572161"), BigInteger.Parse("505447028499293771"), BigInteger.Parse("5559917313492231481"), BigInteger.Parse("61159090448414546291"), BigInteger.Parse("672749994932560009201") },
			{ BigInteger.Parse("1"), BigInteger.Parse("12"), BigInteger.Parse("144"), BigInteger.Parse("1728"), BigInteger.Parse("20736"), BigInteger.Parse("248832"), BigInteger.Parse("2985984"), BigInteger.Parse("35831808"), BigInteger.Parse("429981696"), BigInteger.Parse("5159780352"), BigInteger.Parse("61917364224"), BigInteger.Parse("743008370688"), BigInteger.Parse("8916100448256"), BigInteger.Parse("106993205379072"), BigInteger.Parse("1283918464548864"), BigInteger.Parse("15407021574586368"), BigInteger.Parse("184884258895036416"), BigInteger.Parse("2218611106740436992"), BigInteger.Parse("26623333280885243904"), BigInteger.Parse("319479999370622926848"), BigInteger.Parse("3833759992447475122176") },
			{ BigInteger.Parse("1"), BigInteger.Parse("13"), BigInteger.Parse("169"), BigInteger.Parse("2197"), BigInteger.Parse("28561"), BigInteger.Parse("371293"), BigInteger.Parse("4826809"), BigInteger.Parse("62748517"), BigInteger.Parse("815730721"), BigInteger.Parse("10604499373"), BigInteger.Parse("137858491849"), BigInteger.Parse("1792160394037"), BigInteger.Parse("23298085122481"), BigInteger.Parse("302875106592253"), BigInteger.Parse("3937376385699289"), BigInteger.Parse("51185893014090757"), BigInteger.Parse("665416609183179841"), BigInteger.Parse("8650415919381337933"), BigInteger.Parse("112455406951957393129"), BigInteger.Parse("1461920290375446110677"), BigInteger.Parse("19004963774880799438801") },
			{ BigInteger.Parse("1"), BigInteger.Parse("14"), BigInteger.Parse("196"), BigInteger.Parse("2744"), BigInteger.Parse("38416"), BigInteger.Parse("537824"), BigInteger.Parse("7529536"), BigInteger.Parse("105413504"), BigInteger.Parse("1475789056"), BigInteger.Parse("20661046784"), BigInteger.Parse("289254654976"), BigInteger.Parse("4049565169664"), BigInteger.Parse("56693912375296"), BigInteger.Parse("793714773254144"), BigInteger.Parse("11112006825558016"), BigInteger.Parse("155568095557812224"), BigInteger.Parse("2177953337809371136"), BigInteger.Parse("30491346729331195904"), BigInteger.Parse("426878854210636742656"), BigInteger.Parse("5976303958948914397184"), BigInteger.Parse("83668255425284801560576") },
			{ BigInteger.Parse("1"), BigInteger.Parse("15"), BigInteger.Parse("225"), BigInteger.Parse("3375"), BigInteger.Parse("50625"), BigInteger.Parse("759375"), BigInteger.Parse("11390625"), BigInteger.Parse("170859375"), BigInteger.Parse("2562890625"), BigInteger.Parse("38443359375"), BigInteger.Parse("576650390625"), BigInteger.Parse("8649755859375"), BigInteger.Parse("129746337890625"), BigInteger.Parse("1946195068359375"), BigInteger.Parse("29192926025390625"), BigInteger.Parse("437893890380859375"), BigInteger.Parse("6568408355712890625"), BigInteger.Parse("98526125335693359375"), BigInteger.Parse("1477891880035400390625"), BigInteger.Parse("22168378200531005859375"), BigInteger.Parse("332525673007965087890625") },
			{ BigInteger.Parse("1"), BigInteger.Parse("16"), BigInteger.Parse("256"), BigInteger.Parse("4096"), BigInteger.Parse("65536"), BigInteger.Parse("1048576"), BigInteger.Parse("16777216"), BigInteger.Parse("268435456"), BigInteger.Parse("4294967296"), BigInteger.Parse("68719476736"), BigInteger.Parse("1099511627776"), BigInteger.Parse("17592186044416"), BigInteger.Parse("281474976710656"), BigInteger.Parse("4503599627370496"), BigInteger.Parse("72057594037927936"), BigInteger.Parse("1152921504606846976"), BigInteger.Parse("18446744073709551616"), BigInteger.Parse("295147905179352825856"), BigInteger.Parse("4722366482869645213696"), BigInteger.Parse("75557863725914323419136"), BigInteger.Parse("1208925819614629174706176") },
			{ BigInteger.Parse("1"), BigInteger.Parse("17"), BigInteger.Parse("289"), BigInteger.Parse("4913"), BigInteger.Parse("83521"), BigInteger.Parse("1419857"), BigInteger.Parse("24137569"), BigInteger.Parse("410338673"), BigInteger.Parse("6975757441"), BigInteger.Parse("118587876497"), BigInteger.Parse("2015993900449"), BigInteger.Parse("34271896307633"), BigInteger.Parse("582622237229761"), BigInteger.Parse("9904578032905937"), BigInteger.Parse("168377826559400929"), BigInteger.Parse("2862423051509815793"), BigInteger.Parse("48661191875666868481"), BigInteger.Parse("827240261886336764177"), BigInteger.Parse("14063084452067724991009"), BigInteger.Parse("239072435685151324847153"), BigInteger.Parse("4064231406647572522401601") },
			{ BigInteger.Parse("1"), BigInteger.Parse("18"), BigInteger.Parse("324"), BigInteger.Parse("5832"), BigInteger.Parse("104976"), BigInteger.Parse("1889568"), BigInteger.Parse("34012224"), BigInteger.Parse("612220032"), BigInteger.Parse("11019960576"), BigInteger.Parse("198359290368"), BigInteger.Parse("3570467226624"), BigInteger.Parse("64268410079232"), BigInteger.Parse("1156831381426176"), BigInteger.Parse("20822964865671168"), BigInteger.Parse("374813367582081024"), BigInteger.Parse("6746640616477458432"), BigInteger.Parse("121439531096594251776"), BigInteger.Parse("2185911559738696531968"), BigInteger.Parse("39346408075296537575424"), BigInteger.Parse("708235345355337676357632"), BigInteger.Parse("12748236216396078174437376") },
			{ BigInteger.Parse("1"), BigInteger.Parse("19"), BigInteger.Parse("361"), BigInteger.Parse("6859"), BigInteger.Parse("130321"), BigInteger.Parse("2476099"), BigInteger.Parse("47045881"), BigInteger.Parse("893871739"), BigInteger.Parse("16983563041"), BigInteger.Parse("322687697779"), BigInteger.Parse("6131066257801"), BigInteger.Parse("116490258898219"), BigInteger.Parse("2213314919066161"), BigInteger.Parse("42052983462257059"), BigInteger.Parse("799006685782884121"), BigInteger.Parse("15181127029874798299"), BigInteger.Parse("288441413567621167681"), BigInteger.Parse("5480386857784802185939"), BigInteger.Parse("104127350297911241532841"), BigInteger.Parse("1978419655660313589123979"), BigInteger.Parse("37589973457545958193355601") },
			{ BigInteger.Parse("1"), BigInteger.Parse("20"), BigInteger.Parse("400"), BigInteger.Parse("8000"), BigInteger.Parse("160000"), BigInteger.Parse("3200000"), BigInteger.Parse("64000000"), BigInteger.Parse("1280000000"), BigInteger.Parse("25600000000"), BigInteger.Parse("512000000000"), BigInteger.Parse("10240000000000"), BigInteger.Parse("204800000000000"), BigInteger.Parse("4096000000000000"), BigInteger.Parse("81920000000000000"), BigInteger.Parse("1638400000000000000"), BigInteger.Parse("32768000000000000000"), BigInteger.Parse("655360000000000000000"), BigInteger.Parse("13107200000000000000000"), BigInteger.Parse("262144000000000000000000"), BigInteger.Parse("5242880000000000000000000"), BigInteger.Parse("104857600000000000000000000") }
		};

		//	/// <summary>Supports values where 0 &lt;= base &lt; 20 and (0 &lt;= exp &lt; 20 or base == 0 or 1) and where the result fits in the long type. Otherwise, throws an <see cref="IndexOutOfRangeException"/></summary>
		//	public static long LookupLongPow(int @base, int exp) => @base == 0 ? 0 : @base == 1 ? 1 : _someLongPowers[@base][exp];
		//	
		//	/// <summary>Supports values where 0 &lt;= base &lt; 20 and 0 &lt;= exp &lt; 20. Otherwise, throws an <see cref="IndexOutOfRangeException"/></summary>
		//	public static BigInteger LookupBigIntPow(int @base, int exp) => _someBigIntPowers[@base, exp];

		public static long LookupLongPow(int @base, int exp)
		{
			if (@base < _someLongPowers.Length) {
				var arr = _someLongPowers[@base];
				if (exp < arr.Length) {
					return arr[exp];
				}
			}
			return (long)BigInteger.Pow(@base, exp);
		}

		public static BigInteger LookupBigIntPow(int @base, int exp)
		{
			if (@base < _someBigIntPowers.GetLength(0) && exp < _someBigIntPowers.GetLength(1)) {
				return _someBigIntPowers[@base, exp];
			}
			return BigInteger.Pow(@base, exp);
		}

		//Based on https://stackoverflow.com/a/24274850/4149474
		public static bool IsPowerOfThree(uint x) => x != 0 && 3486784401u          % x == 0;
		//public static bool IsPowerOfThree(int x)   => x != 0 && 1162261467           % x == 0; //method below tested as slightly faster, but this may vary
		public static bool IsPowerOfThree(ulong x) => x != 0 && 12157665459056928801 % x == 0;
		public static bool IsPowerOfThree(long x) => x != 0 && 4052555153018976267  % x == 0;

		//Based on https://stackoverflow.com/a/41582594/4149474
		public static bool IsPowerOfThree(int x) => powersOfThreeHashlookupTable[(x ^ (x>>1) ^ (x>>2)) & 31] == x;
		private static int[] powersOfThreeHashlookupTable = new[] {
			1162261467, 1, 3, 729, 14348907, 1, 1, 1,
			1, 1, 19683, 1, 2187, 81, 1594323, 9,
			27, 43046721, 129140163, 1, 1, 531441, 243, 59049,
			177147, 6561, 1, 4782969, 1, 1, 1, 387420489
		};

		//Based on https://stackoverflow.com/a/600306/4149474, then modified to avoid
		//branching (untested whether this is faster)
		public static bool IsPowerOfTwo(uint  x) => (x > 0) & ((x & (x - 1)) == 0);
		public static bool IsPowerOfTwo(int   x) => (x > 0) & ((x & (x - 1)) == 0);
		public static bool IsPowerOfTwo(ulong x) => (x > 0) & ((x & (x - 1)) == 0);
		public static bool IsPowerOfTwo(long  x) => (x > 0) & ((x & (x - 1)) == 0);

		//This is more complex than expected, and here it's easier to sum the sucessive powers of 3 and test that anyway
		//	//TODO: Highest power of 3 less than x. There will only ever be one between sucessive powers of 2,
		//	//so use that to do a lookup.
		//	public static int HighestThreePowAtMost(int x) {
		//		int index = FloorLog2(checked((uint)(x - 1)) + 1);
		//		int inRange;
		//		inRange = floorLog2ToPow3Table[]
		//		//Then if its too big, have to move down an index, possibly twice
		//	}
		//	private static int[] floorLog2ToPow3Table = new[] {
		//		1, 1, 3, 3, 9, 27, 27, 81, 243, 243, 729, 729, 2187, 6561, 6561, 19683, 59049, 59049,
		//		177147, 177147, 531441, 1594323, 1594323, 4782969, 14348907, 14348907, 43046721,
		//		129140163, 129140163, 387420489, 387420489, 1162261467, 1162261467,
		//	};

		//TODO: This may need to be made more efficient; it's could be used frequently
		/// <summary>Finds the largest integer S = 3^0 + 3^1 + 3^2 ... 3^N less than <paramref name="x"/>,
		/// and returns S via <paramref name="sum"/>, 3^N via <paramref name="highestPower"/>,
		/// and N via <paramref name="highestExponent"/></summary>
		//Note: In general if P = B^E, P is the power, B is the base, and E is the exponent
		public static void HighestPow3SumAtMost(long x, out long sum, out long highestPower, out long highestExponent)
		{
			if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x), x, "Must be positive.");

			sum = 1;
			highestExponent = 0;
			highestPower = 1;

			while (true)
			{
				if (sum == x) return;
				else if (sum > x) {
					//Backtrack
					sum -= highestPower;
					highestPower /= 3;
					highestExponent--;
					return;
				}

				highestExponent++;
				highestPower *= 3;
				sum += highestPower;
			}
		}

		//https://stackoverflow.com/a/12175897/4149474
		public static int NumberOfSetBits(int i)
		{
			i = i - ((i >> 1) & 0x55555555);
			i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
			return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
		}
	}
}

//*/
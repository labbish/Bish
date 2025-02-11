namespace Bish {

    internal class BishUnitTest {

        private static void ConditionTest(double count, bool condition, string? message = null) {
            BishUtils.Assert(condition, $"Test {count} Failed: {message}");
            Console.WriteLine($"Test {count} Passed");
        }

        private static void ExpectTest(double count, string[] preInputs, string input
            , dynamic? value) {
            BishProgram program = new();
            foreach (string preInput in preInputs) program.Parse(preInput);
            var result = program.Parse(input);
            ConditionTest(count, result.value == value,
                $"Expected {value}, Returned {result.value}");
        }

        private static void ExpectTest(double count, string input, dynamic? value) {
            ExpectTest(count, Array.Empty<string>(), input, value);
        }

        private static void ExpectVarTest(double count, string[] preInputs, string input
            , string? name, dynamic? value) {
            BishProgram program = new();
            foreach (string preInput in preInputs) program.Parse(preInput);
            var result = program.Parse(input);
            BishVariable expected = new(name, value);
            ConditionTest(count, BishVariable.SameVar(result, expected),
                $"Expected [{expected}], Returned [{result}]");
        }

        private static void ExpectVarTest(double count, string input, string? name, dynamic? value) {
            ExpectVarTest(count, Array.Empty<string>(), input, name, value);
        }

        private static void TestGroup0() {
            ConditionTest(0, true);
        }

        private static void TestGroup1() {
            ExpectTest(1.1, "114514", 114514);
            ExpectTest(1.2, "114.514", 114.514);
            ExpectTest(1.3, "'114514'", "114514");
            ExpectTest(1.4, "\"114514\"", "114514");
        }

        private static void TestGroup2() {
            ExpectTest(2.1, "6+3", 9);
            ExpectTest(2.2, "6-3", 3);
            ExpectTest(2.3, "6*3", 18);
            ExpectTest(2.4, "6/3", 2);
            ExpectTest(2.5, "6^3", 216);
        }

        private static void TestGroup3() {
            ExpectTest(3, "-8/((6-2^3)*4)", 1);
        }

        private static void TestGroup4() {
            ExpectVarTest(4.11, "int x = 3", null, 3);
            ExpectVarTest(4.12, "num x = 3.14", null, 3.14);
            ExpectVarTest(4.13, "string x = '3.14'", null, "3.14");

            ExpectVarTest(4.21, ["int x = 3"], "x", "x", 3);
            ExpectVarTest(4.22, ["int x = 3", "x = 5"], "x", "x", 5);
        }

        public static void TestAll() {
            try {
                TestGroup0();
                TestGroup1();
                TestGroup2();
                TestGroup3();
                TestGroup4();

                ConditionTest(double.PositiveInfinity, false, "End of Program");
            }
            catch (ArgumentException ex) {
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
            }
        }
    }
}
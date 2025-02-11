namespace Bish {

    internal class BishUnitTest {

        private static void Error(double count, string? message = null) {
            ConditionTest(count, false, message);
        }

        private static void ConditionTest(double count, bool condition, string? message = null) {
            BishUtils.Assert(condition, $"Test {count} Failed: {message}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Test {count} Passed");
            Console.ResetColor();
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

        private static void FailTest(double count, string[] preInputs, string input) {
            bool caught = false;
            try {
                BishProgram program = new();
                foreach (string preInput in preInputs) program.Parse(preInput);
                var result = program.Parse(input);
            }
            catch (Exception) {
                caught = true;
            }
            ConditionTest(count, caught, "Expected to Fail, No Exceptions Caught");
        }

        private static void FailTest(double count, string input) {
            FailTest(count, Array.Empty<string>(), input);
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
            ExpectVarTest(4.14, "bool x = true", null, true);
            ExpectVarTest(4.15, "bool x = false", null, false);

            FailTest(4.21, "x");
            ExpectVarTest(4.22, ["int x = 3"], "x", "x", 3);
            ExpectTest(4.23, ["int x = 3"], "x * (x + 1)", 12);
            ExpectVarTest(4.24, ["int x = 3", "x = 5"], "x", "x", 5);
            ExpectVarTest(4.25, ["int x = 3", "x = x + 1"], "x", "x", 4);
            FailTest(4.26, ["int x = 3"], "int x = 5");
            ExpectVarTest(4.27, ["int x", "x = 5"], "x", "x", 5);
            FailTest(4.28, ["int x"], "x = 3.14");
            FailTest(4.29, ["int x"], "int x");
        }

        public static void TestAll() {
            try {
                TestGroup0();
                TestGroup1();
                TestGroup2();
                TestGroup3();
                TestGroup4();

                //Error(double.PositiveInfinity, "End of Program");
            }
            catch (ArgumentException ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
            }
        }
    }
}
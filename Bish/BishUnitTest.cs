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

        private static void TestGroup(int count, string info) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Starting Test Group {count}: {info}");
            Console.ResetColor();
        }

        private static void TestGroup0() {
            TestGroup(0, "test");

            ConditionTest(0, true);
        }

        private static void TestGroup1() {
            TestGroup(1, "types");

            ExpectTest(1.1, "114514", 114514);
            ExpectTest(1.2, "114.514", 114.514);
            ExpectTest(1.3, "'114514'", "114514");
            ExpectTest(1.4, "\"114514\"", "114514");
        }

        private static void TestGroup2() {
            TestGroup(2, "operations");

            ExpectTest(2.11, "6+3", 9);
            ExpectTest(2.12, "6-3", 3);
            ExpectTest(2.13, "6*3", 18);
            ExpectTest(2.14, "6/3", 2);
            ExpectTest(2.15, "6^3", 216);

            ExpectTest(2.21, "+3", 3);
            ExpectTest(2.22, "-3", -3);
            ExpectTest(2.23, "!true", false);
            ExpectTest(2.23, "!false", true);
        }

        private static void TestGroup3() {
            TestGroup(3, "multiple operations");

            ExpectTest(3, "-8/((6-2^3)*4)", 1);
        }

        private static void TestGroup4() {
            TestGroup(4, "vars");

            ExpectVarTest(4.11, "int x = 3", null, 3);
            ExpectVarTest(4.12, "num x = 3.14", null, 3.14);
            ExpectVarTest(4.13, "string x = '3.14'", null, "3.14");
            ExpectVarTest(4.14, "bool x = true", null, true);
            ExpectVarTest(4.15, "bool x = false", null, false);

            ExpectVarTest(4.21, ["int x = 3"], "x", "x", 3);
            ExpectTest(4.22, ["int x = 3"], "x * (x + 1)", 12);
            ExpectVarTest(4.23, ["int x = 3", "x = 5"], "x", "x", 5);
            ExpectVarTest(4.24, ["int x = 3", "x = x + 1"], "x", "x", 4);
            ExpectVarTest(4.25, ["int x", "x = 5"], "x", "x", 5);
            ExpectVarTest(4.26, ["int x = 3", "int y = 5", "x = y = 4"], "x", "x", 4);
            ExpectVarTest(4.27, ["int x = 3", "int y = 5", "x = y = 4"], "y", "y", 4);

            FailTest(4.31, "x");
            FailTest(4.32, ["int x = 3"], "int x = 5");
            FailTest(4.33, ["int x"], "x = 3.14");
            FailTest(4.34, ["int x"], "int x");
            FailTest(4.35, "1=2");

            FailTest(4.41, ["int x"], "x");
            FailTest(4.42, ["int x"], "int y = x");
            ExpectVarTest(4.43, ["int? x"], "x", "x", null);
            ExpectVarTest(4.44, ["int? x = null"], "x", "x", null);
            FailTest(4.45, ["int x"], "x = null");
            FailTest(4.46, ["int? x"], "int y = x");
            ExpectVarTest(4.47, ["int? x", "int? y = x"], "y", "y", null);

            ExpectTest(4.51, ["int x = 5", "x++"], "x", 6);
            ExpectTest(4.52, ["int x = 6", "x--"], "x", 5);
            FailTest(4.53, "12++");
        }

        private static void TestGroup5() {
            TestGroup(5, "multi-sentences");

            ExpectVarTest(5.1, "", null, null);
            ExpectTest(5.2, ["int x = 3; x = x * x"], "x", 9);
            ExpectTest(5.3, ["int x = 3; x = x * x;"], "x", 9);
        }

        private static void TestGroup6() {
            TestGroup(6, "code blocks");

            ExpectTest(6.1, "{int x = 3; x = x * x; x}", 9);
            ExpectTest(6.2, "{int x = 3; x = x * x; x;}", null);
            FailTest(6.3, ["{int x = 3; x = x * x; x;}"], "x");
        }

        public static void TestAll() {
            try {
                TestGroup0();
                TestGroup1();
                TestGroup2();
                TestGroup3();
                TestGroup4();
                TestGroup5();
                TestGroup6();

                if (Program.StopIfTestFinished) {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Test Finished");
                    Console.ResetColor();
                    Environment.Exit(0);
                }
            }
            catch (ArgumentException ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                if (Program.StopIfTestFailed) Environment.Exit(-1);
            }
        }
    }
}
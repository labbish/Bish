﻿namespace Bish {

    internal class BishUnitTest {

        private static void Error(dynamic count, string? message = null) {
            ConditionTest(count, false, message);
        }

        private static void ConditionTest(dynamic count, bool condition, string? message = null) {
            BishUtils.Assert(condition, $"Test {count} Failed: {message}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Test {count} Passed");
            Console.ResetColor();
        }

        private static void ExpectTest(dynamic count, string[] preInputs, string input
            , dynamic? value) {
            BishProgram program = new();
            foreach (string preInput in preInputs) program.Parse(preInput);
            var result = program.Parse(input);
            ConditionTest(count, result.value == value,
                $"Expected {value}, Returned {result.value}");
        }

        private static void ExpectTest(dynamic count, string input, dynamic? value) {
            ExpectTest(count, Array.Empty<string>(), input, value);
        }

        private static void ExpectGroupTest(dynamic count, string[] preInputs,
            string[] inputs, dynamic?[] values) {
            BishUtils.Assert(inputs.Length == values.Length,
                $"ExpectGroupTest {count} Argument Error");
            for (int i = 0; i < inputs.Length; i++) {
                string input = inputs[i];
                dynamic? value = values[i];
                ExpectTest($"{count}.{i + 1}", preInputs, input, value);
            }
        }

        private static void ExpectGroupTest(dynamic count, string[] inputs, dynamic?[] values) {
            ExpectGroupTest(count, Array.Empty<string>(), inputs, values);
        }

        private static void ExpectVarTest(dynamic count, string[] preInputs, string input
            , string? name, dynamic? value) {
            BishProgram program = new();
            foreach (string preInput in preInputs) program.Parse(preInput);
            var result = program.Parse(input);
            BishVariable expected = new(name, value);
            ConditionTest(count, BishVariable.SameVar(result, expected),
                $"Expected [{expected}], Returned [{result}]");
        }

        private static void ExpectVarTest(dynamic count, string input, string? name, dynamic? value) {
            ExpectVarTest(count, Array.Empty<string>(), input, name, value);
        }

        private static void SameTest(dynamic count, string input1, string input2) {
            var result1 = new BishProgram().Parse(input1);
            var result2 = new BishProgram().Parse(input2);
            ConditionTest(count, (result1 == result2).value,
                $"Expected same, Returned ([{result1}] , [{result2}])");
        }

        private static void FailTest(dynamic count, string[] preInputs, string input) {
            bool caught = false;
            BishProgram program = new();
            foreach (string preInput in preInputs) program.Parse(preInput);
            try {
                var result = program.Parse(input);
            }
            catch (Exception) {
                caught = true;
            }
            ConditionTest(count, caught, "Expected to Fail, No Exceptions Caught");
        }

        private static void FailTest(dynamic count, string input) {
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
            TestGroup(1, "literals");

            ExpectTest(1.1, "114514", 114514);
            ExpectTest(1.2, "114.514", 114.514);
            ExpectTest(1.3, "'114\\t514'", "114\t514");
            ExpectTest(1.4, "\"114\\t514\"", "114\t514");
            ExpectTest(1.5, "\"\"\"114\\t514\"\"\"", "114\\t514");
        }

        private static void TestGroup2() {
            TestGroup(2, "operations");

            ExpectTest(2.11, "6+3", 9);
            ExpectTest(2.12, "6-3", 3);
            ExpectTest(2.13, "6*3", 18);
            ExpectTest(2.14, "6/3", 2);
            ExpectTest(2.15, "6%3", 0);
            ExpectTest(2.16, "6^3", 216);

            ExpectTest(2.21, "+3", 3);
            ExpectTest(2.22, "-3", -3);
            ExpectTest(2.23, "!true", false);
            ExpectTest(2.23, "!false", true);

            ExpectGroupTest(2.31,
                ["true && true", "true && false", "false && true", "false && false"],
                [true, false, false, false]);
            ExpectGroupTest(2.32,
                ["true || true", "true || false", "false || true", "false || false"],
                [true, true, true, false]);
            ExpectTest(2.33, ["bool x = false", "true || (x = true)"], "x", false);

            ExpectTest(2.41, "1 > 5", false);
            ExpectTest(2.42, "3 <= 4", true);
            ExpectTest(2.43, "2 != 2", false);
        }

        private static void TestGroup3() {
            TestGroup(3, "multiple operations");

            ExpectTest(3, "-8/((6-2^3)*4)", 1);
        } //Why is this guy still here?

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
            ExpectGroupTest(4.54, ["int x = 2"],
                ["x += 3; x", "x -= 3; x", "x *= 2; x", "x /= 2; x", "x %= 3; x", "x ^= 3; x"],
                [5, -1, 4, 1, 2, 8]);

            FailTest(4.61, ["const int x = 1"], "x = 2");
            FailTest(4.62, ["const int x = 1"], "x += 2");
            FailTest(4.63, ["const int x = 1"], "x++");
            FailTest(4.64, "const int x");
            ExpectTest(4.65, ["const int? x"], "x", null);

            ExpectVarTest(4.71, ["var x = 2"], "x", "x", 2);
            ExpectVarTest(4.72, ["var x = 2", "x = 'hello'"], "x", "x", "hello");
            FailTest(4.73, ["var x"], "x");
            FailTest(4.74, ["var x = 1"], "x = null");
            ExpectVarTest(4.75, ["var? x"], "x", "x", null);
            ExpectVarTest(4.76, ["var? x = null"], "x", "x", null);
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
            ExpectTest(6.4, ["int x = 3", "{x = 5}"], "x", 5);
        }

        private static void TestGroup7() {
            TestGroup(7, "conditions");

            ExpectTest(7.11, "if (true) {3}", 3);
            ExpectTest(7.12, "if (false) {3}", null);
            ExpectTest(7.13, "if (true) {3} else {5}", 3);
            ExpectTest(7.14, "if (false) {3} else {5}", 5);
            ExpectTest(7.15, ["int x = 0", "if (false) {x = 3;}"], "x", 0);
            ExpectTest(7.16, ["int x = 0", "if (true) {x = 3;} else {x = 5;}"], "x", 3);
            ExpectTest(7.17, ["int x = 0", "if (false) {x = 3;} else {x = 5;}"], "x", 5);
            ExpectGroupTest(7.18,
                ["if (true) { if (true) {1} else {2} } else {3}",
                "if (true) { if (false) {1} else {2} } else {3}",
                "if (false) { if (false) {1} else {2} } else {3}", ],
                [1, 2, 3]);

            ExpectTest(7.21, "true ? 3 : 5", 3);
            ExpectTest(7.22, "false ? 3 : 5", 5);
            ExpectTest(7.23, ["int x = 0", "true ? x : x = 1"], "x", 0);
            ExpectTest(7.24, "int x = 0; true ? x : x = 1", 0);
        }

        private static void TestGroup8() {
            TestGroup(8, "loops");

            ExpectTest(8.11, ["int x = 11", "while (x > 0) {x -= 2;} "], "x", -1);
            ExpectTest(8.12, ["int x = 11", "int y = 0",
                "while (x > 0) { x--; y += x; }"], "y", 55);

            ExpectTest(8.21, ["int x = 11", "do {x -= 2} while (x > 0)"], "x", -1);

            ExpectTest(8.31, "for (int i = 0; i < 10; i++) {i}", 10);
            ExpectTest(8.32, ["int j = 0; for (int i = 0; i < 10; i++) {j++}"], "j", 10);
            ExpectTest(8.33, ["int s = 0; for (int i = 0; i < 10; i++) {s += i}"], "s", 45);
            FailTest(8.34, ["for (int i = 0; i < 10; i++) {i}"], "i");

            ExpectTest(8.41, ["int x = 3", "do {jump end; x = 5;} while (false)"], "x", 3);
            ExpectTest(8.42, ["int x = 3", "tag a: do {jump end[a]; x = 5;} while (false)"], "x", 3);
            ExpectTest(8.43, ["int s = 0", "for (int i = 0; i < 10; i++)"
                + "{s += i; if (i % 2 == 0) {jump next;}}"], "s", 20);
            FailTest(8.44, ["while (true) {int i; jump end;}"], "i");
            FailTest(8.45, ["do {int i; jump end;} while (true)"], "i");
            FailTest(8.46, ["for (int i = 0; i < 10; i++) {jump end;}"], "i");
            FailTest(8.47, ["tag a: for (int i = 0; i < 10; i++) {jump end[a]}"], "i");
        }

        private static void TestGroup9() {
            TestGroup(9, "intervals");

            ExpectGroupTest(9.1,
                ["(3,5)", "(3,5]", "[3,5)", "[3,5]"],
                [   new BishInterval(false, 3, false, 5),
                    new BishInterval(false, 3, true, 5),
                    new BishInterval(true, 3, false, 5),
                    new BishInterval(true, 3, true, 5)]);
            SameTest(9.2, "(1,3)+(2,4)", "(1,4)");
            SameTest(9.3, "(1,3)*(2,4)", "(2,3)");
            ExpectGroupTest(9.4, ["interval I = (3,5]"],
                ["2 < I", "3 < I", "4 < I", "5 < I", "6 < I"],
                [false, false, true, true, false]);
            ExpectGroupTest(9.5,
                ["interval I = (3,5]", "interval J = (1,5)", "interval K = (1,7]"],
                ["I <= J", "I <= K", "J <= K"],
                [false, true, true]);
            ExpectGroupTest(9.6,
                ["inf < (-inf,5)", "-inf < (-inf,5)", "-inf < [-inf,5)"],
                [false, false, true]);
            FailTest(9.7, "(5,3)");
        }

        public static void TestGroup10() {
            TestGroup(10, "pattern matching");

            ExpectGroupTest(10.1, ["1 ~ 0", "1 ~ 1", "1 ~ 2"], [false, true, false]);
            ExpectGroupTest(10.2, ["1 ~ null", "null ~ null"], [false, true]);
            ExpectGroupTest(10.3, ["1 !~ 0", "1 !~ 1", "1 !~ 2"], [true, false, true]);
            ExpectGroupTest(10.4, ["1 ~ not 0", "1 ~ not 1", "1 ~ not 2"], [true, false, true]);
            ExpectGroupTest(10.5, ["1 !~ not 0", "1 !~ not 1", "1 !~ not 2"], [false, true, false]);
            ExpectGroupTest(10.6, ["1 ~ <2", "3 ~ <2", "3 ~ >=2", "2 ~ ==2"],
                [true, false, true, true]);
            ExpectGroupTest(10.7,
                ["1 ~ 1 and <2", "1 ~ (>5 and <2)", "1 ~ (>2 or <5) and 1"],
                [true, false, true]);
            ExpectGroupTest(10.8, ["type T = num"],
                ["1 ~ T _", "3.14 ~ T _", "'hi' ~ T _"],
                [true, true, false]);
            ExpectGroupTest(10.9, ["def f(int x) => x == 0"],
                ["1 ~ func f", "0 ~ func f", "0 ~ func func(var _) => false"],
                [false, true, false]);
        }

        public static void TestGroup11() {
            TestGroup(11, "switch-cases");

            List<string> patterns1 = ["0", "==0", "<2", ">1 or <=2", "0 and <2", "int? a",
                "int _", "func func(int a) => a <= 3"];
            for (int i = 0; i < patterns1.Count; i++) {
                string pattern = patterns1[i];
                ExpectTest($"11.1{i + 1}",
                    ["int x = 3", $"switch(0){{case {pattern}: {{x = 5;}}}}"], "x", 5);
            }

            List<string> patterns2 = ["0 and >1", "string _", ">1 or 3"];
            for (int i = 0; i < patterns2.Count; i++) {
                string pattern = patterns2[i];
                ExpectTest($"11.2{i + 1}",
                    ["int x = 3", $"switch(0){{case {pattern}: {{x = 5;}}}}"], "x", 3);
            }

            List<string> patterns3 = ["1", "string _", "!=0"];
            for (int i = 0; i < patterns3.Count; i++) {
                string pattern = patterns3[i];
                ExpectTest($"11.3{i + 1}",
                    ["int x = 3", $"switch(0!){{case {pattern}: {{x = 5;}}}}"], "x", 5);
            }

            List<string> patterns4 = ["1", "string _", "!=0"];
            for (int i = 0; i < patterns4.Count; i++) {
                string pattern = patterns4[i];
                ExpectTest($"11.4{i + 1}",
                    ["int x = 3", $"switch(0){{case not {pattern}: {{x = 5;}}}}"], "x", 5);
            }

            ExpectTest(11.51, ["int x = 3",
                "switch(0){case 1: {x = 5;} case 0: {x = 4;}}"], "x", 4);
            FailTest(11.52, ["switch(0){case int x: {}}"], "x");
            ExpectTest(11.53, ["int x = 3",
                "switch(0){case 0: {x = 4; continue} case 0: {x = 5}}"], "x", 5);
            ExpectTest(11.54, ["int x = 3",
                "switch(0){case 1: {x = 4} default: {x = 5}}"], "x", 5);
        }

        public static void TestGroup12() {
            TestGroup(12, "funcs");

            ExpectTest("12.1.1", ["def f() {0}"], "f()", 0);
            ExpectTest("12.1.2", ["def f() {return 0}"], "f()", 0);
            ExpectTest("12.1.3", ["def f(int x) {return x}"], "f(0)", 0);
            ExpectTest("12.1.4", ["int a = 0", "def f() {return a}"], "f()", 0);
            ExpectTest("12.1.5", ["int a = 1", "def f() {return a}", "a = 0"], "f()", 0);
            ExpectTest("12.1.6", ["int a = 1", "def f() {a = 0}", "f()"], "a", 0);

            FailTest("12.2.1", ["def f(int x) {x}"], "f(null)");
            ExpectTest("12.2.2", ["def f(int? x) {x}"], "f(null)", null);
            FailTest("12.2.3", ["def f(const int x) {x = 1}"], "f(0)");
            ExpectTest("12.2.4", ["int a = 0", "def f(int x) {x = 1}", "f(a)"], "a", 0);
            FailTest("12.2.5", ["def f() {int x = 1}"], "x");

            ExpectTest("12.3.1", ["def f() {return 0}", "def f(int _) {return 1}"], "f()", 0);
            ExpectTest("12.3.2", ["def f() {return 1}", "def f(int _) {return 0}"], "f(1)", 0);
            FailTest("12.3.3", ["def f() {return 1}", "def f() {return 0}"], "f()");
            ExpectTest("12.3.4", ["def f() {return}"], "f()", null);

            ExpectGroupTest("12.4.1", ["def f(int x = 0) {return x}"],
                ["f()", "f(1)"], [0, 1]);
            ExpectGroupTest("12.4.2", ["def f(int x = 0, int y) {return x + y}"],
                ["f(1)", "f(1, 1)"], [1, 2]);
            ExpectGroupTest("12.4.3", ["def f(int x = 0, int y = 0) {return x + y}"],
                ["f()", "f(1, 1)"], [0, 2]);
            FailTest("12.4.4", ["def f(int x = 0, int y = 0) {return x + y}"], "f(1)");
            ExpectGroupTest("12.4.5", ["def f(int x = 0, num y = 0) {return x + y}"],
                ["f()", "f(0.1)", "f(1)", "f(1, 1)"], [0, 0.1, 1, 2]);
            ExpectGroupTest("12.4.6", ["def f(int x = 0, int y = 0) {return x - y}"],
                ["f(x: 1)", "f(y: 1)", "f(x: 2, y: 1)", "f(y: 2, x: 1)",],
                [1, -1, 1, -1]);

            ExpectTest("12.5.1", ["def f() {return 0}", "func g = f"], "g()", 0);
            ExpectTest("12.5.2", ["def f() => 0"], "f()", 0);
            ExpectTest("12.5.3", ["def f(int x) => x + 1"], "f(0)", 1);
            ExpectTest("12.5.4", ["func f = func() {return 0}"], "f()", 0);
            ExpectTest("12.5.5", ["func f = func() => 0"], "f()", 0);
            ExpectTest("12.5.6", ["func f = func(int x) => x + 1"], "f(0)", 1);
            ExpectTest("12.5.7", ["def f(int x) => func(int y) => x * y;"], "f(3)(5)", 15);
            ExpectTest("12.5.8", ["def f(func x) => func(var a) => x(a) * 2",
                "def g(int x) => x + 1"], "f(g)(3)", 8);
            ExpectTest("12.5.9", ["def f(int x) => x <= 0 ? 1 : x * f(x - 1)"], "f(5)", 120);

            ExpectTest("12.6.1", ["type T = int", "def f(T a, T b) => a + b"], "f(1, 2)", 3);
            FailTest("12.6.2", ["type T = int", "def f(T a, T b) => a + b"], "f(1, 2.1)");

            ExpectTest("12.7.1", ["def[int] f() => 0"], "f()", 0);
            ExpectTest("12.7.2", ["def[num] f() => 0"], "f()", 0.0);
            FailTest("12.7.3", ["def[int] f() => 0.1"], "f()");
            ExpectTest("12.7.4", ["func[int] f = func() => 0"], "f()", 0);
            ExpectTest("12.7.5", ["func[num] f = func() => 0"], "f()", 0.0);
            FailTest("12.7.6", ["func[int] f = func() => 0.1"], "f()");
            FailTest("12.7.7", ["def f() => 0", "def f(int _) => 1"], "f");

            ExpectTest("12.8.1", ["def f(int x) where(x == 0) => x"], "f(0)", 0);
            FailTest("12.8.2", ["def f(int x) where(x == 0) => x"], "f(1)");
            ExpectTest("12.8.3", ["func f = func(int x) where(x == 0) => x"], "f(0)", 0);
            FailTest("12.8.4", ["func f = func(int x) where(x == 0) => x"], "f(1)");
            ExpectTest("12.8.5", ["int x = 0", "def g() {x = 1; true}",
                "def f() where(g()) => 0", "f()"], "x", 1);
            ExpectTest("12.8.6", ["def f(int x) where(x == 0) => x",
                "def f(int x) => 1"], "f(1)", 1);

            FailTest("12.9.1", ["def f() => 0"], "f = 0");
            ExpectTest("12.9.2", ["def f() => 0", "f = func() => 1"], "f()", 1);
            FailTest("12.9.3", ["const def f() => 0"], "f = func() => 1");

            ExpectTest("12.10.1", ["def a(func f) => func() => f() + 1",
                "@a def f() => 1"], "f()", 2);
            ExpectTest("12.10.2", ["def a(func f) => func() => f() + 1",
                "@a @a def f() => 1"], "f()", 3);
            ExpectTest("12.10.3", ["def a(func f) => func() => f() + 1",
                "def b(func f) => func() => f() * 2",
                "@a @b def f() => 1"], "f()", 3);
        }

        public static void TestGroup13() {
            TestGroup(13, "types");

            ExpectTest(13.11, ["var[int, string] x = 1", "x = 'hello'"], "x", "hello");
            FailTest(13.12, ["var[int, string] x = 1"], "x = 3.14");
            FailTest(13.13, ["var[int, string] x = 1"], "x = null");
            ExpectTest(13.14, ["var[int?, string] x"], "x", null);
            ExpectTest(13.15, ["var[int, string?] x"], "x", null);

            ExpectTest(13.21, ["type T = int", "T x = 2"], "x", 2);
            FailTest(13.22, ["type T = int"], "T x = 3.14");
            ExpectTest(13.23, ["type T = int|string", "T x = 'hello'"], "x", "hello");
            FailTest(13.24, ["type T = int|string"], "T x = 3.14");

            SameTest(13.31, "var[int]", "int");
            SameTest(13.32, "var[int?]", "int?");
            SameTest(13.33, "var[int?]", "var[int]?");
            SameTest(13.34, "var[int?, var[string|var[num], int], num]", "num|string|int?");
        }

        public static void TestGroup14() {
            TestGroup(14, "classes");

            //class X{int x=0; def X(int a) {x=a;}; def f() => x;}; X x = X(3)
        }

        public static void Test(int? num = null) {
            try {
                if (num is null || num == 0) TestGroup0();
                if (num is null || num == 1) TestGroup1();
                if (num is null || num == 2) TestGroup2();
                if (num is null || num == 3) TestGroup3();
                if (num is null || num == 4) TestGroup4();
                if (num is null || num == 5) TestGroup5();
                if (num is null || num == 6) TestGroup6();
                if (num is null || num == 7) TestGroup7();
                if (num is null || num == 8) TestGroup8();
                if (num is null || num == 9) TestGroup9();
                if (num is null || num == 10) TestGroup10();
                if (num is null || num == 11) TestGroup11();
                if (num is null || num == 12) TestGroup12();
                if (num is null || num == 13) TestGroup13();

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
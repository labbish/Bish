using Irony.Parsing;
using System.Collections.Generic;

namespace Bish {

    internal class BishArg {
        public BishType type;
        public string name;
        public BishVariable? defaultValue;

        public BishArg(ParseTreeNode typeNode, string name, BishVariable? defaultValue = null) {
            this.name = name;
            type = new(typeNode);
            this.defaultValue = defaultValue;
        }

        public override string ToString() {
            return $"{type} {name}";
        }
    }

    internal interface IBishExecutable {

        public BishVariable Exec(BishVariable[] args);

        public bool MatchArgs(BishVariable[] args);
    }

    internal class BishFunc(BishVars vars, ParseTreeNode node, List<BishArg> args)
        : IBishExecutable {
        private BishVars VarsFrame = new(vars);
        private ParseTreeNode node = node;
        private List<BishArg> args = CheckArgs(args);

        private bool TriviallyToVars(BishVariable[] inArgs,
            out List<(string name, BishVariable value)> values, out string ErrorMsg,
            out int times, List<BishArg>? expected = null) {
            times = 0;
            values = [];
            ErrorMsg = "";
            List<BishVariable> args = [.. inArgs];
            expected ??= this.args;
            try {
                BishUtils.Assert(expected.Count >= args.Count, "Args more than Expected");
                BishUtils.Assert(expected.Count <= args.Count, "Args less than Expected");
                for (int i = 0; i < args.Count; i++) {
                    BishVariable arg = args[i];
                    BishArg expect = expected[i];
                    BishVariable value =
                        BishVars.WeakConvert(expect.type, arg, out int t);
                    values.Add((expect.name, value));
                    times += t;
                }
            }
            catch (Exception ex) {
                ErrorMsg = ex.Message;
                values = [];
                return false;
            }
            return true;
        }

        private bool ToVars(BishVariable[] inArgs,
            out List<(List<(string name, BishVariable value)>, int times)> values, out string ErrorMsg) {
            values = [];
            ErrorMsg = "";
            List<BishVariable> args = [.. inArgs];
            var expected = this.args;
            var defaults = expected.Where(arg => arg.defaultValue is not null).ToList();
            int n = defaults.Count;
            var choices = GetAllCombinations(n);
            foreach (var choice in choices) {
                List<BishArg> exp = [.. expected];
                List<(string name, BishVariable value)> values0 = [];
                for (int i = 0; i < n; i++)
                    if (choice[i] == 0) {
                        exp.Remove(defaults[i]);
                        values0.Add((defaults[i].name, defaults[i].defaultValue!));
                    }
                if (TriviallyToVars([.. args], out var values1, out _, out int t, exp))
                    values.Add(([.. values1.Concat(values0)], t));
            }
            return values.Count != 0;
        }

        public BishVariable Exec(BishVariable[] inArgs) {
            BishInterpreter interpreter = new(VarsFrame);
            bool success = ToVars(inArgs, out var values, out string msg);
            BishUtils.Assert(values.Count > 0, "No Possible Function Found");
            int minTimes = values.Min(x => x.times);
            var minValues = values.Where(x => x.times == minTimes).ToList();
            BishUtils.Assert(minValues.Count == 1, "More than 1 Possible Function Found");
            if (!success) BishUtils.Error(msg);
            foreach (var (name, value) in minValues[0].Item1) {
                interpreter.vars.New(name, value);
            }
            return interpreter.Evaluate(node);
        }

        public bool MatchArgs(BishVariable[] inArgs) {
            return ToVars(inArgs, out _, out _);
        }

        private static List<BishArg> CheckArgs(List<BishArg> args) {
            List<string> names = [];
            foreach (BishArg arg in args) {
                string name = arg.name;
                if (names.Contains(name)) BishUtils.Error($"Duplicate Arg Name: {name}");
                names.Add(name);
            }
            return args;
        }

        public static List<List<int>> GetAllCombinations(int length, int max = 1) {
            if (length == 0) return [[]];
            List<List<int>> last = GetAllCombinations(length - 1, max);
            List<List<int>> ans = [];
            foreach (var list in last)
                foreach (int i in Enumerable.Range(0, max + 1))
                    ans.Add([.. list, i]);
            return ans;
        }
    }
}
using Irony.Parsing;

namespace Bish {

    internal class BishArg {
        public BishType type;
        public string name;
        public BishVariable? defaultValue;

        public BishArg(ParseTreeNode typeNode, string name, BishVariable? defaultValue = null)
            : this(type: new(typeNode), name, defaultValue) { }

        public BishArg(BishType type, string name, BishVariable? defaultValue = null) {
            this.name = name;
            this.type = type;
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

    internal class BishFunc : IBishExecutable {
        private BishVars VarsFrame;
        private ParseTreeNode node;
        private List<BishArg> args;

        public BishFunc(BishVars vars, ParseTreeNode node, List<BishArg> args) {
            VarsFrame = new(vars);
            this.node = node;
            this.args = args;
            HashSet<string> names = [.. args.Select(arg => arg.name)];
            BishUtils.Assert(names.Count == args.Count, $"Duplicate Argument");
        }

        public void BindSelf(string name, BishVariable self) {
            VarsFrame.NewUnchecked(name, self);
        }

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
            //return interpreter.Evaluate(node);
            BishVariable result = new(null);
            Exception? exception = null;
            var thread = new Thread(() => {
                try {
                    result = interpreter.Evaluate(node);
                }
                catch (Exception ex) {
                    exception = ex;
                }
            });
            thread.Start();
            thread.Join();
            if (exception is not null) throw exception;
            return result;
        }

        public bool MatchArgs(BishVariable[] inArgs) {
            return ToVars(inArgs, out _, out _);
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
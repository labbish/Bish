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

    internal class BishInArg(string? name, BishVariable value) {
        public string? name = name;
        public BishVariable value = value;

        public BishInArg(BishVariable value) : this(null, value) {
        }

        public override string ToString() {
            return $"{name ?? ""}{(name is null ? "" : ":")}[{value}]";
        }
    }

    internal interface IBishExecutable {

        public BishVariable Exec(BishInArg[] args);

        public bool MatchArgs(BishInArg[] args);
    }

    internal class BishFunc : IBishExecutable {
        private BishThreadPool pool = new();

        private BishVars VarsFrame;
        private ParseTreeNode node;
        private List<BishArg> args;
        public BishType? returnType;
        private ParseTreeNode? where;

        public BishFunc(BishVars vars, ParseTreeNode node,
            List<BishArg> args, BishType? returnType = null,
            ParseTreeNode? where = null) {
            VarsFrame = new(vars);
            this.node = node;
            this.args = args;
            HashSet<string> names = [.. args.Select(arg => arg.name)];
            BishUtils.Assert(names.Count == args.Count, $"Duplicate Argument");
            this.returnType = returnType;
            this.where = where;
        }

        public void BindSelf(string name, BishVariable self) {
            VarsFrame.New(name, self, checkExist: false);
        }

        private bool TriviallyToVars(BishInArg[] inArgs,
            out List<(string name, BishVariable value)> values, out string ErrorMsg,
            out int times, List<BishArg>? expected = null) {
            times = 0;
            values = [];
            ErrorMsg = "";
            List<BishInArg> args = [.. inArgs];
            expected ??= this.args;
            try {
                BishUtils.Assert(expected.Count >= args.Count, "Args more than Expected");
                BishUtils.Assert(expected.Count <= args.Count, "Args less than Expected");
                List<BishInArg> namedArgs = [.. args.Where(arg => arg.name is not null)];
                foreach (BishInArg namedArg in namedArgs) {
                    string name = namedArg.name!;
                    BishUtils.Assert(expected.Any(arg => arg.name == name),
                        $"No such Arg: {name}");
                    args.RemoveAll(arg => arg.name == name);
                    expected.RemoveAll(arg => arg.name == name);
                    values.Add((name, namedArg.value));
                }
                for (int i = 0; i < args.Count; i++) {
                    BishVariable arg = args[i].value;
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

        private bool ToVars(BishInArg[] inArgs,
            out List<(List<(string name, BishVariable value)>, int times)> values, out string ErrorMsg) {
            values = [];
            ErrorMsg = "";
            List<BishInArg> namedArgs = [.. inArgs.Where(arg => arg.name is not null)];
            List<BishInArg> inArgsUnnamed = [.. inArgs.Where(arg => arg.name is null)];
            List<BishVariable> args = [.. inArgsUnnamed.Select(arg => arg.value)];
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
                bool canConvert = TriviallyToVars([.. inArgs], out var values1, out _, out int t, exp);
                if (canConvert) values.Add(([.. values1.Concat(values0)], t));
            }
            values = [.. values.Where(x => WhereCheck(x.Item1))];
            return values.Count != 0;
        }

        public BishVariable Exec(BishInArg[] inArgs) {
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
            BishVariable result = pool.GetResult(() => interpreter.Evaluate(node))!;
            return BishVars.WeakConvert(returnType ?? new(null, "var", nullable: true), result);
        }

        private bool WhereCheck(List<(string name, BishVariable value)> vars) {
            if (where is null) return true;
            BishInterpreter interpreter = new(VarsFrame);
            foreach (var (name, value) in vars) {
                interpreter.vars.New(name, value);
            }
            return interpreter.Evaluate(where).value;
        }

        public bool MatchArgs(BishInArg[] inArgs) {
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
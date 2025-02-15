using Irony.Parsing;

namespace Bish {

    internal class BishArg {
        public bool isConst;
        public string type;
        public bool nullable;
        public string name;
        public BishVariable? defaultValue;

        public BishArg(ParseTreeNode typeNode, string name, BishVariable? defaultValue = null) {
            this.name = name;
            (isConst, type, nullable) = BishVars.CutType(typeNode);
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

    internal class BishFunc(BishVars vars, ParseTreeNode node, List<BishArg> args) : IBishExecutable {
        private BishVars VarsFrame = new(vars);
        private ParseTreeNode node = node;
        private List<BishArg> args = CheckArgs(args);

        private bool ToVars(BishVariable[] inArgs,
            out List<(string name, BishVariable value)> values,
            out string ErrorMsg) {
            ErrorMsg = "";
            values = [];
            try {
                List<BishVariable> args = [.. inArgs];
                foreach (BishArg require in this.args) {
                    if (args.Count == 0) BishUtils.Error("Less args than Required");
                    string name = require.name;
                    var value = BishVars.WeakConvert(require.type, args[0], require.nullable);
                    value.nullable = require.nullable;
                    value.isConst = require.isConst;
                    values.Add((name, value));
                    args = args[1..];
                }
                if (args.Count != 0) BishUtils.Error("More args than Required");
            }
            catch (Exception ex) {
                ErrorMsg = ex.Message;
                return false;
            }
            return true;
        }

        public BishVariable Exec(BishVariable[] inArgs) {
            BishInterpreter interpreter = new(VarsFrame);
            bool success = ToVars(inArgs, out var values, out string msg);
            if (!success) BishUtils.Error(msg);
            foreach (var (name, value) in values) {
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
    }
}
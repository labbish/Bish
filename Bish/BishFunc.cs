using Irony.Parsing;

namespace Bish {

    internal class BishArg(string type, string name) {
        public string type = type;
        public string name = name;

        public override string ToString() {
            return $"{type} {name}";
        }
    }

    internal interface IBishExecutable {

        public BishVariable Exec(BishVariable[] args);
    }

    internal class BishFunc(BishVars vars, ParseTreeNode node, List<BishArg> args) : IBishExecutable {
        private BishVars VarsFrame = new(vars);
        private ParseTreeNode node = node;
        private List<BishArg> args = CheckArgs(args);

        public BishVariable Exec(BishVariable[] inArgs) {
            BishInterpreter interpreter = new(VarsFrame);
            List<BishVariable> args = [.. inArgs];
            foreach (BishArg require in this.args) {
                if (args.Count == 0) BishUtils.Error("Less args than Required");
                string name = require.name;
                var value = BishVars.WeakConvert(require.type, args[0]);
                interpreter.vars.New(name, value);
                args = args[1..];
            }
            if (args.Count != 0) BishUtils.Error("More args than Required");
            return interpreter.Evaluate(node);
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
namespace Bish {

    internal class BishBuiltInFunc
        (Func<BishInArg[], BishVariable> exec, Func<BishInArg[], bool> matching)
        : IBishExecutable {
        private readonly Func<BishInArg[], BishVariable> exec = exec;
        private readonly Func<BishInArg[], bool> matching = matching;

        public BishVariable Exec(BishInArg[] args) {
            return exec(args);
        }

        public bool MatchArgs(BishInArg[] args) {
            return matching(args);
        }
    }

    internal class BishBuiltInFuncs {

        private static readonly BishBuiltInFunc print = new(
            args => {
                List<BishInArg> print = [.. args];
                string sep = " ";
                string end = "";
                if (print.Any(arg => arg.name == "sep")) {
                    sep = (string)print.Where(arg => arg.name == "sep").First().value.value!;
                    print.RemoveAll(arg => arg.name == "sep");
                }
                if (print.Any(arg => arg.name == "end")) {
                    end = (string)print.Where(arg => arg.name == "end").First().value.value!;
                    print.RemoveAll(arg => arg.name == "end");
                }
                Console.Write(string.Join(sep, print.Select(arg => arg.value.ValueString())) + end);
                return new(null);
            },
            args => {
                return args.All(arg => arg.name is null || arg.name == "sep" || arg.name == "end")
                    && args.Where(arg => arg.name == "sep").Count() <= 1
                    && args.Where(arg => arg.name == "end").Count() <= 1;
            }
            ); //TEMP, for debugging

        public static HashSet<BishVariable> GetBuiltIns() {
            return [
                new("print", type: "func", value: print),
                ];
        }
    }
}
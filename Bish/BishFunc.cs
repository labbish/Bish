using Irony.Parsing;

namespace Bish {

    internal interface IBishExecutable {

        public BishVariable exec(BishVariable[] args);
    }

    internal class BishFunc(BishVars vars, ParseTreeNode node) : IBishExecutable {
        private BishVars VarsFrame = new(vars);
        private ParseTreeNode node = node;

        public BishVariable exec(BishVariable[] args) {
            return new BishInterpreter(VarsFrame).Evaluate(node);
        }
    }
}
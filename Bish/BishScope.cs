namespace Bish {

    internal class BishScope {
        private List<BishVars> VarsStack;

        public BishVars currentVars;

        public BishScope() {
            VarsStack = [new BishVars()];
            currentVars = VarsStack[^1];
        }

        public BishScope(BishVars vars) {
            VarsStack = [new(vars)];
            currentVars = VarsStack[^1];
        }

        public static BishScope GetScope(BishVars vars) {
            BishScope scope = new();
            scope.VarsStack = [vars];
            scope.currentVars = scope.VarsStack[^1];
            return scope;
        }

        public void Inner() {
            VarsStack.Add(new(currentVars));
            currentVars = VarsStack[^1];
        }

        public void Outer() {
            VarsStack = VarsStack[..^1];
            currentVars = VarsStack[^1];
        }

        public int Depth() {
            return VarsStack.Count - 1;
        }
    }
}
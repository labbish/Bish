namespace Bish {

    internal class BishReturnException(BishVariable returnVar) : Exception("Return must be used in Funcs") {
        public BishVariable returnVar = returnVar;
    }
}
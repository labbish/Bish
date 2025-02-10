using Irony.Parsing;
using System.Collections;

namespace Bish {

    internal class BishVars : IEnumerable<BishVariable> {
        public HashSet<BishVariable> vars;

        public BishVars() {
            vars = new HashSet<BishVariable> { };
        }

        public IEnumerator<BishVariable> GetEnumerator() {
            return vars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return vars.GetEnumerator();
        }

        public BishVariable Get(ParseTreeNode node) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) return var;
            throw new InvalidDataException($"Variable not found: {name}");
        }

        public BishVariable Set(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                var.value = value.value;
                return new BishVariable(null, value.value);
            }
            throw new InvalidDataException($"Variable not found: {name}");
        }

        public BishVariable New(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            vars.Add(new BishVariable(name, value.value));
            return value;
        }
    }
}
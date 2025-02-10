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
            foreach (BishVariable var in vars)
                BishUtils.Assert(var.name != name, $"Var {name} already exists");
            vars.Add(new BishVariable(name, value.value));
            return value;
        }

        public static BishVariable WeakConvert(string type, BishVariable var) {
            bool converted = false;
            dynamic? value = null;
            if (type == "num" && var.value is double num) {
                value = num;
                converted = true;
            }
            if (type == "string" && var.value is string str) {
                value = str;
                converted = true;
            }
            if (!converted)
                throw new TypeLoadException($"Cannot convert [{var}] into type {type}");
            return new BishVariable(null, value);
        }
    }
}
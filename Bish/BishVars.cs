using Irony.Parsing;
using System.Collections;

namespace Bish {

    internal class BishVars : IEnumerable<BishVariable> {
        public HashSet<BishVariable> vars;

        public BishVars() {
            vars = [];
        }

        public BishVars(BishVars original) {
            vars = [.. original.vars];
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
            foreach (BishVariable var in matched) return var.GetNullChecked();
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable GetUnchecked(ParseTreeNode node) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) return var;
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable Exec(ParseTreeNode node, BishVariable[] args) {
            string name = node.FindTokenAndGetText();
            if (name == "print") {
                Console.Write(string.Join(' ', args.Select(arg => arg.ValueString())));
                return new(null);
            } //TEMP, for debugging
            var matched = vars.Where(var => var.name == name).ToHashSet();
            var funcs = matched.Where(var => var.value is BishFunc && var.value is not null)
                .Where(var => var.value!.MatchArgs(args)).ToHashSet();
            BishUtils.Assert(funcs.Count <= 1, $"Multiple Functions found: {name}");
            foreach (BishVariable func in funcs) return func.Exec(args);
            return BishUtils.Error($"Function not found: {name}");
        }

        public BishVariable Set(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            return Set(name, value);
        }

        public BishVariable Set(string name, BishVariable value) {
            if (name.All(c => c == '_')) return new(null, value.value);
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                BishUtils.Assert(!var.type.isConst, $"Cannot modify const var: {name}");
                BishVariable newVar = new(null, value.value);
                WeakConvert(var.type, newVar); //might throw
                var.value = newVar.value;
                return new(null, value.value);
            }
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable SetIfExist(string name, BishVariable value) {
            if (name.All(c => c == '_')) return new(null, value.value);
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                BishUtils.Assert(!var.type.isConst, $"Cannot modify const var: {name}");
                BishVariable newVar = new(null, value.value);
                WeakConvert(var.type, newVar); //might throw
                var.value = newVar.value;
                return new(null, value.value);
            }
            return new(null);
        }

        public BishVariable New(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            return New(name, value);
        }

        public BishVariable New(string name, BishVariable value) {
            if (name.All(c => c == '_')) return new(null, value.value);
            var matched = vars.Where(var => var.name == name).ToHashSet();
            BishUtils.Assert(matched.Count == 0, $"Var {name} already exists");
            vars.Add(new BishVariable(name: name, type: value.type, value: value.value));
            return value;
        }

        public BishVariable NewUnchecked(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            return NewUnchecked(name, value);
        }

        public BishVariable NewUnchecked(string name, BishVariable value) {
            if (name.All(c => c == '_')) return new(null, value.value);
            vars.Add(new BishVariable(name, value.type, value.value));
            return value;
        }

        public static BishVariable WeakConvert(BishType type, BishVariable var) {
            return WeakConvert(type, var, out _);
        }

        public static BishVariable WeakConvert(BishType type, BishVariable var,
            out int ConvertTimes) {
            bool converted = false;
            dynamic? value = null;
            ConvertTimes = 0;
            if (type.nullable && var.value is null) {
                converted = true;
            }
            if (type.type == "var" && type.typeArgs.Count > 0) {
                foreach (BishType subType in type.typeArgs) {
                    try {
                        BishVariable result = WeakConvert(subType, var, out int subConvertTimes);
                        ConvertTimes = subConvertTimes;
                        result.type = type;
                        return result;
                    }
                    catch (Exception) { }
                }
            }
            if (type.type == "var" && type.typeArgs.Count == 0 && var.value is not null) {
                value = var.value;
                converted = true;
                ConvertTimes += 1;
            }
            if (type.type == "num" && type.typeArgs.Count == 0) {
                if (var.value is double num) {
                    value = num;
                    converted = true;
                }
                else if (var.value is int i) {
                    value = i;
                    converted = true;
                    ConvertTimes++;
                }
            }
            else if (type.type == "int" && type.typeArgs.Count == 0) {
                if (var.value is int num) {
                    value = num;
                    converted = true;
                }
            }
            else if (type.type == "string" && type.typeArgs.Count == 0) {
                if (var.value is string str) {
                    value = str;
                    converted = true;
                }
            }
            else if (type.type == "bool" && type.typeArgs.Count == 0) {
                if (var.value is bool b) {
                    value = b;
                    converted = true;
                }
            }
            else if (type.type == "interval" && type.typeArgs.Count == 0) {
                if (var.value is BishInterval i) {
                    value = i;
                    converted = true;
                }
            }
            else if (type.type == "func") {
                if (var.value is BishFunc f) {
                    value = f;
                    converted = true;
                }
            }
            BishUtils.Assert(converted, $"Cannot convert [{var}] into type {type}");
            return new BishVariable(name: null, type: type, value);
        }

        public override string ToString() {
            return "{\n  "
                + string.Join("\n  ", vars.Select(var => $"{var.name}: {var.ValueString()}"))
                + "\n}";
        }

        public static List<string> ToPlainStrings(ParseTreeNode node) {
            if (node.ChildNodes.Count == 0) return [node.FindTokenAndGetText()];
            List<string> strings = [];
            foreach (ParseTreeNode child in node.ChildNodes) {
                strings.AddRange(ToPlainStrings(child));
            }
            return strings;
        }

        public static string ToPlainString(ParseTreeNode node) {
            return string.Join("·", ToPlainStrings(node));
        }
    }
}
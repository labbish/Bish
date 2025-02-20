using Irony.Parsing;
using System.Collections;

namespace Bish {

    internal class BishVars : IEnumerable<BishVariable> {
        public HashSet<BishVariable> vars;

        public BishVars() {
            vars = [];
        }

        public void Clear() {
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

        public BishVariable Get(ParseTreeNode node, bool checkNull = true) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            var values = matched.Select(var => checkNull ? var.GetNullChecked() : var).ToHashSet();
            BishUtils.Assert(values.Count <= 1, $"Multiple variables found: {name}");
            BishUtils.Assert(values.Count > 0, $"Variable not found: {name}");
            return values.First();
        }

        public BishVariable Exec(ParseTreeNode node, BishInArg[] args) {
            string name = node.FindTokenAndGetText();
            if (name == "print") {
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
            } //TEMP, for debugging
            var matched = vars.Where(var => var.name == name).ToHashSet();
            var funcs = matched.Where(var => var.value is BishFunc && var.value is not null)
                .Where(var => var.value!.MatchArgs(args)).ToHashSet();
            BishUtils.Assert(funcs.Count <= 1, $"Multiple Functions found: {name}");
            foreach (BishVariable func in funcs) return func.Exec(args);
            return BishUtils.Error($"Function not found: {name}");
        }

        public BishVariable Set(ParseTreeNode node, BishVariable value,
            bool checkConst = true, bool checkExist = true) {
            string name = node.FindTokenAndGetText();
            return Set(name, value, checkConst, checkExist);
        }

        public BishVariable Set(string name, BishVariable value,
            bool checkConst = true, bool checkExist = true) {
            if (name.All(c => c == '_')) return new(null, value.value);
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                if (checkConst)
                    BishUtils.Assert(!var.type.isConst, $"Cannot modify const var: {name}");
                BishVariable newVar = new(null, value.value);
                WeakConvert(var.type, newVar); //might throw
                var.value = newVar.value;
                return new(null, value.value);
            }
            if (checkExist) return BishUtils.Error($"Variable not found: {name}");
            return new(null);
        }

        public BishVariable New(ParseTreeNode node, BishVariable value, bool checkExist = true) {
            string name = node.FindTokenAndGetText();
            return New(name, value, checkExist);
        }

        public BishVariable New(string name, BishVariable value, bool checkExist = true) {
            if (name.All(c => c == '_')) return new(null, value.value);
            if (checkExist) {
                var matched = vars.Where(var => var.name == name).ToHashSet();
                BishUtils.Assert(matched.Count == 0, $"Var {name} already exists");
            }
            vars.Add(new BishVariable(name: name, type: value.type, value: value.value));
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
            if (!type.nullable && var.value is null) {
                BishUtils.Error("Cannot convert null value to not nullable type");
            }
            if (type.type == "var" && type.typeArgs.Count > 0) {
                BishUtils.Assert(type.typeArgs.All(arg => arg.value is BishType));
                foreach (BishType? subType in type.typeArgs.Select(arg => arg.value as BishType)) {
                    try {
                        BishVariable result = WeakConvert(subType!, var, out int subConvertTimes);
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
                if (var.value is BishNum num) {
                    value = num;
                    converted = true;
                }
                else if (var.value is BishInt i) {
                    value = (BishNum)i;
                    converted = true;
                    ConvertTimes++;
                }
            }
            else if (type.type == "int" && type.typeArgs.Count == 0) {
                if (var.value is BishInt num) {
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
            else if (type.type == "type" && type.typeArgs.Count == 0) {
                if (var.value is BishType t) {
                    value = t;
                    converted = true;
                }
            }
            else if (type.type == "func" && type.typeArgs.Count <= 1) {
                if (var.value is BishFunc f) {
                    value = f;
                    if (type.typeArgs.Count == 1) {
                        BishUtils.Assert(type.typeArgs.All(arg => arg.value is BishType));
                        value.returnType = type.typeArgs[0].value;
                    }
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
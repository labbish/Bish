using Irony.Parsing;
using System.Diagnostics;

namespace Bish {

    internal class BishInterpreter {
        public BishVars vars;

        public BishInterpreter() {
            vars = new BishVars();
        }

        public BishVariable Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) {
                throw new ArgumentException("Parse tree is empty.");
            }
            return Evaluate(parseTree.Root);
        }

        private BishVariable Evaluate(ParseTreeNode node) {
            //Console.WriteLine(node.Term.Name);
            if (node == null) return new BishVariable(null);
            if (node.Term is IdentifierTerminal) {
                return vars.Get(node);
            }
            else if (node.Term is NumberLiteral) {
                var str = node.Token.Value.ToString();
                BishUtils.Assert(str != null, "NumberLiteral is Null");
                dynamic value;
                if (str!.Contains('.')) value = double.Parse(str!);
                else value = int.Parse(str!);
                return new BishVariable(null, value);
            }
            else if (node.Term is StringLiteral) {
                var str = node.Token.Value.ToString();
                BishUtils.Assert(str != null, "NumberLiteral is Null");
                return new BishVariable(null, str!);
            }
            else if (node.Term is NonTerminal) {
                if (node.ChildNodes.Count == 1) return Evaluate(node.ChildNodes[0]);
                if (node.ChildNodes.Count == 2 && node.Term.Name == "factor") {
                    var sign = node.ChildNodes[0].FindTokenAndGetText();
                    var value = Evaluate(node.ChildNodes[1]);
                    if (sign == "+") return +value;
                    if (sign == "-") return -value;
                }
                if (node.ChildNodes.Count == 3 && node.ChildNodes[0].FindTokenAndGetText() == "(" && node.ChildNodes[2].FindTokenAndGetText() == ")")
                    return Evaluate(node.ChildNodes[1]);
                if (node.ChildNodes.Count == 3) {
                    ParseTreeNode leftNode = node.ChildNodes[0];
                    BishVariable left = Evaluate(node.ChildNodes[0]);
                    BishVariable right = Evaluate(node.ChildNodes[2]);
                    string op = node.ChildNodes[1].FindTokenAndGetText();
                    return op switch {
                        "+" => left + right,
                        "-" => left - right,
                        "*" => left * right,
                        "/" => left / right,
                        "^" => left ^ right,
                        "=" => vars.Set(leftNode, right),
                        _ => (BishVariable)BishUtils.Error($"Unsupported operator: {op}"),
                    };
                }
                if (node.ChildNodes.Count == 4 && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                    string type = node.ChildNodes[0].FindTokenAndGetText();
                    ParseTreeNode varName = node.ChildNodes[1];
                    BishVariable right = Evaluate(node.ChildNodes[3]);
                    dynamic? value = BishVars.WeakConvert(type, right);
                    return vars.New(varName, value);
                }
                return BishUtils.Error($"Unsupported NonTerminal with name {node.Term.Name} and child count of {node.ChildNodes.Count}");
            }
            return BishUtils.Error($"Unsupported expression type: {node.Term}");
        }
    }
}
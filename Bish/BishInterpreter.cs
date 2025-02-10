using Irony.Parsing;
using System.Diagnostics;

namespace Bish {

    internal class BishInterpreter {
        public BishVars vars;

        public BishInterpreter() {
            vars = new BishVars();
        }

        public void Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) {
                throw new ArgumentException("Parse tree is empty.");
            }
            var result = Evaluate(parseTree.Root);
            Console.WriteLine($"Result: {result}");
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
                double value = double.Parse(str!);
                return new BishVariable(null, value);
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
                    switch (op) {
                        case "+": return left + right;
                        case "-": return left - right;
                        case "*": return left * right;
                        case "/": return left / right;
                        case "^": return left ^ right;
                        case "=": return vars.Set(leftNode, right);
                        default:
                            throw new InvalidOperationException($"Unsupported operator: {op}");
                    }
                }
                if (node.ChildNodes.Count == 4 && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                    string _ = node.ChildNodes[0].FindTokenAndGetText();
                    ParseTreeNode varName = node.ChildNodes[1];
                    BishVariable right = Evaluate(node.ChildNodes[3]);
                    return vars.New(varName, right);
                }
                throw new InvalidOperationException($"Unsupported NonTerminal with name {node.Term.Name} and child count of {node.ChildNodes.Count}");
            }
            throw new InvalidOperationException($"Unsupported expression type: {node.Term}");
        }
    }
}
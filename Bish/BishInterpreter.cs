using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bish;
using Irony.Parsing;
using System.Numerics;

namespace Bish {

    internal class BishInterpreter {
        private Dictionary<string, string> a;

        public void Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) {
                throw new ArgumentException("Parse tree is empty.");
            }
            var result = Evaluate(parseTree.Root);
            Console.WriteLine($"Result: {result}");
        }

        private double Evaluate(ParseTreeNode node) {
            if (node == null) return 0;
            if (node.Term is IdentifierTerminal) {
                return GetIdentifierValue(node);
            }
            else if (node.Term is NumberLiteral) {
                return double.Parse(node.Token.Value.ToString());
            }
            else if (node.Term is NonTerminal) {
                if (node.Term.Name == "expression" || node.Term.Name == "term" || node.Term.Name == "factor" || node.Term.Name == "powerExpr") {
                    if (node.ChildNodes.Count == 1) return Evaluate(node.ChildNodes[0]);
                    if (node.ChildNodes.Count == 2 && node.Term.Name == "factor") {
                        var sign = node.ChildNodes[0].FindTokenAndGetText();
                        var value = Evaluate(node.ChildNodes[1]);
                        if (sign == "+") return +value;
                        if (sign == "-") return -value;
                    }
                    if (node.ChildNodes.Count == 3 && node.ChildNodes[0].FindTokenAndGetText() == "(" && node.ChildNodes[2].FindTokenAndGetText() == ")") return Evaluate(node.ChildNodes[1]);
                }
                if (node.ChildNodes.Count == 3) {
                    double left = Evaluate(node.ChildNodes[0]);
                    double right = Evaluate(node.ChildNodes[2]);
                    string op = node.ChildNodes[1].FindTokenAndGetText();
                    switch (op) {
                        case "+":
                            return left + right;

                        case "-":
                            return left - right;

                        case "*":
                            return left * right;

                        case "/": {
                                if (right == 0) throw new DivideByZeroException($"Divided by zero: {left}/{right}");
                                return left / right;
                            }

                        case "^": {
                                return Math.Pow(left, right);
                            }

                        default:
                            throw new InvalidOperationException($"Unsupported operator: {op}");
                    }
                }
            }
            throw new InvalidOperationException($"Unsupported expression type: {node.Term}");
        }

        private double GetIdentifierValue(ParseTreeNode node) {
            return 0;
        }
    }
}
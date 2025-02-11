using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var number = new NumberLiteral("number");
            var singleString = new StringLiteral("single_string", "'");
            var doubleString = new StringLiteral("double_string", "\"");
            var plus = ToTerm("+");
            var minus = ToTerm("-");
            var multiply = ToTerm("*");
            var divide = ToTerm("/");
            var power = ToTerm("^");
            var assign = ToTerm("=");
            var nullable = ToTerm("?");

            var intVar = ToTerm("int");
            var numVar = ToTerm("num");
            var strVar = ToTerm("string");
            var boolVar = ToTerm("bool");
            var trueValue = ToTerm("true");
            var falseValue = ToTerm("false");

            var str = new NonTerminal("string");
            var boolValue = new NonTerminal("boolValue");
            var literal = new NonTerminal("literal");
            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var assignment = new NonTerminal("assignment");
            var varType = new NonTerminal("varType");
            var statement = new NonTerminal("statement");
            var root = new NonTerminal("root");

            str.Rule = singleString | doubleString;
            boolValue.Rule = trueValue | falseValue;
            literal.Rule = str | number | boolValue;
            factor.Rule = (minus | plus) + factor | literal | identifier | "(" + expression + ")";
            powerExpr.Rule = factor | powerExpr + power + factor;
            term.Rule = powerExpr | term + multiply + powerExpr | term + divide + powerExpr;
            expression.Rule = term | expression + plus + term | expression + minus + term;
            assignment.Rule = expression | expression + assign + assignment;
            varType.Rule = intVar | numVar | strVar | boolVar;
            statement.Rule = assignment | varType + identifier | varType + identifier + assign + assignment;
            root.Rule = statement | Empty;

            Root = root;
        }
    }
}
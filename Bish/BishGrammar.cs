using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var number = new NumberLiteral("number");
            var plus = ToTerm("+");
            var minus = ToTerm("-");
            var multiply = ToTerm("*");
            var divide = ToTerm("/");
            var power = ToTerm("^");
            var assign = ToTerm("=");
            var numVar = ToTerm("num");

            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var assignment = new NonTerminal("assignment");
            var statement = new NonTerminal("statement");

            factor.Rule = (minus | plus) + factor | number | identifier | "(" + expression + ")";
            powerExpr.Rule = factor | powerExpr + power + factor;
            term.Rule = powerExpr | term + multiply + powerExpr | term + divide + powerExpr;
            expression.Rule = term | expression + plus + term | expression + minus + term;
            assignment.Rule = expression | expression + assign + assignment;
            statement.Rule = assignment | numVar + identifier + assign + assignment;

            Root = statement;
        }
    }
}
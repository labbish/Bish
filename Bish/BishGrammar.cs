using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var factor = new NonTerminal("factor");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var powerExpr = new NonTerminal("powerExpr");

            factor.Rule = (minus | plus) + factor | number | identifier | "(" + expression + ")";
            powerExpr.Rule = factor | powerExpr + power + factor;
            term.Rule = powerExpr | term + multiply + powerExpr | term + divide + powerExpr;
            expression.Rule = term | expression + plus + term | expression + minus + term;

            Root = expression;
        }
    }
}
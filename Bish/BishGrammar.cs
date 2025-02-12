using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var number = new NumberLiteral("number");
            var singleString = new StringLiteral("single_string", "'");
            var doubleString = new StringLiteral("double_string", "\"");

            var intType = ToTerm("int");
            var numType = ToTerm("num");
            var stringType = ToTerm("string");
            var boolType = ToTerm("bool");
            var trueValue = ToTerm("true");
            var falseValue = ToTerm("false");

            var stringLiteral = new NonTerminal("stringLiteral");
            var boolLiteral = new NonTerminal("boolLiteral");
            var literal = new NonTerminal("literal");
            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var assignment = new NonTerminal("assignment");
            var varTypes = new NonTerminal("varTypes");
            var statement = new NonTerminal("statement");
            var root = new NonTerminal("root");

            stringLiteral.Rule = singleString | doubleString;
            boolLiteral.Rule = trueValue | falseValue;
            literal.Rule = stringLiteral | number | boolLiteral;
            factor.Rule = "+" + factor | "-" + factor | literal | identifier | "(" + expression + ")";
            powerExpr.Rule = factor | powerExpr + "^" + factor;
            term.Rule = powerExpr | term + "*" + powerExpr | term + "/" + powerExpr;
            expression.Rule = term | expression + "+" + term | expression + "-" + term;
            assignment.Rule = expression | identifier + "=" + assignment;
            varTypes.Rule = intType | numType | stringType | boolType;
            statement.Rule = assignment | varTypes + identifier | varTypes + identifier + "=" + assignment;
            root.Rule = statement | Empty;

            Root = root;
        }
    }
}
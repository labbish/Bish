﻿using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {
        public static readonly List<string> MatchableOperators = ["==", "!=", "<", "<=", ">", ">="];

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var numberLiteral = new NumberLiteral("numberLiteral");
            var singleString = new StringLiteral("single_string", "'");
            var doubleString = new StringLiteral("double_string", "\"");
            var rawString = new StringLiteral("raw_string", "\"\"\"",
                StringOptions.AllowsLineBreak | StringOptions.NoEscapes);

            var intType = ToTerm("int");
            var numType = ToTerm("num");
            var stringType = ToTerm("string");
            var boolType = ToTerm("bool");
            var trueLiteral = ToTerm("true");
            var falseLiteral = ToTerm("false");
            var nullLiteral = ToTerm("null");
            var constModifier = ToTerm("const");
            var ifTerm = ToTerm("if");
            var elseTerm = ToTerm("else");
            var whileTerm = ToTerm("while");
            var doTerm = ToTerm("do");
            var forTerm = ToTerm("for");
            var jumpTerm = ToTerm("jump");
            var endPos = ToTerm("end");
            var startPos = ToTerm("start");
            var nextPos = ToTerm("next");
            var tagTerm = ToTerm("tag");
            var intervalType = ToTerm("interval");
            var infLiteral = ToTerm("inf");
            var switchTerm = ToTerm("switch");
            var caseTerm = ToTerm("case");
            var continueTerm = ToTerm("continue");
            var defaultTerm = ToTerm("default");
            var varType = ToTerm("var");
            var funcTerm = ToTerm("func");
            var defTerm = ToTerm("def");
            var notTerm = ToTerm("not");
            var returnTerm = ToTerm("return");
            var typeType = ToTerm("type");

            var stringLiteral = new NonTerminal("stringLiteral");
            var boolLiteral = new NonTerminal("boolLiteral");
            var literal = new NonTerminal("literal");
            var funcType = new NonTerminal("funcType");
            var funcCallArg = new NonTerminal("funcCallArg");
            var funcCallArgs = new NonTerminal("funcCallArgs");
            var funcCall = new NonTerminal("funcCall");
            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var comparison = new NonTerminal("comparison");
            var logicAnd = new NonTerminal("logicAnd");
            var logicOr = new NonTerminal("logicOr");
            var triCondition = new NonTerminal("triCondition");
            var assignment = new NonTerminal("assignment");
            var interval = new NonTerminal("interval");
            var varOriginalTypes = new NonTerminal("varOriginalTypes");
            var varTypes = new NonTerminal("varTypes");
            var varNullableTypes = new NonTerminal("varNullableTypes");
            var varModifiedTypes = new NonTerminal("varModifiedTypes");
            var varTypeList = new NonTerminal("varTypeList");
            var typeValue = new NonTerminal("typeValue");
            var matchingExpr = new NonTerminal("matchingExpr");
            var matchingAndExpr = new NonTerminal("matchingAndExpr");
            var matchingOrExpr = new NonTerminal("matchingOrExpr");
            var matching = new NonTerminal("matching");
            var statement = new NonTerminal("statement");
            var jumpPos = new NonTerminal("jumpPos");
            var jump = new NonTerminal("jump");
            var sentence = new NonTerminal("sentence");
            var sentences = new NonTerminal("sentences");
            var structure = new NonTerminal("structure");
            var codeBlocks = new NonTerminal("codeBlocks");
            var ifStatement = new NonTerminal("ifStatement");
            var tag = new NonTerminal("tag");
            var loopStatement = new NonTerminal("loopStatement");
            var caseTag = new NonTerminal("caseTag");
            var caseBlock = new NonTerminal("caseBlock");
            var caseBlocks = new NonTerminal("caseBlocks");
            var switchExpr = new NonTerminal("switchExpr");
            var switchStatement = new NonTerminal("switchStatement");
            var funcValue = new NonTerminal("funcValue");
            var funcStateArg = new NonTerminal("funcStateArg");
            var funcStateArgs = new NonTerminal("funcStateArgs");
            var funcStatement = new NonTerminal("funcStatement");
            var root = new NonTerminal("root");

            stringLiteral.Rule = singleString | doubleString | rawString;
            boolLiteral.Rule = trueLiteral | falseLiteral;
            literal.Rule = stringLiteral | numberLiteral | boolLiteral
                | nullLiteral | infLiteral | interval | funcValue | varModifiedTypes;
            funcType.Rule = funcTerm;
            funcCallArg.Rule = assignment;
            funcCallArgs.Rule = funcCallArg | funcCallArgs + "," + funcCallArg;
            funcCall.Rule = identifier + "(" + (funcCallArgs | Empty) + ")";
            factor.Rule = "!" + factor
                | "+" + factor | "-" + factor | factor + "++" | factor + "--"
                | literal | identifier | "(" + codeBlocks + ")" | funcCall
                | statement + "(" + (funcCallArgs | Empty) + ")";
            powerExpr.Rule = factor | powerExpr + "^" + factor;
            term.Rule = powerExpr | term + "*" + powerExpr | term + "/" + powerExpr
                | term + "%" + powerExpr;
            expression.Rule = term | expression + "+" + term | expression + "-" + term;
            comparison.Rule = expression | comparison + "<=>" + expression
                | comparison + "==" + expression | comparison + "!=" + expression
                | comparison + ">" + expression | comparison + ">=" + expression
                | comparison + "<" + expression | comparison + "<=" + expression;
            logicAnd.Rule = comparison | logicAnd + "&&" + comparison;
            logicOr.Rule = logicAnd | logicOr + "||" + logicAnd;
            triCondition.Rule = logicOr | logicOr + "?" + codeBlocks + ":" + codeBlocks;
            assignment.Rule = triCondition | identifier + "=" + assignment
                | identifier + "+=" + assignment | identifier + "-=" + assignment
                | identifier + "*=" + assignment | identifier + "/=" + assignment
                | identifier + "%=" + assignment | identifier + "^=" + assignment;
            interval.Rule = "(" + assignment + "," + assignment + ")"
                | "(" + assignment + "," + assignment + "]"
                | "[" + assignment + "," + assignment + ")"
                | "[" + assignment + "," + assignment + "]";
            varOriginalTypes.Rule = intType | numType | stringType | boolType
                | intervalType | varType | funcType | typeType;
            varTypes.Rule = varOriginalTypes | varOriginalTypes + "<" + varTypeList + ">";
            varNullableTypes.Rule = varTypes | varTypes + "?";
            varModifiedTypes.Rule = varNullableTypes | constModifier + varNullableTypes;
            varTypeList.Rule = varModifiedTypes | varTypeList + "," + varModifiedTypes;
            typeValue.Rule = varModifiedTypes | identifier;
            matchingExpr.Rule = assignment | typeValue + identifier;
            foreach (var op in MatchableOperators) matchingExpr.Rule |= op + assignment;
            matchingExpr.Rule |= "(" + matchingOrExpr + ")";
            matchingExpr.Rule |= notTerm + matchingOrExpr;
            matchingAndExpr.Rule = matchingExpr | matchingAndExpr + "&" + matchingExpr;
            matchingOrExpr.Rule = matchingAndExpr | matchingOrExpr + "|" + matchingAndExpr;
            matching.Rule = assignment | assignment + "~" + matchingOrExpr
                | assignment + "!" + "~" + matchingOrExpr;
            statement.Rule = matching | typeValue + identifier
                | typeValue + identifier + "=" + matching;
            jumpPos.Rule = endPos | startPos | nextPos;
            jump.Rule = jumpTerm + jumpPos + "[" + identifier + "]" | jumpTerm + jumpPos;
            sentence.Rule = Empty | statement | jump | continueTerm | returnTerm + statement;
            sentences.Rule = root | sentence | sentences + ";" + root;
            structure.Rule = "{" + sentences + "}";
            codeBlocks.Rule = sentences | "{" + sentences + "}";
            ifStatement.Rule = codeBlocks | ifTerm + "(" + sentence + ")" + structure
                | ifTerm + "(" + sentence + ")" + structure + elseTerm + structure;
            tag.Rule = tagTerm + identifier + ":";
            loopStatement.Rule = codeBlocks
                | whileTerm + "(" + sentence + ")" + structure
                | tag + whileTerm + "(" + sentence + ")" + structure
                | doTerm + structure + whileTerm + "(" + sentence + ")"
                | tag + doTerm + structure + whileTerm + "(" + sentence + ")"
                | forTerm + "(" + sentence + ";" + sentence + ";" + sentence + ")" + structure
                | tag + forTerm + "(" + sentence + ";" + sentence + ";" + sentence + ")" + structure;
            caseTag.Rule = caseTerm + matchingOrExpr + ":" | defaultTerm + ":";
            caseBlock.Rule = caseTag + structure;
            caseBlocks.Rule = caseBlock | caseBlocks + caseBlock;
            switchExpr.Rule = assignment | assignment + "!";
            switchStatement.Rule = switchTerm + "(" + switchExpr + ")" + "{" + caseBlocks + "}";
            funcStateArg.Rule = typeValue + identifier
                | typeValue + identifier + "=" + assignment;
            funcStateArgs.Rule = funcStateArg | funcStateArgs + "," + funcStateArg;
            funcStatement.Rule = defTerm + identifier + "(" + (funcStateArgs | Empty) + ")"
                + (structure | "=>" + statement);
            funcValue.Rule = funcTerm + "(" + (funcStateArgs | Empty) + ")"
                + (structure | "=>" + statement);
            root.Rule = ifStatement | loopStatement | switchStatement | funcStatement;

            Root = root;
        }
    }
}
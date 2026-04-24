lexer grammar BishLexer;

END : ';' ;
LPAREN : '(' ;
RPAREN : ')' ;
LBRACK : '[' ;
RBRACK : ']' ;
LBRACE : '{' ;
RBRACE : '}' ;
COL : ':' ;
ADD : '+' ;
SUB : '-' ;
MUL : '*' ;
DIV : '/' ;
MOD : '%' ;
POW : '^' ;
BANG : '!' ;
INVERT : '~' ;
EQ : '==' ;
NEQ : '!=' ;
REQ : '===' ;
NREQ : '!==' ;
TRI : '<=>' ;
LT : '<' ;
LE : '<=' ;
GT : '>' ;
GE : '>=' ;
BAND : '&&' ;
BOR : '||' ;
NCOMB : '??' ;
SETS : '=' ;
DEFS : ':=' ;
COM : ',' ;
PIPE : '$' ;
DOT : '.' ;
REST : '..' ;
BAR : '|' ;
QUES : '?' ;
ARROW : '=>' ;
AT : '@' ;

BLOCK_COMMENT
    : '/*' .*? '*/' -> skip
    ;

LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;

INT : '0'
    | [1-9] [0-9]*
    | '0x' [1-9A-Fa-f] [0-9A-Fa-f]*
    | '0o' [1-7] [0-7]*
    | '0b' '1' [01]*
    ;
NUM : ([0-9]+ '.' [0-9]* | '.' [0-9]+) ('e' ('+'|'-')? INT)? ;
BOL : 'true' | 'false' ;

STR : S1 | S2 | R11 | R12 | R13 | R14 | R21 | R22 | R23 | R24 ;

// We'll use a simple method for now
fragment RP
    : 'r'
    ;
R11 : RP '\'' .*? '\'' ;
R12 : RP '#' '\'' .*? '\'' '#' ;
R13 : RP '##' '\'' .*? '\'' '##' ;
R14 : RP '###' '\'' .*? '\'' '###' ;
R21 : RP '"' .*? '"' ;
R22 : RP '#' '"' .*? '"' '#' ;
R23 : RP '##' '"' .*? '"' '##' ;
R24 : RP '###' '"' .*? '"' '###' ;

S1  : '\'' ( ~('\\'|'\'') | '\\' . )* '\'' ;
S2  : '"' ( ~('\\'|'"') | '\\' . )* '"' ;

NUL : 'null' ;

IF  : 'if' ;
ELS : 'else' ;
BRK : 'break' ;
CTN : 'continue' ;
WHL : 'while' ;
DO  : 'do' ;
FOR : 'for' ;
GET : 'get' ;
SET : 'set' ;
DEF : 'def' ;
DEL : 'del' ;
FUN : 'func' ;
OP  : 'oper' ;
INI : 'init' ;
CRE : 'create' ;
BND : 'bind' ;
ENT : 'enter' ;
EXI : 'exit' ;
RET : 'return' ;
YLD : 'yield' ;
CLS : 'class' ;
THR : 'throw' ;
TRY : 'try' ;
WHN : 'when' ;
AS  : 'as' ;
IS  : 'is' ;
SWC : 'switch' ;
CAS : 'case' ;
WTH : 'with' ;
OF  : 'of' ;
ERR : 'err' ;
NOT : 'not' ;
AND : 'and' ;
OR :  'or' ;

ID  : [A-Za-z_][A-Za-z0-9_]* ;

WS  : [ \t\r\n]+ -> skip ;
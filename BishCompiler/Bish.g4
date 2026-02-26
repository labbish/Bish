grammar Bish;

program
    : stat* EOF
    ;

stat
    : expr END                                                  # ExprStat
    | BRK ID? END                                               # BreakStat
    | CTN ID? END                                               # ContinueStat
    | RET expr END                                              # ReturnStat
    | IF '(' cond=expr ')' left=stat (ELS right=stat)?          # IfStat
    | tag? WHL '(' expr ')' stat                                # WhileStat
    | tag? DO stat WHL '(' expr ')' END                         # DoWhileStat
    | tag? FOR '(' forStats ')' stat                            # ForStat
    | tag? FOR '(' name=ID set=':'? ':' expr ')' stat           # ForIterStat
    | TRY tryStat=stat (CTH ('(' ID ')')? (('=>' catchExpr=expr END)
        | ('{' catchStat=stat* '}')))? (FIN finallyStat=stat)?  # ErrorStat
    | SWC expr '{' caseStat* '}'                                # SwitchStat
    | '{' stat* '}'                                             # BlockStat
    | END                                                       # EmptyStat
    ;

expr
    : '(' expr ')'                                              # ParenExpr
    | deco* '(' defArgs ')' '=>' (expr | '{' stat* '}')         # FuncExpr
    | deco* FUN ID? '(' defArgs ')'
        (('=>' expr) | ('{' stat* '}'))                         # FuncExpr
    | deco* CLS ID? (':' args)? ('{' stat* '}')?                # ClassExpr
    | '[' args ']'                                              # ListExpr
    | expr nullAccess+                                          # GetAccess
    | expr SWC '{' (caseExpr (',' caseExpr)* ','?)? '}'         # SwitchExpr
    | <assoc=right> op=('+'|'-'|'~') expr                       # UnOpExpr
    | <assoc=right> left=expr op='^' right=expr                 # BinOpExpr
    | left=expr op=('*'|'/'|'%') right=expr                     # BinOpExpr
    | left=expr op=('+'|'-') right=expr                         # BinOpExpr
    | left=expr op='<=>' right=expr                             # BinOpExpr
    | left=expr op=('<'|'<='|'>'|'>=') right=expr               # BinOpExpr
    | expr IS pattern                                           # MatchExpr
    | left=expr op=('=='|'!='|'==='|'!==') right=expr           # BinOpExpr
    | left=expr '&&' right=expr                                 # LogicAndExpr
    | left=expr '||' right=expr                                 # LogicOrExpr
    | left=expr '??' right=expr                                 # NullCombExpr
    | <assoc=right> cond=expr '?' left=expr ':' right=expr      # TernOpExpr
    | <assoc=right> THR expr                                    # ThrowExpr
    | <assoc=right> obj=expr index setOp? '=' value=expr        # SetIndex
    | <assoc=right> obj=expr '.' name=ID setOp? '=' value=expr  # SetMember
    | <assoc=right> name=ID setOp? '=' value=expr               # Set
    | <assoc=right> DEL obj=expr index                          # DelIndex
    | <assoc=right> DEL obj=expr '.' name=ID                    # DelMember
    | <assoc=right> DEL name=ID                                 # Del
    | <assoc=right> name=ID ':=' value=expr                     # Def
    | atom                                                      # AtomExpr
    ;

nullAccess
    : op='?'? access
    ;

access
    : '(' args ')'                                              # CallAccess
    | index                                                     # IndexAccess
    | '.' ID                                                    # MemberAccess
    ;

forStats
    : init=expr END cond=expr END step=expr
    ;

tag : ID ':' ;

index
    : '[' expr ']'                                              # SingleIndex
    | '[' start=expr? ':' end=expr? (':' step=expr)? ']'        # RangeIndex
    ;

caseExpr
    : pattern '=>' expr
    ;

caseStat
    : CAS pattern ':' stat
    ;

pattern
    : '_'                                                       # DefaultPattern
    | NUL                                                       # NullPattern
    | '(' pattern ')'                                           # ParenPattern
    | expr                                                      # ExprPattern
    | op=matchOp expr                                           # OpPattern
    | 'of' type=expr ID?                                        # TypePattern
    | 'not' pattern                                             # NotPattern
    | left=pattern 'and' right=pattern                          # AndPattern
    | left=pattern 'or' right=pattern                           # OrPattern
    ;

matchOp
    : '<'|'<='|'>'|'>='|'=='|'!='
    ;

setOp
    : '+'|'-'|'*'|'/'|'%'|'^'|'&&'|'||'|'??'
    ;

args
    : (arg (',' arg)* ','?)?
    ;
arg
    : expr                                                      # SingleArg
    | '..' expr                                                 # RestArg
    ;

defArgs
    : (defArg (',' defArg)* ','?)?
    ;
defArg
    : dots='..'? name=ID ('=' expr)?
    ;

deco
    : '@' expr
    ;

atom
    : INT                                                       # IntAtom
    | NUM                                                       # NumAtom
    | STR                                                       # StrAtom
    | NUL                                                       # NullAtom
    | ID                                                        # IdAtom
    ;

END : ';' ;

BLOCK_COMMENT
    : '/*' .*? '*/' -> skip
    ;

LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;

INT : [0-9]+ ;
NUM : [0-9]+ '.' [0-9]* | '.' [0-9]+ ;

// TODO: format string (seems difficult)

STR : S1 | S2 | R11 | R12 | R13 | R14 | R21 | R22 | R23 | R24 ;

// We'll use a simple method for now
RP  : 'r';
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
DEL : 'del' ;
FUN : 'func' ;
RET : 'return' ;
CLS : 'class' ;
THR : 'throw' ;
TRY : 'try' ;
CTH : 'catch' ;
FIN : 'finally' ;

IS  : 'is' ;
SWC : 'switch' ;
CAS : 'case' ;

ID  : [A-Za-z_][A-Za-z0-9_]* ;

WS  : [ \t\r\n]+ -> skip ;
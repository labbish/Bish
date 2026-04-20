grammar Bish;

program
    : (front+=expr END)* last=expr? EOF
    ;

// define Setable : AtomExpr(IdAtom) | GetAccess not ending with call | (List|Map)Expr of Setables
expr
    : '(' expr ')'                                              # ParenExpr
    | deco* (FUN ID?)? funcBody                                 # FuncExpr
    | deco* OP defOp funcBody                                   # OperExpr
    | deco* accessOp accessItem? funcBody                       # AccessExpr
    | deco* INI funcBody                                        # InitExpr
    | deco* CRT funcBody                                        # CreateExpr
    | deco* CLS ID? (':' args)? expr?                           # ClassExpr
    | '[' args ']'                                              # ListExpr
    | '{' entries '}'                                           # MapExpr
    | '{' objEntries '}'                                        # ObjExpr
    | expr nullAccess+                                          # GetAccess
    | <assoc=right> op=('+'|'-'|'!'|'~') expr                   # UnOpExpr
    | <assoc=right> left=expr op='^' right=expr                 # BinOpExpr
    | left=expr op=('*'|'/'|'%') right=expr                     # BinOpExpr
    | left=expr op=('+'|'-') right=expr                         # BinOpExpr
    | left=expr op='<=>' right=expr                             # BinOpExpr
    | left=expr op=('<'|'<='|'>'|'>=') right=expr               # BinOpExpr
    | expr IS pattern                                           # MatchExpr
    | obj=expr AS type=expr                                     # AsExpr
    | left=expr op=('=='|'!='|'==='|'!==') right=expr           # BinOpExpr
    | left=expr '&&' right=expr                                 # LogicAndExpr
    | left=expr '||' right=expr                                 # LogicOrExpr
    | left=expr '??' right=expr                                 # NullCombExpr
    | expr pipe+                                                # PipeExpr
    // In the following 3 cases, obj is Setable
    | <assoc=right> obj=expr setOp? '=' value=expr              # Set
    | <assoc=right> obj=expr ':=' value=expr                    # Def
    | <assoc=right> DEL obj=expr                                # Del
    | IF '(' cond=expr ')' left=expr (ELS right=expr)?          # IfExpr
    | tag? WHL '(' cond=expr ')' loop=expr                      # WhileExpr
    | tag? DO loop=expr WHL '(' cond=expr ')'                   # DoWhileExpr
    // obj is Setable
    | tag? FOR '(' obj=expr ':' iter=expr ')' loop=expr         # ForExpr
    | TRY expr                                                  # TryExpr
    // obj is Setable
    | WTH '(' (obj=expr ':')? cont=expr ')' main=expr           # WithExpr
    | expr SWC '{' (caseExpr (',' caseExpr)* ','?)? '}'         # SwitchExpr
    | <assoc=right> THR expr                                    # ThrowExpr
    | <assoc=right> BRK ID?                                     # BreakExpr
    | <assoc=right> CTN ID?                                     # ContinueExpr
    | <assoc=right> RET expr?                                   # ReturnExpr
    | <assoc=right> YLD gen='*'? expr                           # YieldExpr
    | '{' (front+=expr END)* last=expr? '}'                     # BlockExpr
    | atom                                                      # AtomExpr
    | '$'                                                       # PipeVarExpr
    ;

objEntries
    : (objEntry (',' objEntry)* ','?)?
    ;

objEntry
    : '.' ID (':' expr)?
    ;

entries
    : (entry (',' entry)* ','?)?
    ;

entry
    : key=expr ':' value=expr                                   # SingleEntry
    | '..' expr                                                 # RestEntry
    ;

pipe
    : '|' op='?'? '>' expr
    ;

funcBody
    : '(' defArgs ')' gen='*'? expr
    ;

accessOp
    : GET | SET | DEF | DEL
    ;

accessItem 
    : ID | '[' ']'
    ;

defOp
    : '=='|'!='|'+'|'-'|'*'|'/'|'%'|'^'
    | ('(' ')')|'<=>'|'<'|'<='|'>'|'>='|'~'
    ;

nullAccess
    : op='?'? access
    ;

access
    : '(' args ')'                                              # CallAccess
    | index                                                     # IndexAccess
    | '.' ID                                                    # MemberAccess
    ;

tag : ID ':' ;

index
    : '[' expr ']'                                              # SingleIndex
    | '[' start=expr? ':' end=expr? (':' step=expr)? ']'        # RangeIndex
    ;

caseExpr
    : pattern '=>' expr
    ;

// We handle _ in ExprPattern to allow it to be used as an ID in other places
pattern
    : NUL                                                       # NullPattern
    | '(' pattern ')'                                           # ParenPattern
    | '[' (patternItem (',' patternItem)* ','?)? ']'            # ListPattern
    | '{' (patternEntry (',' patternEntry)* ','?)? '}'          # MapPattern
    | '{' (patternObjEntry (',' patternObjEntry)* ','?)? '}'    # ObjPattern
    | expr                                                      # ExprPattern
    | op=matchOp expr                                           # OpPattern
    | 'of' type=expr var=expr?                                  # TypePattern
    | 'err' expr?                                               # ErrPattern
    | 'not' pattern                                             # NotPattern
    | left=pattern 'and' right=pattern                          # AndPattern
    | left=pattern 'or' right=pattern                           # OrPattern
    | pattern WHN expr                                          # WhenPattern
    ;

patternItem
    : dots='..'? pattern
    ;

patternEntry
    : expr ':' pattern                                          # SinglePatternEntry
    | '..' pattern                                              # RestPatternEntry
    ;

patternObjEntry
    : '.' ID ':' pattern
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
    : dots='..'? obj=expr (':' def=expr)?
    ;

deco
    : '@' expr
    ;

atom
    : INT                                                       # IntAtom
    | NUM                                                       # NumAtom
    | STR                                                       # StrAtom
    | NUL                                                       # NullAtom
    | BOL                                                       # BoolAtom
    | ID                                                        # IdAtom
    ;

END : ';' ;

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

// TODO: format string (seems difficult)

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
CRT : 'create' ;
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

ID  : [A-Za-z_][A-Za-z0-9_]* ;

WS  : [ \t\r\n]+ -> skip ;
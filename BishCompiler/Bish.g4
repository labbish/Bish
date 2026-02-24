grammar Bish;

program
    : stat* EOF
    ;

stat
    : expr END                                                  # ExprStat
    | RET expr END                                              # ReturnStat
    | IF '(' cond=expr ')' left=stat (ELS right=stat)?          # IfStat
    | WHL '(' expr ')' stat                                     # WhileStat
    | DO stat WHL '(' expr ')' END                              # DoWhileStat
    | FOR '(' init=expr END cond=expr END step=expr ')' stat    # ForStat
    | FOR '(' name=ID ':' expr ')' stat                         # ForIterStat
    | '{' stat* '}'                                             # BlockStat
    | END                                                       # EmptyStat
    ;

expr
    : '(' expr ')'                                              # ParenExpr
    | '(' defArgs ')' '=>' (expr | '{' stat* '}')               # FuncExpr
    | FUN ID? '(' defArgs ')' (('=>' expr) | ('{' stat* '}'))   # FuncExpr
    | CLS ID? (':' args)? ('{' stat* '}')?                      # ClassExpr
    | '[' args ']'                                              # ListExpr
    | func=expr '(' args ')'                                    # CallExpr
    | expr '.' name=ID                                          # GetMember
    | <assoc=right> op=('+'|'-'|'~') expr                       # UnOpExpr
    | <assoc=right> left=expr op='^' right=expr                 # BinOpExpr
    | left=expr op=('*'|'/'|'%') right=expr                     # BinOpExpr
    | left=expr op=('+'|'-') right=expr                         # BinOpExpr
    | left=expr op='<=>' right=expr                             # BinOpExpr
    | left=expr op=('<'|'<='|'>'|'>=') right=expr               # BinOpExpr
    | left=expr op=('=='|'!=') right=expr                       # BinOpExpr
    | left=expr '&&' right=expr                                 # LogicAndExpr
    | left=expr '||' right=expr                                 # LogicOrExpr
    | <assoc=right> cond=expr '?' left=expr ':' right=expr      # TernOpExpr
    | <assoc=right> obj=expr '.' name=ID '=' value=expr         # SetMember
    | <assoc=right> name=ID '=' value=expr                      # Set
    | <assoc=right> DEL obj=expr '.' name=ID                    # DelMember
    | <assoc=right> DEL name=ID                                 # Del
    // TODO: op-assign (e.g. +=)
    | <assoc=right> name=ID ':=' value=expr                     # Def
    | atom                                                      # AtomExpr
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

atom
    : INT                                                       # IntAtom
    | NUM                                                       # NumAtom
    | STR                                                       # StrAtom
    | ID                                                        # IdAtom
    ;

END : ';' ;

INT : [0-9]+ ;
NUM : [0-9]+ '.' [0-9]* | '.' [0-9]+ ;

STR : S1 | S2 ;
S1  : '"' ( ~('\\'|'"') | '\\' . )* '"' ;
S2  : '\'' ( ~('\\'|'\'') | '\\' . )* '\'' ;

IF  : 'if' ;
ELS : 'else' ;
WHL : 'while' ;
DO  : 'do' ;
FOR : 'for' ;
DEL : 'del' ;
FUN : 'func' ;
RET : 'return' ;
CLS : 'class' ;

ID  : [A-Za-z_][A-Za-z0-9_]* ;

WS  : [ \t\r\n]+ -> skip ;
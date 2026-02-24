grammar Bish;

program : stat* EOF ;

stat
    : expr END                                                  # ExprStat
    | '{' stat* '}'                                             # BlockStat
    | END                                                       # EmptyStat
    ;

expr
    : '(' expr ')'                                              # ParenExpr
    | '[' args ']'                                              # ListExpr
    | func=expr '(' args ')'                                    # CallExpr
    | expr '.' name=ID                                          # GetMember
    | <assoc=right> op=('+'|'-'|'~') expr                       # UnOpExpr
    | <assoc=right> left=expr op='^' right=expr                 # BinOpExpr
    | left=expr op=('*'|'/') right=expr                         # BinOpExpr
    | left=expr op=('+'|'-') right=expr                         # BinOpExpr
    | left=expr op='<=>' right=expr                             # BinOpExpr
    | left=expr op=('<'|'<='|'>'|'>=') right=expr               # BinOpExpr
    | left=expr op=('=='|'!=') right=expr                       # BinOpExpr
    | left=expr '&&' right=expr                                 # LogicAndExpr
    | left=expr '||' right=expr                                 # LogicOrExpr
    | <assoc=right> cond=expr '?' left=expr ':' right=expr      # TernOpExpr
    | <assoc=right> obj=expr '.' name=ID '=' value=expr         # SetMember
    | <assoc=right> name=ID '=' value=expr                      # Set
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

atom
    : INT                                                       # IntAtom
    | NUM                                                       # NumAtom
    | STR                                                       # StrAtom
    | ID                                                        # IdAtom
    ;

END : ';' ;

INT : [0-9]+ ;
NUM : [0-9]+ '.' [0-9]* | '.' [0-9]+ ;

STR : S1 | S2;
S1  : '"' ( ~('\\'|'"') | '\\' . )* '"' ;
S2  : '\'' ( ~('\\'|'\'') | '\\' . )* '\'' ;

ID  : [A-Za-z_][A-Za-z0-9_]* ;

WS  : [ \t\r\n]+ -> skip ;
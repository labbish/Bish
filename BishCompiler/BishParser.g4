parser grammar BishParser;
options { tokenVocab=BishLexer; }

program
    : (front+=expr END)* last=expr? EOF
    ;

// define Setable : AtomExpr(IdAtom) | GetAccess not ending with call | (List|Map)Expr of Setables
expr
    : LPAREN expr RPAREN                                        # ParenExpr
    | deco* (FUN ID?)? funcBody                                 # FuncExpr
    | deco* OP defOp funcBody                                   # OperExpr
    | deco* accessOp accessItem? funcBody                       # AccessExpr
    | deco* defHook funcBody                                    # HookExpr
    | deco* CLS ID? (COL args)? expr?                           # ClassExpr
    | EXT obj=expr body=expr?                                   # ExtendExpr
    | LBRACK args RBRACK                                        # ListExpr
    | LBRACE entries RBRACE                                     # MapExpr
    | LBRACE objEntries RBRACE                                  # ObjExpr
    | expr nullAccess+                                          # GetAccess
    | <assoc=right> AWT expr                                    # AwaitExpr
    | <assoc=right> op=(ADD|SUB|BANG|INVERT) expr               # UnOpExpr
    | <assoc=right> left=expr op=POW right=expr                 # BinOpExpr
    | left=expr op=(MUL|DIV|MOD) right=expr                     # BinOpExpr
    | left=expr op=(ADD|SUB) right=expr                         # BinOpExpr
    | left=expr op=TRI right=expr                               # BinOpExpr
    | left=expr op=(LT|LE|GT|GE) right=expr                     # BinOpExpr
    | expr IS pattern                                           # MatchExpr
    | obj=expr AS type=expr                                     # AsExpr
    | left=expr op=(EQ|NEQ|REQ|NREQ) right=expr                 # BinOpExpr
    | left=expr BAND right=expr                                 # LogicAndExpr
    | left=expr BOR right=expr                                  # LogicOrExpr
    | left=expr NCOMB right=expr                                # NullCombExpr
    | expr pipe+                                                # PipeExpr
    // In the following 3 cases, obj is Setable
    | <assoc=right> obj=expr setOp? SETS value=expr             # Set
    | <assoc=right> obj=expr DEFS value=expr                    # Def
    | <assoc=right> DEL obj=expr                                # Del
    | IF LPAREN cond=expr RPAREN left=expr (ELS right=expr)?    # IfExpr
    | tag? WHL LPAREN cond=expr RPAREN loop=expr                # WhileExpr
    | tag? DO loop=expr WHL LPAREN cond=expr RPAREN             # DoWhileExpr
    // obj is Setable
    | tag? forBody loop=expr                                    # ForExpr
    | TRY expr                                                  # TryExpr
    // obj is Setable
    | withBody main=expr                                        # WithExpr
    | expr SWC LBRACE (caseExpr (COM caseExpr)* COM?)? RBRACE   # SwitchExpr
    | <assoc=right> THR expr                                    # ThrowExpr
    | <assoc=right> BRK ID?                                     # BreakExpr
    | <assoc=right> CTN ID?                                     # ContinueExpr
    | <assoc=right> RET expr?                                   # ReturnExpr
    | <assoc=right> YLD await=AWT? gen=MUL? expr                # YieldExpr
    | <assoc=right> SHARP macro=expr? LBRACK body=expr RBRACK   # MacroExpr
    | LBRACE (front+=expr END)* last=expr? RBRACE               # BlockExpr
    | atom                                                      # AtomExpr
    | PIPE                                                      # PipeVarExpr
    ;

forBody
    : FOR AWT? LPAREN obj=expr COL iter=expr RPAREN
    ;

withBody
    : WTH AWT? LPAREN (obj=expr COL)? cont=expr RPAREN
    ;

objEntries
    : (objEntry (COM objEntry)* COM?)?
    ;

objEntry
    : DOT ID (COL expr)?
    ;

entries
    : (entry (COM entry)* COM?)?
    ;

entry
    : key=expr COL value=expr                                   # SingleEntry
    | REST expr                                                 # RestEntry
    ;

pipe
    : BAR op=QUES? GT expr
    ;

funcBody
    : LPAREN defArgs RPAREN async=ASY? gen=MUL? expr
    ;

accessOp
    : GET | SET | DEF | DEL
    ;

accessItem 
    : ID | LBRACK RBRACK
    ;

defOp
    : EQ|NEQ|ADD|SUB|MUL|DIV|MOD|POW
    | (LPAREN RPAREN)|TRI|LT|LE|GT|GE|INVERT
    ;

defHook
    : NEW | BND | ENT | EXI
    ;

nullAccess
    : op=QUES? access
    ;

access
    : LPAREN args RPAREN                                        # CallAccess
    | index                                                     # IndexAccess
    | DOT ID                                                    # MemberAccess
    ;

tag : ID COL ;

index
    : LBRACK expr RBRACK                                        # SingleIndex
    | LBRACK start=expr? COL end=expr? (COL step=expr)? RBRACK  # RangeIndex
    ;

caseExpr
    : pattern ARROW expr
    ;

// We handle _ in ExprPattern to allow it to be used as an ID in other places
pattern
    : NUL                                                       # NullPattern
    | LPAREN pattern RPAREN                                     # ParenPattern
    | LBRACK (patItem (COM patItem)* COM?)? RBRACK              # ListPattern
    | LBRACE (patEntry (COM patEntry)* COM?)? RBRACE            # MapPattern
    | LBRACE (patObjEntry (COM patObjEntry)* COM?)? RBRACE      # ObjPattern
    | expr                                                      # ExprPattern
    | op=matchOp expr                                           # OpPattern
    | OF type=expr var=expr?                                    # TypePattern
    | ERR expr?                                                 # ErrPattern
    | NOT pattern                                               # NotPattern
    | left=pattern AND right=pattern                            # AndPattern
    | left=pattern OR right=pattern                             # OrPattern
    | pattern WHN expr                                          # WhenPattern
    ;

patItem
    : dots=REST? pattern
    ;

patEntry
    : expr COL pattern                                          # SinglePatternEntry
    | REST pattern                                              # RestPatternEntry
    ;

patObjEntry
    : DOT ID COL pattern
    ;

matchOp
    : LT|LE|GT|GE|EQ|NEQ
    ;

setOp
    : ADD|SUB|MUL|DIV|MOD|POW|BAND|BOR|NCOMB
    ;

args
    : (arg (COM arg)* COM?)?
    ;
arg
    : expr                                                      # SingleArg
    | REST expr                                                 # RestArg
    ;

defArgs
    : (defArg (COM defArg)* COM?)?
    ;
defArg
    : dots=REST? obj=expr (COL def=expr)?
    ;

deco
    : AT expr
    ;

atom
    : INT                                                       # IntAtom
    | NUM                                                       # NumAtom
    | STR                                                       # StrAtom
    | NUL                                                       # NullAtom
    | BOL                                                       # BoolAtom
    | ID                                                        # IdAtom
    ;
parser grammar BishParser;
options { tokenVocab=BishLexer; }

program
    : (expr END)* expr? EOF
    ;

expr
    : LPAREN expr RPAREN                                        # ParenExpr
    | deco* (FUN id?)? funcBody                                 # FuncExpr
    | deco* OP defOp funcBody                                   # OperExpr
    | deco* accessOp accessItem? funcBody                       # AccessExpr
    | deco* defHook funcBody                                    # HookExpr
    | deco* CLS (LBRACK expr RBRACK)? id? (COL args)? expr?     # ClassExpr
    | EXT expr expr                                             # ExtendExpr
    | LBRACK args RBRACK                                        # ListExpr
    | LBRACE (entries | COL) RBRACE                             # MapExpr
    | LBRACE (objEntries | DOT) RBRACE                          # ObjExpr
    | expr nullAccess+                                          # GetAccess
    | <assoc=right> AWT expr                                    # AwaitExpr
    | <assoc=right> (ADD|SUB|BANG|INVERT) expr                  # UnOpExpr
    | <assoc=right> expr POW expr                               # BinOpExpr
    | expr (MUL|DIV|MOD) expr                                   # BinOpExpr
    | expr (ADD|SUB) expr                                       # BinOpExpr
    | expr TRI expr                                             # BinOpExpr
    | expr (LT|LE|GT|GE) expr                                   # BinOpExpr
    | expr IS pattern                                           # MatchExpr
    | expr AS expr                                              # AsExpr
    | expr (EQ|NEQ|REQ|NREQ) expr                               # BinOpExpr
    | expr BAND expr                                            # LogicAndExpr
    | expr BOR expr                                             # LogicOrExpr
    | expr NCOMB expr                                           # NullCombExpr
    | expr pipe+                                                # PipeExpr
    | <assoc=right> expr setOp? SETS expr                       # Set
    | <assoc=right> expr DEFS expr                              # Def
    | <assoc=right> DEL expr                                    # Del
    | IF LPAREN expr RPAREN expr (ELS expr)?                    # IfExpr
    | tag? WHL LPAREN expr RPAREN expr                          # WhileExpr
    | tag? DO expr WHL LPAREN expr RPAREN                       # DoWhileExpr
    | tag? forBody expr                                         # ForExpr
    | TRY expr                                                  # TryExpr
    | withBody expr                                             # WithExpr
    | SWC expr LBRACE (caseExpr (COM caseExpr)* COM?)? RBRACE   # SwitchExpr
    | <assoc=right> THR expr                                    # ThrowExpr
    | <assoc=right> BRK id?                                     # BreakExpr
    | <assoc=right> CTN id?                                     # ContinueExpr
    | <assoc=right> RET expr?                                   # ReturnExpr
    | <assoc=right> YLD AWT? MUL? expr                          # YieldExpr
    | LBRACE (expr END)* expr? RBRACE                           # BlockExpr
    | atom                                                      # AtomExpr
    | PIPE                                                      # PipeVarExpr
    ;

forBody
    : FOR AWT? LPAREN expr COL expr RPAREN
    ;

withBody
    : WTH AWT? LPAREN (expr COL)? expr RPAREN
    ;

objEntries
    : objEntry (COM objEntry)* COM?
    ;

objEntry
    : DOT id (COL expr)?
    ;

entries
    : entry (COM entry)* COM?
    ;

entry
    : expr COL expr                                             # SingleEntry
    | REST expr                                                 # RestEntry
    ;

pipe
    : BAR QUES? GT expr
    ;

funcBody
    : LPAREN defArgs RPAREN ASY? MUL? expr
    ;

accessOp
    : GET | SET | DEF | DEL
    ;

accessItem 
    : id | LBRACK RBRACK
    ;

defOp
    : EQ|NEQ|ADD|SUB|MUL|DIV|MOD|POW
    | (LPAREN RPAREN)|TRI|LT|LE|GT|GE|INVERT
    ;

defHook
    : NEW | BND
    ;

nullAccess
    : QUES? access
    ;

access
    : LPAREN args RPAREN                                        # CallAccess
    | index                                                     # IndexAccess
    | DOT id                                                    # MemberAccess
    ;

tag : id COL ;

index
    : LBRACK expr RBRACK                                        # SingleIndex
    | LBRACK expr? COL expr? (COL expr)? RBRACK  # RangeIndex
    ;

caseExpr
    : pattern ARROW expr
    ;

// We handle _ in ExprPattern to allow it to be used as an id in other places
pattern
    : NUL                                                       # NullPattern
    | LPAREN pattern RPAREN                                     # ParenPattern
    | LBRACK (patItem (COM patItem)* COM?)? RBRACK              # ListPattern
    | LBRACE (patEntries | COL) RBRACE                          # MapPattern
    | LBRACE (patObjEntries | DOT) RBRACE                       # ObjPattern
    | expr                                                      # ExprPattern
    | matchOp expr                                              # OpPattern
    | OF expr expr?                                             # TypePattern
    | ERR expr?                                                 # ErrPattern
    | NOT pattern                                               # NotPattern
    | pattern AND pattern                                       # AndPattern
    | pattern OR pattern                                        # OrPattern
    | pattern WHN expr                                          # WhenPattern
    ;

patItem
    : REST? pattern
    ;

patEntries
    : patEntry (COM patEntry)* COM?
    ;

patEntry
    : expr COL pattern                                          # SinglePatternEntry
    | REST pattern                                              # RestPatternEntry
    ;

patObjEntries
    : patObjEntry (COM patObjEntry)* COM?
    ;

patObjEntry
    : DOT id COL pattern
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
    : REST? expr (COL expr)?
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
    | id                                                        # IdAtom
    ;

id
    : ID                                                        # SimpleId
    | SHARP LPAREN STR RPAREN                                   # StrId
    ;
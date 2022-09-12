grammar Enlyn;

program : classList? EOF;
end     : ';' | NEWLINE;

classList : classDefinition (end classDefinition)*;
classDefinition : CLASS id = IDENTIFIER (':' parent = IDENTIFIER)? '{' memberList? '}';

memberList : member (end member)*;
visibility : access = (PUBLIC | PROTECTED | PRIVATE);
member
    : visibility id = IDENTIFIER ':' typeExpr ('=' expr)?                                     # field
    | visibility OVERRIDE? methodName '(' paramList? ')' ('->' typeExpr)? (block | '=' stmt)  # method
    | visibility NEW '(' paramList? ')' (':' BASE '(' exprList? ')')?     (block | '=' stmt)  # constructor
    ;

methodName
    : id = IDENTIFIER
    | BINARY op = ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '==' | '!=' | '<' | '>' | '<=' | '>=')
    | UNARY op = ('-' | '!')
    ;

paramList : param (',' param)*;
param : id = IDENTIFIER ':' typeExpr;

stmtList : stmt (end stmt)*;
block : '{' stmtList? '}';
stmt
    : expr                                                     #exprStmt
    ;

typeExpr
    : typeExpr '?'                                             # option
    | value = IDENTIFIER                                       # type
    ;

exprList : expr (',' expr)*;
expr
    : expr '.' id = IDENTIFIER                                 # access
    | expr '(' exprList? ')'                                   # call
    | NEW type = IDENTIFIER '(' exprList? ')'                  # new
    | expr '!'                                                 # assert

    | op = ('-' | '!') expr                                    # unary
    | left = expr op = ('*' | '/' | '%')         right = expr  # binary
    | left = expr op = ('+' | '-')               right = expr  # binary
    | left = expr op = ('<' | '>' | '<=' | '>=') right = expr  # binary
    | left = expr op = ('==' | '!=')             right = expr  # binary
    | left = expr op = '&'                       right = expr  # binary
    | left = expr op = '|'                       right = expr  # binary
    | <assoc = right> target = expr '=' value = expr           # assign
    | '(' expr ')'                                             # group

    | value = IDENTIFIER                                       # identifier
    | value = NUMBER                                           # number
    | value = STRING                                           # string
    | value = BOOLEAN                                          # boolean
    | THIS                                                     # this
    | BASE                                                     # base
    | NULL                                                     # null
    ;

WHITESPACE : [ \t]+ -> skip;
NEWLINE    : NEXT | COMMENT;

AND        : '&';
OR         : '|';

EQUAL      : '=';
DEQUAL     : '==';
NEQUAL     : '!=';

LESS       : '<';
GREATER    : '>';
LEQUAL     : '<=';
GEQUAL     : '>=';

PLUS       : '+';
MINUS      : '-';
STAR       : '*';
SLASH      : '/';
PERCENT    : '%';

DOT        : '.';
COMMA      : ',';
COLON      : ':';
SEMI       : ';';

QUESTION   : '?';
EXCLAIM    : '!';
ARROW      : '->';

LPAREN     : '(';
RPAREN     : ')';
LBRACE     : '{';
RBRACE     : '}';

CLASS      : 'class';
NEW        : 'new';

PUBLIC     : 'public';
PRIVATE    : 'private';
PROTECTED  : 'protected';
OVERRIDE   : 'override';

BINARY     : 'binary';
UNARY      : 'unary';

LET        : 'let';
RETURN     : 'return';

IF         : 'if';
THEN       : 'then';
ELSE       : 'else';

MATCH      : 'match';
CASE       : 'case';
DEFAULT    : 'default';

WHILE      : 'while';
DO         : 'do';

THIS       : 'this';
BASE       : 'base';

BOOLEAN    : 'true' | 'false';
NULL       : 'null';

IDENTIFIER : LETTER (LETTER | DIGIT)*;
NUMBER     : '-'? (INT | FLOAT) EXPONENT?;
STRING     : '"' (~["\r\n\\] | ESCAPE)* '"';

fragment NEXT     : [\r\n] | '\r\n';
fragment COMMENT  : '//' ~[\r\n]* NEXT;

fragment DIGIT    : [0-9];
fragment LETTER   : [a-zA-Z_];

fragment INT      : DIGIT+;
fragment FLOAT    : DIGIT* '.' DIGIT+;
fragment EXPONENT : [eE] [+-]? DIGIT+;

fragment ESCAPE   : '\\' [tnrbf"\\];

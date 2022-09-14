grammar Enlyn;

program : classList? EOF;
end     : ';' | NEWLINE;

classList : classDefinition (end classDefinition)*;
classDefinition : CLASS id = IDENTIFIER (':' parent = IDENTIFIER)? '{' memberList? '}';

memberList : member (end member)*;
visibility : access = (PUBLIC | PROTECTED | PRIVATE);
member
    : visibility id = IDENTIFIER ':' type ('=' expr)?                                     # field
    | visibility OVERRIDE? methodName '(' paramList? ')' ('->' type)? (block | '=' stmt)  # method
    | visibility NEW '(' paramList? ')' (':' BASE '(' exprList? ')')? (block | '=' stmt)  # constructor
    ;

methodName
    : id = IDENTIFIER
    | BINARY op = ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '==' | '!=' | '<' | '>' | '<=' | '>=')
    | UNARY  op = ('-' | '!')
    ;

paramList : param (',' param)*;
param : id = IDENTIFIER ':' type;

stmtList : stmt (end stmt)*;
stmt
    : LET id = IDENTIFIER (':' type)? '=' expr                 # let
    | IF expr (block | THEN stmt) elseBranch?                  # if
    | WHILE expr (block | DO stmt)                             # while
    | RETURN expr?                                             # return
    | expr                                                     # exprStmt
    ;

block      : '{' stmtList? '}';
elseBranch : ELSE (block | stmt);

type
    : type '?'                                                 # option
    | value = IDENTIFIER                                       # typeIdentifier
    ;

exprList : expr (',' expr)*;
expr
    : expr '.' id = IDENTIFIER                                 # access
    | expr '(' exprList? ')'                                   # call
    | NEW id = IDENTIFIER '(' exprList? ')'                    # new
    | expr '!'                                                 # assert

    | '(' expr ')'                                             # group
    | op = ('-' | '!') expr                                    # unary
    | left = expr op = ('*' | '/' | '%')         right = expr  # binary
    | left = expr op = ('+' | '-')               right = expr  # binary
    | left = expr op = ('<' | '>' | '<=' | '>=') right = expr  # binary
    | left = expr op = ('==' | '!=')             right = expr  # binary
    | left = expr op = '&'                       right = expr  # binary
    | left = expr op = '|'                       right = expr  # binary

    | <assoc = right> target = expr '=' value = expr           # assign
    | expr IS type                                             # instance
    | expr AS type                                             # cast

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

IS         : 'is';
AS         : 'as';

LET        : 'let';
RETURN     : 'return';

IF         : 'if';
THEN       : 'then';
ELSE       : 'else';

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

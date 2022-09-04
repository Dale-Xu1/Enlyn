grammar Enlyn;

expr :
    | expr '+' expr
    | expr '-' expr
    | expr '*' expr
    | expr '/' expr
    | '(' expr ')'
    | literal
    ;

literal :
    | IDENTIFIER
    | NUMBER
    | STRING
    ;

WHITESPACE : [ \t]+ -> skip ;
NEWLINE    : NEXT | COMMENT ;

AND        : '&' ;
OR         : '|' ;

EQUAL      : '=' ;
DEQUAL     : '==' ;
NEQUAL     : '!=' ;

LESS       : '<' ;
GREATER    : '>' ;
LEQUAL     : '<=' ;
GEQUAL     : '>=' ;

PLUS       : '+' ;
MINUS      : '-' ;
STAR       : '*' ;
SLASH      : '/' ;
PERCENT    : '%' ;

DOT        : '.' ;
COMMA      : ',' ;
COLON      : ':' ;
SEMI       : ';' ;

QUESTION   : '?' ;
EXCLAIM    : '!' ;
ARROW      : '->' ;

LPAREN     : '(' ;
RPAREN     : ')' ;
LBRACE     : '{' ;
RBRACE     : '}' ;

CLASS      : 'class' ;
NEW        : 'new' ;

PUBLIC     : 'public' ;
PRIVATE    : 'private' ;
PROTECTED  : 'protected' ;
OVERRIDE   : 'override' ;

BINARY     : 'binary' ;
UNARY      : 'unary' ;

THIS       : 'this' ;
BASE       : 'base' ;

LET        : 'let' ;
RETURN     : 'return' ;

IF         : 'if' ;
THEN       : 'then' ;
ELSE       : 'else' ;

MATCH      : 'match' ;
CASE       : 'case' ;
DEFAULT    : 'default' ;

WHILE      : 'while' ;
DO         : 'do' ;

TRUE       : 'true' ;
FALSE      : 'false' ;
NULL       : 'null' ;

IDENTIFIER : LETTER (LETTER | DIGIT)* ;
NUMBER     : '-'? (INT | FLOAT) EXPONENT? ;
STRING     : '"' (~["\r\n\\] | ESCAPE)* '"' ;

fragment NEXT     : [\r\n] | '\r\n' ;
fragment COMMENT  : '//' ~[\r\n]* NEXT ;

fragment DIGIT    : [0-9] ;
fragment LETTER   : [a-zA-Z_] ;

fragment INT      : DIGIT+ ;
fragment FLOAT    : DIGIT* '.' DIGIT+ ;
fragment EXPONENT : [eE] [+-]? DIGIT+ ;

fragment ESCAPE   : '\\' [tnrbf"\\] ;

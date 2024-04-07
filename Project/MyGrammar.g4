grammar MyGrammar;

// Comments, whitespace, and newlines are ignored
INT: [0-9]+;
FLOAT: [0-9]+'.'[0-9]*;
BOOL: 'true' | 'false';
//STRING: ('"' [a-zA-Z0-9(){}<>,._!?:/*+%=; ]* '-'? [a-zA-Z0-9(){}<>,._!?:/*+%=; ]* '"') | '""';
STRING: '"' ( '\\' . | ~["\\])* '"' ;
ID: [a-zA-Z_][a-zA-Z0-9_]* ;
WS: [ \t\r\n]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
SEMI: ';';
COMMA: ',';

// Program
program : statement+ EOF;

statementList : statement (statement)*;

// Statements
statement : 
          varDecl SEMI 
         | emptyStmt SEMI
         | expr SEMI
         | readStmt SEMI
         | writeStmt SEMI
         | blockStmt
         | ifStmt
         | whileStmt;

emptyStmt : ;

varDecl : type ID (COMMA ID)*;

type : 'int' | 'float' | 'bool' | 'string';

expr : 
      literal 
     | ID 
     | expr op expr
     | parenExpr 
     | unaryExpr
     | assign;

literal : INT | FLOAT | BOOL | STRING;

parenExpr : '(' expr ')';

unaryExpr : SUB expr | NOT expr;

op : ADD | SUB | MUL | DIV | MOD 
   | LT | GT | LE | GE 
   | EQ | NE 
   | AND | OR 
   | NOT | DOT;

ADD : '+';
SUB : '-';
MUL : '*';
DIV : '/';
MOD : '%';

LT : '<';
GT : '>';
LE : '<=' ;
GE : '>=';

EQ : '==';
NE : '!=';

AND : '&&';
OR : '||';

NOT : '!';

DOT : '.';

// Assignment (treated as an expression with side effects)
assign : ID '=' expr;

// Input/Output
readStmt : 'read' ID (COMMA ID)*;

writeStmt : 'write' expr (COMMA expr)*;

// Block of statements
blockStmt : '{' statement+ '}';

// Conditional statement
ifStmt : 'if' '(' expr ')' statement ('else' statement)?;

// While loop
whileStmt : 'while' '(' expr ')' statement;
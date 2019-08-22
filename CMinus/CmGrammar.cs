namespace CMinus
{
    public static class CmGrammar
    {
        public static readonly (string, string)[] Patterns =
        {
            ("RELOP", @">=|<=|==|!=|>|<"),
            ("ADDOP", @"+|-"),
            ("MULOP", @"\*|/"),
            ("INT", @"\d+"),
            ("FLOAT", @"\d+\.\d+?([eE]\d+)?"),
            ("ID", @"[A-Za-z]+")
        };

        public static readonly string[] Grammar =
        {
            "program -> declaration-list",
            "declaration-list -> declaration-list declaration | declaration",
            "declaration -> var-declaration | fun-declaration",
            "var-declaration -> type-specifier ID ; | type-specifier ID [ INT ] ;",
            "type-specifier -> int | void | float",
            "fun-declaration -> type-specifier ID ( params ) compound-stmt",
            "params -> param-list | void",
            "param-list -> param-list , param | param",
            "param -> type-specifier ID | type-specifier ID [ ]",
            "compound-stmt -> { local-declarations statement-list }",
            "local-declarations -> local-declarations var-declaration | #",
            "statement-list -> statement-list statement | #",
            "statement -> expression-stmt | compound-stmt | selection-stmt | iteration-stmt | return-stmt",
            "expression-stmt -> expression ; | ;",
            "selection-stmt -> if ( expression ) statement | if ( expression ) statement else statement",
            "iteration-stmt -> while ( expression ) statement",
            "return-stmt -> return ; | return expression ;",
            "expression -> var = expression | simple-expression",
            "var -> ID | ID [ expression ]",
            "simple-expression -> additive-expression RELOP additive-expression | additive-expression",
            "additive-expression -> additive-expression ADDOP term | term",
            "term -> term MULOP factor | factor",
            "factor -> ( expression ) | var | call | INT | FLOAT",
            "call -> ID ( args )",
            "args -> arg-list | #",
            "arg-list -> arg-list , expression | expression"
        };
    }
}
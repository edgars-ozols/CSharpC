namespace Compiler.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        IntegerLiteralToken,
        
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        PercentToken,
        
        BangToken,
        
        LessToken,
        LessOrEqualToken,
        GreaterToken,
        GreaterOrEqualToken,
        
        BangEqualsToken,
        EqualsEqualsToken,
        
        EqualsToken,
        
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        SemicolonToken,

        // Keyworkds
        FalseKeyword,
        TrueKeyword,
        
        BoolKeyword,
        IntKeyword,

        NumericLiteralExpression,
        BooleanLiteralExpression,
        
        Identifier,
        
        // Expressions
        EmptyExpression,
        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,

        // Statements
        BlockStatement,
        EmptyStatement,
        LocalVariableDeclarationStatement,
    }
}
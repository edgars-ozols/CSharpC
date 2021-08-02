using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler.CodeAnalysis.Syntax
{
    public class Parser
    {
        private readonly DiagnosticBag diagnostics = new();
        private readonly SyntaxToken[] tokens;
        private int position;

        public Parser(string text)
        {
            Lexer lexer = new(text);
            var syntaxTokens = new List<SyntaxToken>();
            SyntaxToken token;
            do
            {
                token = lexer.Lex();
                if (token.Kind != SyntaxKind.WhitespaceToken)
                    syntaxTokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            tokens = syntaxTokens.ToArray();
            diagnostics.AddRange(lexer.Diagnostics);
        }

        private SyntaxToken Current => Peak(0);

        public DiagnosticBag Diagnostics => diagnostics;

        private SyntaxToken Peak(int offset)
        {
            var index = position + offset;
            if (index >= tokens.Length)
                return tokens[^1];
            return tokens[index];
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            if (Current.Kind == SyntaxKind.BadToken)
            {
                //Bad token is already reported in diagnostics.
                //Consuming bad token and replacing with expected for enabling further processing
                position++;
                return new SyntaxToken(kind, Current.Position, string.Empty, null);
            }
            
            Diagnostics.ReportUnexpectedToken(Current.TextSpan, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, string.Empty, null);
        }

        private SyntaxToken NextToken()
        {
            var current = Current;
            position++;
            return current;
        }

        public SyntaxTree Parse()
        {
            var statementBlock = ParseStatementBlock();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new SyntaxTree(Diagnostics, statementBlock, endOfFileToken);
        }

        private BlockStatement ParseStatementBlock()
        {
            var openBrace = MatchToken(SyntaxKind.OpenBrace);

            var statementList = ImmutableList.CreateBuilder<Statement>();
            
            while (Current.Kind != SyntaxKind.CloseBrace)
            {
                statementList.Add(ParseStatement());
            }

            var closeBrace = MatchToken(SyntaxKind.CloseBrace);
            return new BlockStatement(openBrace, statementList.ToImmutable(), closeBrace);

        }

        private Statement ParseStatement()
        {
            return Current.Kind switch
            {
                SyntaxKind.OpenBrace => ParseStatementBlock(),
                SyntaxKind.Semicolon => new EmptyStatement(NextToken()),
                _ => throw new NotImplementedException("Not all statement types are parsable"),
            };
        }

        private ExpressionsSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionsSyntax left;
            var unaryPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseExpression(unaryPrecedence);
                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }
            
            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;
                
                var operatorToken = NextToken();
                var right = ParseExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }
            return left;
        }

        private ExpressionsSyntax ParsePrimaryExpression()
        {
            if (Current.Kind is SyntaxKind.OpenParenthesis)
            {
                var open = NextToken();
                var expression = ParseExpression();
                var close = MatchToken(SyntaxKind.CloseParenthesis);
                return new ParenthesizedExpressionSyntax(open, expression, close);
            }

            if(Current.Kind is SyntaxKind.FalseKeyword or SyntaxKind.TrueKeyword)
                return new BooleanLiteralExpressionSyntax(NextToken());
            
            return new NumericLiteralExpressionSyntax(MatchToken(SyntaxKind.IntegerLiteralToken));
        }
    }
}
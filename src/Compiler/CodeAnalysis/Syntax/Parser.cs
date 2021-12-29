using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Compiler.CodeAnalysis.Syntax
{
    public class Parser
    {
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
            Diagnostics.AddRange(lexer.Diagnostics);
        }

        private SyntaxToken Current => Peak(0);

        public DiagnosticBag Diagnostics { get; } = new();

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
            var compilationUnit = ParseCompilationUnit();
            return new SyntaxTree(Diagnostics, compilationUnit);
        }

        private CompilationUnit ParseCompilationUnit()
        {
            var builder = ImmutableList.CreateBuilder<MethodDeclarationSyntax>();
            var startposition = position;
            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                startposition = position;
                var method = ParseMethodeDeclaration();
                if (position == startposition)
                {
                    position++;
                }
                else
                {
                    builder.Add(method);
                }
            }

            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnit(builder.ToImmutable(), endOfFileToken);
        }

        private MethodDeclarationSyntax ParseMethodeDeclaration()
        {
            var type = ParseType();

            var memberName = MatchToken(SyntaxKind.Identifier);
            var openParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);


            SeperatedSyntaxList<ParameterSyntax>? parameters = SeperatedSyntaxList<ParameterSyntax>.Empty();
            if (Current.Kind is not (SyntaxKind.CloseParenthesisToken or SyntaxKind.OpenBraceToken))
                parameters = ParseMethodeDeclarationParameters();

            var closeParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            var body = ParseStatementBlock();

            return new MethodDeclarationSyntax(type, memberName, openParenthesis, parameters, closeParenthesis, body);
        }


        private SeperatedSyntaxList<ParameterSyntax> ParseMethodeDeclarationParameters()
        {
            var builder = ImmutableList.CreateBuilder<SyntaxNode>();

            var parseNextParameter = true;
            
            while (parseNextParameter && 
                   Current.Kind != SyntaxKind.CloseBraceToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = ParseParameter();
                builder.Add(parameter);
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = NextToken();
                    builder.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeperatedSyntaxList<ParameterSyntax>(builder.ToImmutable());
        }

        private ParameterSyntax ParseParameter()
        {
            var type = ParseType();
            var identifier = MatchToken(SyntaxKind.Identifier);
            return new ParameterSyntax(type, identifier);
        }
        
        private TypeSyntax ParseType()
        {
            return SyntaxFacts.IsBuiltInType(Current.Kind)
                ? new TypeSyntax(NextToken())
                : new TypeSyntax(MatchToken(SyntaxKind.Identifier));
        }


        private BlockStatementSyntax ParseStatementBlock()
        {
            var openBrace = MatchToken(SyntaxKind.OpenBraceToken);

            var statementList = ImmutableList.CreateBuilder<StatementSyntax>();

            var startPosition = position;
            while (Current.Kind is not (SyntaxKind.CloseBraceToken or SyntaxKind.EndOfFileToken))
            {
                startPosition = position;
                statementList.Add(ParseStatement());
                if (position == startPosition)
                    position++; //Skipping tokens if we cant parse statements
            }

            var closeBrace = MatchToken(SyntaxKind.CloseBraceToken);
            return new BlockStatementSyntax(openBrace, statementList.ToImmutable(), closeBrace);
        }

        private StatementSyntax ParseStatement()
        {
            if (Current.Kind == SyntaxKind.OpenBraceToken)
                return ParseStatementBlock();
            if (Current.Kind == SyntaxKind.SemicolonToken)
                return new EmptyStatementSyntax(NextToken());
            if (IsTypeSyntax())
                return ParseDeclarationStatement();
            if (Current.Kind == SyntaxKind.ReturnKeyword)
                return ParseReturnStatement();
            return ParseExpressionStatement();
        }

        private bool IsTypeSyntax()
        {
            return Current.Kind is SyntaxKind.IntKeyword or SyntaxKind.BoolKeyword or SyntaxKind.VoidKeyword;
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            var returnKeyword = NextToken();
            if (Current.Kind == SyntaxKind.SemicolonToken)
                return new ReturnStatementSyntax(returnKeyword, null, NextToken());

            var expression = ParseBinaryExpression();
            var semicolon = MatchToken(SyntaxKind.SemicolonToken);
            return new ReturnStatementSyntax(returnKeyword, expression, semicolon);
        }

        private StatementSyntax ParseExpressionStatement()
        {
            var expression = ParseAssignmentExpression();
            var semicolon = MatchToken(SyntaxKind.SemicolonToken);
            return new ExpressionStatementSyntaxSyntax(expression, semicolon);
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Peak(0).Kind == SyntaxKind.Identifier && Peak(1).Kind == SyntaxKind.EqualsToken)
            {
                var identifier = NextToken();
                var operatorToken = NextToken();
                var right = ParseAssignmentExpression();
                return new AssignmentExpressionSyntax(identifier, operatorToken, right);
            }
            return ParseBinaryExpression();
        }

        private StatementSyntax ParseDeclarationStatement()
        {
            TypeSyntax type = ParseType();
            
            SyntaxToken? equalsToken = null;
            ExpressionSyntax? expressionsSyntax = null;

            SyntaxToken identifier = MatchToken(SyntaxKind.Identifier);

            if (Current.Kind != SyntaxKind.SemicolonToken)
            {
                equalsToken = MatchToken(SyntaxKind.EqualsToken);

                if (Current.Kind == SyntaxKind.SemicolonToken) Diagnostics.ReportMissingExpression(Current.TextSpan);

                expressionsSyntax = ParseBinaryExpression();
            }

            SyntaxToken semicolon = MatchToken(SyntaxKind.SemicolonToken);
            return new LocalVariableDeclarationStatementSyntax(type, identifier, equalsToken, expressionsSyntax,
                semicolon);
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseBinaryExpression(unaryPrecedence);
                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
            {
                left = ParseTerm();
            }

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence < parentPrecedence)
                    break;

                // right associativity 
                if (precedence == parentPrecedence && Current.Kind != SyntaxKind.EqualsToken)
                    break;

                var operatorToken = NextToken();
                var right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParseTerm()
        {
            return ParsePostfixExpression(ParsePrimaryExpression());
        }

        private ExpressionSyntax ParsePostfixExpression(ExpressionSyntax expression)
        {
            while (true)
            {
                switch (Current.Kind)
                {
                    case SyntaxKind.OpenParenthesisToken:
                        var open = NextToken();
                        var arguments = ParseArgumentList();
                        var close = MatchToken(SyntaxKind.CloseParenthesisToken);
                        expression = new InvocationExpressionSyntax(expression, open, arguments, close);
                        break;
                    default:
                        return expression;
                }
            }
        }

        private SeperatedSyntaxList<ExpressionSyntax> ParseArgumentList()
        {
            var builder = ImmutableList.CreateBuilder<SyntaxNode>();
            
            var parseNextParameter = true;
            
            while (parseNextParameter && 
                   Current.Kind != SyntaxKind.CloseBraceToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = ParseBinaryExpression();
                builder.Add(parameter);
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = NextToken();
                    builder.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeperatedSyntaxList<ExpressionSyntax>(builder.ToImmutable());
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                {
                    var open = NextToken();
                    var expression = ParseBinaryExpression();
                    var close = MatchToken(SyntaxKind.CloseParenthesisToken);
                    return new ParenthesizedExpressionSyntax(open, expression, close);
                }
                case SyntaxKind.Identifier:
                    return new SimpleNameExpressionSyntax(NextToken());
                case SyntaxKind.FalseKeyword or SyntaxKind.TrueKeyword:
                    return new BooleanLiteralExpressionSyntax(NextToken());
                case SyntaxKind.IntegerLiteralToken:
                    return new NumericLiteralExpressionSyntax(NextToken());
                default:
                    return new EmptyExpressionSyntax();
            }
        }
    }
}
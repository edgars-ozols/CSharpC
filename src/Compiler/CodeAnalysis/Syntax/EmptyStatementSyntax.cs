using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Syntax
{
    public sealed record EmptyStatementSyntax(SyntaxToken Semicolon) : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.EmptyStatement;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Semicolon;
        }
    }
}
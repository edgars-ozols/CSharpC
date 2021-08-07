using System.Collections.Generic;
using System.Linq;

namespace Compiler.CodeAnalysis.Syntax
{
    public class EmptyExpressionSyntax : ExpressionsSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.EmptyExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Enumerable.Empty<SyntaxNode>();
        }
    }
}
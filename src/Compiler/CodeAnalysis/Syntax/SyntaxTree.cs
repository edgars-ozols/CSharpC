using System.Collections.Generic;

namespace Compiler.CodeAnalysis.Syntax
{
    public class SyntaxTree
    {
        public SyntaxTree(IEnumerable<string> diagnostics, ExpressionsSyntax root, SyntaxToken endOfFileToken)
        {
            Diagnostics = diagnostics;
            Root = root;
            EndOfFileToken = endOfFileToken;
        }

        public IEnumerable<string> Diagnostics { get; }
        public ExpressionsSyntax Root { get; }
        public SyntaxToken EndOfFileToken { get; }

        public static SyntaxTree Parse(string text)
        {
            Parser parser = new(text);
            return parser.Parse();
        }
    }
}
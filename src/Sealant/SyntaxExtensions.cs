using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sealant;

internal static class SyntaxExtensions
{
    public static string GetNewLine(this SyntaxTree tree)
    {
        foreach (var trivia in tree.GetRoot().DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return trivia.ToString();
            }
        }

        return "\n";
    }

    public static bool HasEndOfLineBetween(this SyntaxToken first, SyntaxToken second) =>
        first.TrailingTrivia.ContainsEndOfLine() || second.LeadingTrivia.ContainsEndOfLine();

    public static bool StartsLine(this SyntaxToken token)
    {
        var previous = token.GetPreviousToken();

        return token.LeadingTrivia.ContainsEndOfLine() ||
            previous.IsKind(SyntaxKind.None) ||
            previous.TrailingTrivia.ContainsEndOfLine();
    }

    private static bool ContainsEndOfLine(this SyntaxTriviaList trivia)
    {
        foreach (var item in trivia)
        {
            if (item.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return true;
            }
        }

        return false;
    }
}

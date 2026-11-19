using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sealant.Analyzers;

namespace Sealant;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsealedConcreteClassCodeFixProvider)), Shared]
internal sealed class UnsealedConcreteClassCodeFixProvider : CodeFixProvider
{
    private const string UnsealedComment = "// unsealed";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(UnsealedConcreteClassAnalyzer.Rule.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
        {
            return;
        }

        if (root.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } declaration)
        {
            return;
        }

        var sealAction = CodeAction.Create(
            title: "Seal type",
            createChangedDocument: _ =>
                Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(declaration, AddSealed(declaration)))),
            equivalenceKey: $"{UnsealedConcreteClassAnalyzer.Rule.Id}.Seal");

        var markUnsealedAction = CodeAction.Create(
            title: "Mark as unsealed",
            createChangedDocument: _ =>
                Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(declaration, AddUnsealedComment(declaration)))),
            equivalenceKey: $"{UnsealedConcreteClassAnalyzer.Rule.Id}.Unsealed");

        context.RegisterCodeFix(sealAction, context.Diagnostics);
        context.RegisterCodeFix(markUnsealedAction, context.Diagnostics);
    }

    private static TypeDeclarationSyntax AddSealed(TypeDeclarationSyntax declaration)
    {
        var insertionIndex = GetSealedModifierInsertionIndex(declaration.Modifiers);
        var sealedToken = SyntaxFactory.Token(SyntaxKind.SealedKeyword).WithTrailingTrivia(SyntaxFactory.Space);

        if (insertionIndex == 0)
        {
            var nextToken = declaration.Modifiers.Count > 0 ? declaration.Modifiers[0] : declaration.Keyword;
            declaration = declaration.ReplaceToken(nextToken, nextToken.WithLeadingTrivia(default(SyntaxTriviaList)));
            sealedToken = sealedToken.WithLeadingTrivia(nextToken.LeadingTrivia);
        }

        return declaration.WithModifiers(declaration.Modifiers.Insert(insertionIndex, sealedToken));
    }

    private static int GetSealedModifierInsertionIndex(SyntaxTokenList modifiers)
    {
        for (var i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].IsKind(SyntaxKind.UnsafeKeyword) || modifiers[i].IsKind(SyntaxKind.PartialKeyword))
            {
                return i;
            }
        }

        return modifiers.Count;
    }

    private static TypeDeclarationSyntax AddUnsealedComment(TypeDeclarationSyntax declaration)
    {
        var target = GetUnsealedCommentTarget(declaration);
        var newLeadingTrivia = InsertUnsealedComment(
            target.LeadingTrivia, target.StartsLine(), declaration.SyntaxTree.GetNewLine());

        return declaration.ReplaceToken(target, target.WithLeadingTrivia(newLeadingTrivia));
    }

    private static SyntaxToken GetUnsealedCommentTarget(TypeDeclarationSyntax declaration)
    {
        var insertionIndex = GetSealedModifierInsertionIndex(declaration.Modifiers);
        var target = insertionIndex < declaration.Modifiers.Count
            ? declaration.Modifiers[insertionIndex]
            : declaration.Keyword;

        for (var i = insertionIndex - 1; i >= 0; i--)
        {
            var modifier = declaration.Modifiers[i];

            if (modifier.HasEndOfLineBetween(target))
            {
                break;
            }

            target = modifier;
        }

        return target;
    }

    private static SyntaxTriviaList InsertUnsealedComment(
        SyntaxTriviaList leadingTrivia, bool startsLine, string newLine)
    {
        var hasIndentation = leadingTrivia.Count > 0 && leadingTrivia[leadingTrivia.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia);
        var indentation = hasIndentation ? leadingTrivia[leadingTrivia.Count - 1] : default;

        var trivia = new List<SyntaxTrivia>();
        var prefixEnd = hasIndentation ? leadingTrivia.Count - 1 : leadingTrivia.Count;

        for (var i = 0; i < prefixEnd; i++)
        {
            trivia.Add(leadingTrivia[i]);
        }

        if (!startsLine)
        {
            trivia.Add(SyntaxFactory.EndOfLine(newLine));
        }

        if (hasIndentation)
        {
            trivia.Add(indentation);
        }

        trivia.Add(SyntaxFactory.Comment(UnsealedComment));
        trivia.Add(SyntaxFactory.EndOfLine(newLine));

        if (hasIndentation)
        {
            trivia.Add(indentation);
        }

        return SyntaxFactory.TriviaList(trivia);
    }
}

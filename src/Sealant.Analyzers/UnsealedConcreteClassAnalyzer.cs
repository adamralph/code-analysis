using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sealant.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UnsealedConcreteClassAnalyzer : DiagnosticAnalyzer
{
    private const string OptionsKey = "sealant.SEAL0001.visibilities";

    internal static readonly DiagnosticDescriptor Rule = new(
        id: "SEAL0001",
        title: "Classes should be sealed, static, abstract, or marked unsealed",
        messageFormat: "Type '{0}' should be sealed, static, abstract, or marked unsealed",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Classes should be sealed, static, abstract, or marked unsealed. Sealing prevents unintended inheritance. " +
            "Declare a class abstract when it is intentionally designed for inheritance, " +
            "or static when it has no instance members. " +
            "Add a comment containing only the word unsealed to keep an instantiable class inheritable.",
        helpLinkUri: "https://github.com/adamralph/code-analysis/blob/main/docs/rules/SEAL0001.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var cache = new ConcurrentDictionary<SyntaxTree, Visibilities>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Analyze(symbolContext, cache), SymbolKind.NamedType);
        });
    }

    private static void Analyze(
        SymbolAnalysisContext context, ConcurrentDictionary<SyntaxTree, Visibilities> cache)
    {
        if (context.Symbol is not INamedTypeSymbol
            {
                TypeKind: TypeKind.Class,
                IsImplicitlyDeclared: false,
                IsSealed: false,
                IsStatic: false,
                IsAbstract: false
            } type)
        {
            return;
        }

        // a compiler generated type (e.g. Program class synthesized for top level statements) has no type declaration
        if (!type.HasTypeDeclaration(context.CancellationToken))
        {
            return;
        }

        var location = type.GetFirstNonGeneratedLocation(context.CancellationToken);

        if (location.SourceTree is not { } tree)
        {
            return;
        }

        if ((GetVisibilities(cache, context.Options.AnalyzerConfigOptionsProvider, tree) & type.ToVisibilities()) == 0)
        {
            return;
        }

        if (type.HasUnsealedComment(context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name));
    }

    private static Visibilities GetVisibilities(
        ConcurrentDictionary<SyntaxTree, Visibilities> cache,
        AnalyzerConfigOptionsProvider optionsProvider,
        SyntaxTree tree)
    {
        if (cache.TryGetValue(tree, out var cached))
        {
            return cached;
        }

        var visibilities = optionsProvider.GetOptions(tree).TryGetValue(OptionsKey, out var value)
            ? Visibilities.Parse(value)
            : Visibilities.All;

        _ = cache.TryAdd(tree, visibilities);
        return visibilities;
    }
}

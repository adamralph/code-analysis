using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Fixtures;

public static class CodeFixProviderExtensions
{
    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    public static Task<string> ApplyFirstFixAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source) =>
        provider.ApplyAsync(analyzer, source, fixAll: false, actionIndex: 0);

    public static Task<string> ApplySecondFixAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source) =>
        provider.ApplyAsync(analyzer, source, fixAll: false, actionIndex: 1);

    public static Task<string> ApplyFixAllAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source) =>
        provider.ApplyAsync(analyzer, source, fixAll: true, actionIndex: 0);

    public static Task<string> ApplySecondFixAllAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source) =>
        provider.ApplyAsync(analyzer, source, fixAll: true, actionIndex: 1);

    public static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source)
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.CreateDocument(source);
        var diagnostics = await document.GetAnalyzerDiagnosticsAsync(analyzer).ConfigureAwait(false);
        return await provider.RegisterAsync(document, diagnostics.Single()).ConfigureAwait(false);
    }

    private static async Task<string> ApplyAsync(
        this CodeFixProvider provider, DiagnosticAnalyzer analyzer, string source, bool fixAll, int actionIndex)
    {
        using var workspace = new AdhocWorkspace();

        var document = workspace.CreateDocument(source);
        var diagnostics = await document.GetAnalyzerDiagnosticsAsync(analyzer).ConfigureAwait(false);
        var actions = await provider.RegisterAsync(document, diagnostics[0]).ConfigureAwait(false);
        var selectedAction = actions[actionIndex];

        CodeAction action;

        if (fixAll)
        {
            var context = new FixAllContext(
                document,
                provider,
                FixAllScope.Document,
                selectedAction.EquivalenceKey!,
                provider.FixableDiagnosticIds,
                new StaticDiagnosticProvider(diagnostics),
                Ct.None);

            action = (await provider.GetFixAllProvider()!.GetFixAsync(context).ConfigureAwait(false))!;
        }
        else
        {
            action = selectedAction;
        }

        var operations = await action.GetOperationsAsync(Ct.None).ConfigureAwait(false);
        var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id)!;
        var text = await changedDocument.GetTextAsync().ConfigureAwait(false);
        return text.ToString();
    }

    private static Document CreateDocument(this AdhocWorkspace workspace, string source) =>
        workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    name: "Test",
                    assemblyName: "Test",
                    LanguageNames.CSharp)
                .WithCompilationOptions(
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        nullableContextOptions: NullableContextOptions.Enable,
                        allowUnsafe: true))
                .WithMetadataReferences(References))
            .AddDocument("Test.cs", SourceText.From(source), filePath: "Test.cs");

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        this Document document, DiagnosticAnalyzer analyzer)
    {
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        return await compilation!.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<CodeAction>> RegisterAsync(
        this CodeFixProvider provider, Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (codeAction, _) => actions.Add(codeAction), Ct.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return [.. actions];
    }

    private sealed class StaticDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, Ct ct) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, Ct ct) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, Ct ct) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fixtures;

public static class DiagnosticAnalyzerExtensions
{
    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    public static Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        this DiagnosticAnalyzer analyzer,
        (string Text, string Path)[] files,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        (string Path, string Key, string Value)[]? editorConfig = null) =>
        CSharpCompilation.Create(
                "Test",
                files.Select(file =>
                    CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(LanguageVersion.Latest), file.Path)),
                References,
                new CSharpCompilationOptions(
                    outputKind, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable))
            .WithAnalyzers(
                [analyzer], new AnalyzerOptions([], new TestAnalyzerConfigOptionsProvider(editorConfig ?? [])))
            .GetAnalyzerDiagnosticsAsync();

    private sealed class TestAnalyzerConfigOptionsProvider(
        (string Path, string Key, string Value)[] editorConfig) : AnalyzerConfigOptionsProvider
    {
        private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions([]);

        private readonly ImmutableDictionary<string, AnalyzerConfigOptions> _options = editorConfig
            .GroupBy(item => item.Path)
            .ToImmutableDictionary(
                group => group.Key,
                AnalyzerConfigOptions (group) => new TestAnalyzerConfigOptions(
                    group.ToImmutableDictionary(
                        item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase)));

        public override AnalyzerConfigOptions GlobalOptions => Empty;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            _options.GetValueOrDefault(tree.FilePath, Empty);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;

        private sealed class TestAnalyzerConfigOptions(ImmutableDictionary<string, string> values)
            : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
        }
    }
}

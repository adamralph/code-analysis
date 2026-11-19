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
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary) =>
        CSharpCompilation.Create(
                "Test",
                files.Select(file =>
                    CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(LanguageVersion.Latest), file.Path)),
                References,
                new CSharpCompilationOptions(
                    outputKind, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable))
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
}

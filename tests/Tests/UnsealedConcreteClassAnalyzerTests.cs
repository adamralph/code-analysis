using System.Collections.Immutable;
using System.Globalization;
using Fixtures;
using Microsoft.CodeAnalysis;
using Sealant.Analyzers;
using Xunit;

namespace Tests;

public static class UnsealedConcreteClassAnalyzerTests
{
    [Theory]
    [InlineData("public class Foo { }")]
    [InlineData("internal class Foo { }")]
    [InlineData("public record Foo;")]
    [InlineData("public record class Foo;")]
    [InlineData("public partial class Foo { } public partial class Foo { }")]
    [InlineData("public class Foo<T> { }")]
    [InlineData("file class Foo { }")]
    [InlineData("sealed class Outer { class Foo { } }")]
    public static async Task UnsealedClassIsReported(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("SEAL0001", diagnostic.Id);
    }

    [Theory]
    [InlineData("public sealed class Foo { }")]
    [InlineData("public sealed record Foo;")]
    [InlineData("public static class Foo { }")]
    [InlineData("public abstract class Foo { }")]
    [InlineData("public abstract record Foo;")]
    [InlineData("public partial class Foo { } public sealed partial class Foo { }")]
    [InlineData("public partial class Foo { } public abstract partial class Foo { }")]
    [InlineData("public struct Foo { }")]
    [InlineData("public record struct Foo;")]
    [InlineData("public interface IFoo { }")]
    [InlineData("public enum Foo { }")]
    [InlineData("public delegate void Foo();")]
    [InlineData("sealed class Outer { sealed class Foo { } }")]
    public static async Task TypeThatDoesNotNeedSealingIsNotReported(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    [Theory]
    // comment text
    [InlineData("// unsealed\npublic class Foo { }")]
    [InlineData("//unsealed\npublic class Foo { }")]
    [InlineData("// UNSEALED\npublic class Foo { }")]
    [InlineData("//   UnSeAlEd   \npublic class Foo { }")]
    // basic placements
    [InlineData("""
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        // unsealed
        public class Foo<T> { }
        """)]
    [InlineData("""
        // unsealed
        public partial class Foo { } public partial class Foo { }
        """)]
    [InlineData("""
        public partial class Foo { }
        // unsealed
        public partial class Foo { }
        """)]
    // between modifiers
    [InlineData("""
        public // unsealed
        partial class Foo { }
        """)]
    [InlineData("""
        public
        // unsealed
        partial class Foo { }
        """)]
    [InlineData("""
        public
        // unsealed
        partial
        class Foo { }
        """)]
    // unsafe
    [InlineData("""
        unsafe // unsealed
        class Foo { }
        """)]
    [InlineData("""
        unsafe
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public unsafe // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public unsafe
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public
        // unsealed
        unsafe class Foo { }
        """)]
    // file
    [InlineData("""
        file
        // unsealed
        class Foo { }
        """)]
    // attributes
    [InlineData("""
        [System.Obsolete]
        // unsealed
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete] // unsealed
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete]
        [System.Serializable]
        // unsealed
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete]
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete] // unsealed
        class Foo { }
        """)]
    // doc comments and pragmas
    [InlineData("""
        /// <summary>Foo</summary>
        // unsealed
        public class Foo { }
        """)]
    [InlineData("""
        /// <summary>Foo</summary>
        public // unsealed
        class Foo { }
        """)]
    [InlineData("""
        /// <summary>Foo</summary>
        public
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        #pragma warning disable CA1032
        // unsealed
        public class Foo { }
        """)]
    [InlineData("""
        /// <summary>Foo</summary>
        #pragma warning disable CA1032
        // unsealed
        public class Foo { }
        """)]
    // whitespace, indentation, and line endings
    [InlineData("    // unsealed\n    public class Foo { }")]
    [InlineData("// unsealed\n\npublic class Foo { }")]
    [InlineData("public\n\n// unsealed\nclass Foo { }")]
    [InlineData("public\n// unsealed\n\nclass Foo { }")]
    [InlineData("//\tunsealed\npublic class Foo { }")]
    [InlineData("// unsealed\t\npublic class Foo { }")]
    [InlineData("// unsealed\r\npublic class Foo { }")]
    // nested and namespaced declarations
    [InlineData("sealed class Outer\n{\n    // unsealed\n    public class Foo { }\n}")]
    [InlineData("""
        sealed class Outer { public new // unsealed
        class Foo { } }
        """)]
    [InlineData("""
        namespace N {
        // unsealed
        public class Foo { } }
        """)]
    [InlineData("""
        namespace N;
        // unsealed
        public class Foo { }
        """)]
    // records
    [InlineData("""
        // unsealed
        public record Foo;
        """)]
    [InlineData("""
        // unsealed
        public record class Foo;
        """)]
    [InlineData("""
        // unsealed
        public record Foo(int Value);
        """)]
    [InlineData("""
        public // unsealed
        record Foo;
        """)]
    [InlineData("""
        public
        // unsealed
        record class Foo;
        """)]
    [InlineData("""
        public // unsealed
        record class Foo;
        """)]
    public static async Task ClassWithUnsealedCommentIsNotReported(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("""
        // sealed
        public class Foo { }
        """)]
    [InlineData("""
        // unsealed class
        public class Foo { }
        """)]
    [InlineData("""
        // unsealed extra
        public class Foo { }
        """)]
    [InlineData("""
        /* unsealed */
        public class Foo { }
        """)]
    [InlineData("""
        /// unsealed
        public class Foo { }
        """)]
    public static async Task ClassWithoutExactUnsealedCommentIsReported(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        _ = Assert.Single(diagnostics);
    }

    [Theory]
    [InlineData("""
        public class // unsealed
        Foo { }
        """)]
    [InlineData("""
        public class
        // unsealed
        Foo { }
        """)]
    [InlineData("""
        public class Foo
        // unsealed
        { }
        """)]
    [InlineData("""
        public class Foo : System.Exception
        // unsealed
        { }
        """)]
    [InlineData("""
        public record // unsealed
        class Foo;
        """)]
    [InlineData("""
        public class Foo { }
        // unsealed
        """)]
    [InlineData("""
        // unsealed
        namespace N { public class Foo { } }
        """)]
    [InlineData("""
        // unsealed
        using System;
        public class Foo { }
        """)]
    [InlineData("""
        // unsealed
        [System.Obsolete]
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete]
        // unsealed
        [System.Serializable]
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete] // unsealed
        [System.Serializable]
        public class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete( // unsealed
        "x")]
        public class Foo { }
        """)]
    [InlineData("""
        // unsealed
        [System.Obsolete]
        class Foo { }
        """)]
    [InlineData("""
        public partial // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public partial
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public
        partial
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        partial
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public unsafe partial // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public unsafe partial
        // unsealed
        class Foo { }
        """)]
    public static async Task CommentOutsideTheAcceptedPlacementIsReported(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task UnsealedCommentOnOneTypeDoesNotSuppressTheNextType()
    {
        const string source = "// unsealed\npublic class Foo { } public class Bar { }";

        var diagnostics = await AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(source.IndexOf("Bar", StringComparison.Ordinal), diagnostic.Location.SourceSpan.Start);
    }

    [Fact]
    public static async Task UnsealedCommentOnOuterTypeDoesNotSuppressNestedType()
    {
        var diagnostics = await AnalyzeAsync("// unsealed\nsealed class Outer { class Foo { } }");

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task UnsealedCommentInOnePartialFileSuppressesTheDiagnostic()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("// unsealed\npublic partial class Foo { }", "A.cs"), ("public partial class Foo { }", "B.cs")]);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("public class Foo { }")]
    [InlineData("public record Foo;")]
    [InlineData("public record Foo(int Value);")]
    [InlineData("public class Foo<T> { }")]
    [InlineData("namespace N; class Foo { }")]
    [InlineData("public partial class Foo { } public partial class Foo { }")]
    [InlineData("sealed class Outer { class Foo { } }")]
    public static async Task DiagnosticIsReportedAtTypeName(string source)
    {
        var diagnostics = await AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(source.IndexOf("Foo", StringComparison.Ordinal), diagnostic.Location.SourceSpan.Start);
        Assert.Equal("Foo".Length, diagnostic.Location.SourceSpan.Length);
    }

    [Fact]
    public static async Task DiagnosticMessageNamesTheUnsealedType()
    {
        var diagnostics = await AnalyzeAsync("public class Bar { }");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Type 'Bar' should be sealed, static, abstract, or marked unsealed", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public static async Task TopLevelStatementsAreNotReported()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("var foo = 1;", "Program.cs")], OutputKind.ConsoleApplication);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task GeneratedFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "Test.g.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task GeneratedSuffixFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "Test.generated.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task DesignerSuffixFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "Test.designer.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task GeneratedInterfaceSuffixFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "Test.g.i.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task TemporaryGeneratedFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "TemporaryGeneratedFile_abc.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task LowerCaseTemporaryGeneratedFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "temporarygeneratedfile_abc.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task WindowsPathWithTemporaryGeneratedFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "C:\\path\\TemporaryGeneratedFile_abc.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task DriveRelativePathWithTemporaryGeneratedFileIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("public class Foo { }", "C:TemporaryGeneratedFile_abc.cs");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task AutoGeneratedHeaderIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("""
            // <auto-generated>
            public class Foo { }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task AutoGeneratedHeaderWithoutHyphenIsNotReported()
    {
        var diagnostics = await AnalyzeAsync("""
            // <autogenerated>
            public class Foo { }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task UpperCaseAutoGeneratedHeaderIsReported()
    {
        var diagnostics = await AnalyzeAsync("""
            // <AUTO-GENERATED>
            public class Foo { }
            """);

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task AutoGeneratedMarkerInDocumentationCommentIsReported()
    {
        var diagnostics = await AnalyzeAsync("""
            /// <auto-generated>
            public class Foo { }
            """);

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task GeneratedCodeAttributeIsNotReported()
    {
        var diagnostics = await AnalyzeAsync(
            "[System.CodeDom.Compiler.GeneratedCode(\"test\", \"1.0\")] public class Foo { }");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task GeneratedCodeAttributeOnOnePartialPartIsReportedAtTheHandWrittenFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [
                (
                    """
                    [System.CodeDom.Compiler.GeneratedCode("test", "1.0")]
                    public partial class Foo { }
                    """,
                    "Generated.cs"),
                ("public partial class Foo { }", "Test.cs"),
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Test.cs", diagnostic.Location.SourceTree?.FilePath);
    }

    [Fact]
    public static async Task PartialTypeSplitAcrossGeneratedAndHandWrittenFilesIsReportedAtTheHandWrittenFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("public partial class Foo { }", "Test.g.cs"), ("public partial class Foo { }", "Test.cs")]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Test.cs", diagnostic.Location.SourceTree?.FilePath);
    }

    [Fact]
    public static async Task PartialTypeSplitWithHandWrittenFileFirstIsReportedAtTheHandWrittenFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("public partial class Foo { }", "Test.cs"), ("public partial class Foo { }", "Test.g.cs")]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Test.cs", diagnostic.Location.SourceTree?.FilePath);
    }

    [Fact]
    public static async Task PartialTypeSplitAcrossAutoGeneratedHeaderAndHandWrittenFilesIsReportedAtTheHandWrittenFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [
                (
                    """
                    // <auto-generated>
                    public partial class Foo { }
                    """,
                    "Generated.cs"),
                ("public partial class Foo { }", "Test.cs"),
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Test.cs", diagnostic.Location.SourceTree?.FilePath);
    }

    [Theory]
    [InlineData("public", "public class Foo { }")]
    [InlineData("internal", "internal class Foo { }")]
    [InlineData("private", "sealed class Outer { class Foo { } }")]
    [InlineData("protected", "sealed class Outer { protected class Foo { } }")]
    [InlineData("protected internal", "sealed class Outer { protected internal class Foo { } }")]
    [InlineData("private protected", "sealed class Outer { private protected class Foo { } }")]
    [InlineData("public", "internal class Outer { public class Foo { } }")]
    [InlineData("public, internal", "internal class Foo { }")]
    [InlineData("Public, INTERNAL", "public class Foo { }")]
    [InlineData(" public ,  internal ", "internal class Foo { }")]
    [InlineData("public, unknown", "internal class Foo { }")]
    [InlineData("file", "file class Foo { }")]
    [InlineData("all", "internal class Foo { }")]
    [InlineData("all", "file class Foo { }")]
    [InlineData("public, all", "internal class Foo { }")]
    public static async Task SelectedAccessibilityIsReported(string visibilities, string source)
    {
        var diagnostics = await AnalyzeAsync(source, visibilities: visibilities);

        _ = Assert.Single(diagnostics);
    }

    [Theory]
    [InlineData("public", "internal class Foo { }")]
    [InlineData("public", "class Foo { }")]
    [InlineData("public", "sealed class Outer { class Foo { } }")]
    [InlineData("private", "public class Foo { }")]
    [InlineData("internal", "public class Foo { }")]
    [InlineData("private protected", "sealed class Outer { protected class Foo { } }")]
    [InlineData("internal", "file class Foo { }")]
    [InlineData("public", "file class Foo { }")]
    public static async Task UnselectedAccessibilityIsNotReported(string visibilities, string source)
    {
        var diagnostics = await AnalyzeAsync(source, visibilities: visibilities);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task EmptyVisibilitiesOptionAppliesToEveryAccessibility()
    {
        var diagnostics = await AnalyzeAsync("internal class Foo { }", visibilities: "");

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task UnrecognizedVisibilitiesOptionAppliesToEveryAccessibility()
    {
        var diagnostics = await AnalyzeAsync("internal class Foo { }", visibilities: "unknown");

        _ = Assert.Single(diagnostics);
    }

    [Fact]
    public static async Task VisibilitiesOptionIsReadPerFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("public class Foo { }", "A.cs"), ("public class Bar { }", "B.cs")],
            editorConfig:
            [
                ("A.cs", "sealant.SEAL0001.visibilities", "public"),
                ("B.cs", "sealant.SEAL0001.visibilities", "private"),
            ]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("A.cs", diagnostic.Location.SourceTree?.FilePath);
    }

    [Fact]
    public static async Task VisibilitiesOptionForPartialTypeComesFromTheReportedFile()
    {
        var diagnostics = await new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [("public partial class Foo { }", "A.cs"), ("public partial class Foo { }", "B.cs")],
            editorConfig:
            [
                ("A.cs", "sealant.SEAL0001.visibilities", "private"),
                ("B.cs", "sealant.SEAL0001.visibilities", "public"),
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static async Task UnsealedCommentStillSuppressesWhenAccessibilityIsSelected()
    {
        var diagnostics = await AnalyzeAsync("// unsealed\npublic class Foo { }", visibilities: "public");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public static void SupportedDiagnosticsContainsRule()
    {
        var descriptors = new UnsealedConcreteClassAnalyzer().SupportedDiagnostics;

        var descriptor = Assert.Single(descriptors);
        Assert.Equal("SEAL0001", descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source, string filePath = "Test.cs", string? visibilities = null) =>
        new UnsealedConcreteClassAnalyzer().GetDiagnosticsAsync(
            [(Text: source, Path: filePath)],
            editorConfig: visibilities is null
                ? null
                : [(Path: filePath, Key: "sealant.SEAL0001.visibilities", Value: visibilities)]);
}

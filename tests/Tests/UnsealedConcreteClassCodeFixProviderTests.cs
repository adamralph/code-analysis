using Fixtures;
using Sealant;
using Sealant.Analyzers;
using Xunit;

namespace Tests;

public static class UnsealedConcreteClassCodeFixProviderTests
{
    [Fact]
    public static async Task RegistersTwoCodeActions()
    {
        var actions = await new UnsealedConcreteClassCodeFixProvider()
            .GetCodeActionsAsync(new UnsealedConcreteClassAnalyzer(), "public class Foo { }");

        Assert.Equal(2, actions.Length);
        Assert.Equal("Seal type", actions[0].Title);
        Assert.Equal("SEAL0001.Seal", actions[0].EquivalenceKey);
        Assert.Equal("Mark as unsealed", actions[1].Title);
        Assert.Equal("SEAL0001.Unsealed", actions[1].EquivalenceKey);
    }

    [Theory]
    [InlineData("class Foo { }", "sealed class Foo { }")]
    [InlineData("internal class Foo { }", "internal sealed class Foo { }")]
    [InlineData("public class Foo { }", "public sealed class Foo { }")]
    [InlineData("public record Foo;", "public sealed record Foo;")]
    [InlineData("public record class Foo;", "public sealed record class Foo;")]
    [InlineData("public class Foo<T> { }", "public sealed class Foo<T> { }")]
    [InlineData("unsafe class Foo { }", "sealed unsafe class Foo { }")]
    [InlineData("    unsafe class Foo { }", "    sealed unsafe class Foo { }")]
    [InlineData("file class Foo { }", "file sealed class Foo { }")]
    [InlineData("[System.Obsolete]\nclass Foo { }", "[System.Obsolete]\nsealed class Foo { }")]
    [InlineData("[System.Obsolete]\n    class Foo { }", "[System.Obsolete]\n    sealed class Foo { }")]
    [InlineData(
        "public partial class Foo { } public partial class Foo { }",
        "public sealed partial class Foo { } public partial class Foo { }")]
    [InlineData(
        "sealed class Outer { public new class Inner { } }",
        "sealed class Outer { public new sealed class Inner { } }")]
    public static async Task AddsTheModifier(string source, string expected)
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider().ApplyFirstFixAsync(new UnsealedConcreteClassAnalyzer(), source);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static async Task FixAllSealsEveryReportedType()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(new UnsealedConcreteClassAnalyzer(), "public class Foo { } public class Bar { }");

        Assert.Equal("public sealed class Foo { } public sealed class Bar { }", actual);
    }

    [Theory]
    [InlineData("class Foo { }", "// unsealed\nclass Foo { }")]
    [InlineData("public class Foo { }", "// unsealed\npublic class Foo { }")]
    [InlineData("internal class Foo { }", "// unsealed\ninternal class Foo { }")]
    [InlineData("public record Foo;", "// unsealed\npublic record Foo;")]
    [InlineData("public record class Foo;", "// unsealed\npublic record class Foo;")]
    [InlineData("public class Foo<T> { }", "// unsealed\npublic class Foo<T> { }")]
    [InlineData("    public class Foo { }", "    // unsealed\n    public class Foo { }")]
    [InlineData("""
        [System.Obsolete]
        class Foo { }
        """, """
        [System.Obsolete]
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        [System.Obsolete]
        public class Foo { }
        """, """
        [System.Obsolete]
        // unsealed
        public class Foo { }
        """)]
    [InlineData("[System.Obsolete]\n    public class Foo { }", "[System.Obsolete]\n    // unsealed\n    public class Foo { }")]
    [InlineData("/// <summary>Foo</summary>\npublic class Foo { }", "/// <summary>Foo</summary>\n// unsealed\npublic class Foo { }")]
    [InlineData("    /// <summary>Foo</summary>\n    public class Foo { }", "    /// <summary>Foo</summary>\n    // unsealed\n    public class Foo { }")]
    [InlineData("""
        public
        class Foo { }
        """, """
        public
        // unsealed
        class Foo { }
        """)]
    [InlineData("""
        public
        partial class Foo { }
        """, """
        public
        // unsealed
        partial class Foo { }
        """)]
    [InlineData("""
        public partial
        class Foo { }
        """, """
        // unsealed
        public partial
        class Foo { }
        """)]
    [InlineData("""
        public
        unsafe class Foo { }
        """, """
        public
        // unsealed
        unsafe class Foo { }
        """)]
    [InlineData("""
        public unsafe
        class Foo { }
        """, """
        // unsealed
        public unsafe
        class Foo { }
        """)]
    [InlineData("""
        file
        class Foo { }
        """, """
        file
        // unsealed
        class Foo { }
        """)]
    [InlineData(
        """
        /// <summary>
        /// The command exited with an unexpected exit code.
        /// </summary>
        #pragma warning disable CA1032 // Implement standard exception constructors
        public class ExitCodeException : System.Exception
        #pragma warning restore CA1032
        {
        }
        """,
        """
        /// <summary>
        /// The command exited with an unexpected exit code.
        /// </summary>
        #pragma warning disable CA1032 // Implement standard exception constructors
        // unsealed
        public class ExitCodeException : System.Exception
        #pragma warning restore CA1032
        {
        }
        """)]
    [InlineData(
        "public partial class Foo { } public partial class Foo { }",
        "// unsealed\npublic partial class Foo { } public partial class Foo { }")]
    [InlineData(
        "sealed class Outer { public class Inner { } }",
        "sealed class Outer { \n// unsealed\npublic class Inner { } }")]
    public static async Task AddsTheUnsealedComment(string source, string expected)
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider().ApplySecondFixAsync(new UnsealedConcreteClassAnalyzer(), source);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static async Task SecondFixAllAddsTheUnsealedCommentToEveryReportedType()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplySecondFixAllAsync(new UnsealedConcreteClassAnalyzer(), """
                public class Foo { }
                public class Bar { }
                """);

        Assert.Equal("""
            // unsealed
            public class Foo { }
            // unsealed
            public class Bar { }
            """, actual);
    }
}

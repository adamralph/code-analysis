using Fixtures;
using Sealant;
using Sealant.Analyzers;
using Xunit;

namespace Tests;

public static class UnsealedConcreteClassCodeFixProviderTests
{
    [Fact]
    public static async Task RegistersSealAndUnsealedActionsWhenTypeHasNoDerivedTypes()
    {
        var actions = await new UnsealedConcreteClassCodeFixProvider()
            .GetCodeActionsAsync(new UnsealedConcreteClassAnalyzer(), "public class Foo { }");

        Assert.Equal(2, actions.Length);
        Assert.Equal("Seal type", actions[0].Title);
        Assert.Equal("SEAL0001.Seal", actions[0].EquivalenceKey);
        Assert.Equal("Mark as unsealed", actions[1].Title);
        Assert.Equal("SEAL0001.Unsealed", actions[1].EquivalenceKey);
    }

    [Fact]
    public static async Task RegistersOnlyDerivedTypesUnsealedActionWhenTypeHasDerivedTypes()
    {
        var actions = await new UnsealedConcreteClassCodeFixProvider()
            .GetCodeActionsAsync(
                new UnsealedConcreteClassAnalyzer(),
                "public class Base { } public sealed class Derived : Base { }");

        var action = Assert.Single(actions);
        Assert.Equal("Mark as unsealed (has derived types)", action.Title);
        Assert.Equal("SEAL0001.Unsealed.DerivedTypes", action.EquivalenceKey);
    }

    [Fact]
    public static async Task RegistersOnlyDerivedTypesUnsealedActionWhenGenericTypeHasDerivedTypes()
    {
        var actions = await new UnsealedConcreteClassCodeFixProvider()
            .GetCodeActionsAsync(
                new UnsealedConcreteClassAnalyzer(),
                "public class Base<T> { } public sealed class Derived : Base<int> { }");

        var action = Assert.Single(actions);
        Assert.Equal("Mark as unsealed (has derived types)", action.Title);
        Assert.Equal("SEAL0001.Unsealed.DerivedTypes", action.EquivalenceKey);
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
    public static async Task SealsTypeWithoutDerivedTypes(string source, string expected)
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAsync(new UnsealedConcreteClassAnalyzer(), source, "SEAL0001.Seal");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static async Task FixAllSealsEveryTypeWithoutDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                "public class Foo { } public class Bar { }",
                "SEAL0001.Seal");

        Assert.Equal("public sealed class Foo { } public sealed class Bar { }", actual);
    }

    [Fact]
    public static async Task FixAllDoesNotSealTypeWithDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                "public class Foo { } public class Base { } public class Derived : Base { }",
                "SEAL0001.Seal");

        Assert.Equal(
            "public sealed class Foo { } public class Base { } public sealed class Derived : Base { }",
            actual);
    }

    [Theory]
    [InlineData("class Foo { }", "// unsealed\nclass Foo { }")]
    [InlineData("public class Foo { }", "// unsealed\npublic class Foo { }")]
    [InlineData("internal class Foo { }", "// unsealed\ninternal class Foo { }")]
    [InlineData("public record Foo;", "// unsealed\npublic record Foo;")]
    [InlineData("public record class Foo;", "// unsealed\npublic record class Foo;")]
    [InlineData("public class Foo<T> { }", "// unsealed\npublic class Foo<T> { }")]
    [InlineData("    public class Foo { }", "    // unsealed\n    public class Foo { }")]
    [InlineData("[System.Obsolete]\nclass Foo { }", "[System.Obsolete]\n// unsealed\nclass Foo { }")]
    [InlineData("[System.Obsolete]\npublic class Foo { }", "[System.Obsolete]\n// unsealed\npublic class Foo { }")]
    [InlineData(
        "[System.Obsolete]\n    public class Foo { }",
        "[System.Obsolete]\n    // unsealed\n    public class Foo { }")]
    [InlineData(
        "/// <summary>Foo</summary>\npublic class Foo { }",
        "/// <summary>Foo</summary>\n// unsealed\npublic class Foo { }")]
    [InlineData(
        "    /// <summary>Foo</summary>\n    public class Foo { }",
        "    /// <summary>Foo</summary>\n    // unsealed\n    public class Foo { }")]
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
    public static async Task AddsUnsealedCommentToTypeWithoutDerivedTypes(string source, string expected)
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAsync(new UnsealedConcreteClassAnalyzer(), source, "SEAL0001.Unsealed");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static async Task FixAllUnsealedAddsCommentToEveryTypeWithoutDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                """
                public class Foo { }
                public class Bar { }
                """,
                "SEAL0001.Unsealed");

        Assert.Equal("""
            // unsealed
            public class Foo { }
            // unsealed
            public class Bar { }
            """, actual);
    }

    [Fact]
    public static async Task FixAllUnsealedDoesNotAddCommentToTypesWithDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                """
                public class Base { }
                public class Derived : Base { }
                public class Standalone { }
                """,
                "SEAL0001.Unsealed");

        Assert.Equal("""
            public class Base { }
            // unsealed
            public class Derived : Base { }
            // unsealed
            public class Standalone { }
            """, actual);
    }

    [Fact]
    public static async Task AddsUnsealedCommentToTypeWithDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAsync(
                new UnsealedConcreteClassAnalyzer(),
                "public class Base { } public class Derived : Base { }",
                "SEAL0001.Unsealed.DerivedTypes");

        Assert.Equal("// unsealed\npublic class Base { } public class Derived : Base { }", actual);
    }

    [Fact]
    public static async Task FixAllDerivedTypesUnsealedAddsCommentToEveryTypeWithDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                """
                public class Base1 { }
                public sealed class Derived1 : Base1 { }
                public class Base2 { }
                public sealed class Derived2 : Base2 { }
                """,
                "SEAL0001.Unsealed.DerivedTypes");

        Assert.Equal("""
            // unsealed
            public class Base1 { }
            public sealed class Derived1 : Base1 { }
            // unsealed
            public class Base2 { }
            public sealed class Derived2 : Base2 { }
            """, actual);
    }

    [Fact]
    public static async Task FixAllDerivedTypesUnsealedDoesNotAddCommentToTypesWithoutDerivedTypes()
    {
        var actual = await new UnsealedConcreteClassCodeFixProvider()
            .ApplyFixAllAsync(
                new UnsealedConcreteClassAnalyzer(),
                """
                public class Base { }
                public class Derived : Base { }
                public class Standalone { }
                """,
                "SEAL0001.Unsealed.DerivedTypes");

        Assert.Equal("""
            // unsealed
            public class Base { }
            public class Derived : Base { }
            public class Standalone { }
            """, actual);
    }
}

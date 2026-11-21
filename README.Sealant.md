# Sealant

![Sealant](https://raw.githubusercontent.com/adamralph/code-analysis/main/assets/sealant/icon.svg)

_[![NuGet version](https://img.shields.io/nuget/v/Sealant.svg?style=flat)](https://www.nuget.org/packages/Sealant)_

_[![CI](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml?query=branch%3Amain)_
_[![InferSharp](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml?query=branch%3Amain)_
_[![Lint](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml?query=branch%3Amain)_
_[![Spell check](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml?query=branch%3Amain)_

Sealant is a [.NET source code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview) package, developed by humans, that reports a compiler warning whenever a class is not `sealed`, `static`, `abstract`, or marked `unsealed`, and includes code fixes that add `sealed` or an `unsealed` comment.

Platform support: any project built with [.NET SDK 7.0 or later](https://dot.net), for any target framework.

- [Quick start](#quick-start)
- [Why](#why)
- [Rules](#rules)

## Quick start

- Install [Sealant](https://www.nuget.org/packages/Sealant).

If your IDE supports .NET source code analysis, it shows a warning for each class that is not `sealed`, `static`, `abstract`, or [marked `unsealed`](https://github.com/adamralph/code-analysis/blob/main/docs/rules/SEAL0001.md). Build your project to see the same warnings from the command line. The package only provides .NET source code analysis and code fixes, and adds no runtime dependency to your project.

## Why

Inheritance is sometimes the right approach, but far less often than its prevalence suggests. Alan Kay, who coined the term object-oriented programming, put it this way:

> Unfortunately, inheritance—though an incredibly powerful technique—has turned out to be very difficult for novices (and even professionals) to deal with.
>
> — Alan Kay, [_The Early History of Smalltalk_](https://worrydream.com/EarlyHistoryOfSmalltalk/) (1993)

Inheritance is powerful, but it is also a commitment. Once someone derives from a type, changing that type becomes harder, and closing the type later is a breaking change.

Jamie Zawinski's old joke about regular expressions applies with one substitution:

> Some people, when confronted with a problem, think "I know, I'll use inheritance." Now they have two problems, and a base class they can never change.
>
> — after [Jamie Zawinski](https://en.wikiquote.org/wiki/Jamie_Zawinski)

Tutorials do not help: the standard example is still an Animal, a Dog, and a Cat—a hierarchy that exists nowhere else.

The default should make closing a type the easy path, not the afterthought.

Some languages already work this way. Kotlin classes are [`final` by default](https://kotlinlang.org/docs/inheritance.html), and `open` makes them inheritable. Swift requires [`open`](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/accesscontrol/) to subclass a class outside its defining module. C# takes the opposite default: its classes are inheritable by default. Unless the author remembers to choose `sealed`, `static`, or `abstract`, a class invites subclasses it was never designed for.

Jeffrey Richter, in _CLR via C#_, argues that the default is the wrong way round:

> When defining a new type, compilers should make the class sealed by default so that the class cannot be used as a base class. ... Obviously, it is too late now, but I think that today's compilers have chosen the wrong default and it would be nice if it could change with future compilers.
>
> — Jeffrey Richter, [_CLR via C#_](https://www.microsoftpressstore.com/store/clr-via-c-sharp-9780735667457)

Sealant removes inheritance by default, making it deliberate, not accidental.

## Rules

- [SEAL0001: Classes should be sealed, static, abstract, or marked unsealed](https://github.com/adamralph/code-analysis/blob/main/docs/rules/SEAL0001.md)

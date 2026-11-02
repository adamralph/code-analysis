# CodeAnalysis

![CodeAnalysis](https://raw.githubusercontent.com/adamralph/code-analysis/main/assets/icon.svg)

_[![NuGet version](https://img.shields.io/nuget/v/Sealant.svg?style=flat)](https://www.nuget.org/packages/Sealant)_

_[![CI](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml?query=branch%3Amain)_
_[![InferSharp](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml?query=branch%3Amain)_
_[![Lint](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml?query=branch%3Amain)_
_[![Spell check](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml?query=branch%3Amain)_

A collection of [.NET source code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview) packages, developed by humans, each containing one or more rules that report compiler warnings about code that can be improved.

Platform support: any project built with [.NET SDK 6.0 or later](https://dot.net), for any target framework.

## Packages

| Name | Description |
| --- | --- |
| [Sealant](README.Sealant.md) | Flags classes that are not sealed, static, abstract, or marked unsealed, with code fixes that add `sealed` or an `unsealed` comment |

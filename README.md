# CodeAnalysis

_[![CI](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/ci.yml?query=branch%3Amain)_
_[![InferSharp](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/infer-sharp.yml?query=branch%3Amain)_
_[![Lint](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/lint.yml?query=branch%3Amain)_
_[![Spell check](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml/badge.svg?branch=main)](https://github.com/adamralph/code-analysis/actions/workflows/spell-check.yml?query=branch%3Amain)_

A collection of [.NET source code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview) packages, developed by humans, each containing one or more rules that report compiler warnings about code that can be improved.

<table>
  <tr>
    <td align="center" width="96">
      <a href="README.Sealant.md">
        <img src="assets/sealant/icon.svg" width="64" height="64" alt="Sealant icon">
      </a>
    </td>
    <td>
      <h2><a href="README.Sealant.md">Sealant</a></h2>
      <p>
        <a href="https://www.nuget.org/packages/Sealant">
          <img src="https://img.shields.io/nuget/v/Sealant.svg?style=flat" alt="NuGet version">
        </a>
      </p>
      <p>Classes should be <code>sealed</code>, <code>static</code>, <code>abstract</code>, or marked <code>unsealed</code>, with code fixes that add <code>sealed</code> or an <code>unsealed</code> comment.</p>
    </td>
  </tr>
</table>

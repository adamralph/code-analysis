namespace Sealant.Analyzers;

[Flags]
internal enum Visibilities
{
    None = 0,
    Public = 1 << 0,
    Protected = 1 << 1,
    Internal = 1 << 2,
    Private = 1 << 3,
    ProtectedOrInternal = 1 << 4,
    ProtectedAndInternal = 1 << 5,
    File = 1 << 6,
    All = Public | Protected | Internal | Private | ProtectedOrInternal | ProtectedAndInternal | File,
}

internal static class VisibilitiesExtensions
{
    private static readonly Dictionary<string, Visibilities> VisibilitiesByEntry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["public"] = Visibilities.Public,
            ["protected"] = Visibilities.Protected,
            ["internal"] = Visibilities.Internal,
            ["private"] = Visibilities.Private,
            ["protected internal"] = Visibilities.ProtectedOrInternal,
            ["private protected"] = Visibilities.ProtectedAndInternal,
            ["file"] = Visibilities.File,
            ["all"] = Visibilities.All,
        };

    extension(Visibilities)
    {
        internal static Visibilities Parse(string optionCsv)
        {
            if (string.IsNullOrWhiteSpace(optionCsv))
            {
                return Visibilities.All;
            }

            var visibilities = Visibilities.None;

            foreach (var value in optionCsv.Split(','))
            {
                if (!VisibilitiesByEntry.TryGetValue(value.Trim(), out var visibility))
                {
                    return Visibilities.All;
                }

                visibilities |= visibility;
            }

            return visibilities;
        }
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;


public static class SyntaxHelper {
    public static string GetMethodName(this MethodDeclarationSyntax method) => method.Identifier.ValueText;
    public static string GetConstructorType(this ConstructorDeclarationSyntax cstr) => cstr.Identifier.ValueText;
    public static string GetIdentifierName(this IdentifierNameSyntax var) => var.Identifier.ValueText; 




    public static IEnumerable<T> Cons<T>(this T x, IEnumerable<T> xs) {
        yield return x;
        foreach (T item in xs) {
            yield return item;
        }
    }

    public static IEnumerable<T> Append<T>(this IEnumerable<T> xs, IEnumerable<T> ys) {
        foreach (T item in xs) {
            yield return item;
        }
        foreach (T item in ys) {
            yield return item;
        }
    }

    public static bool Difference<T>(this HashSet<T> xs, HashSet<T> ys, out ImmutableHashSet<T> zs) {
        HashSet<T> ret = [];

        foreach (var item in ys) {
            if (!xs.Contains(item))
                ret.Add(item);
        }

        zs = [.. ret];
        return ret.Count > 0;
    }

}

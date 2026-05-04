using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;


public static class SyntaxHelper {
    public static string GetMethodName(this MethodDeclarationSyntax method)
        => method.ChildTokens().First(t => t.Kind().Equals(SyntaxKind.IdentifierToken)).ValueText;

    public static string GetConstructorType(this ConstructorDeclarationSyntax cstr)
        => cstr.ChildTokens().First(t => t.Kind().Equals(SyntaxKind.IdentifierToken)).ValueText;

    public static IEnumerable<T> Cons<T>(this T x, IEnumerable<T> xs) {
        yield return x;
        foreach (T item in xs) {
            yield return item;
        }
    }

    // public static bool NonEmptyDifference<T>(this HashSet<T> xs, HashSet<T> ys)
    //     => xs.Any(x => !ys.Contains(x));

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

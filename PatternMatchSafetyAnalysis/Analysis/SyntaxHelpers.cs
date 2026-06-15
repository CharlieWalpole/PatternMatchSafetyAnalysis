using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;


public static class SyntaxHelper {
    public static string GetMethodName(this MethodDeclarationSyntax method) => method.Identifier.ValueText;
    public static string GetConstructorType(this ConstructorDeclarationSyntax cstr) => cstr.Identifier.ValueText;
    public static string GetIdentifierName(this IdentifierNameSyntax var) => var.Identifier.ValueText;
    public static string GetFieldName(this FieldDeclarationSyntax f) => 
        f.Declaration.Variables.Last().Identifier.ValueText;
    public static IEnumerable<string> GetFieldPath(this FieldDeclarationSyntax f) =>
        ImmutableArray<string>.Empty.AddRange(f.Declaration.Variables.Select(v => v.Identifier.ValueText))[0..(f.Declaration.Variables.Count-1)];
    public static IEnumerable<VarName> GetFreeVariables(this SyntaxNode node, SemanticModel semantics) =>
        semantics.AnalyzeDataFlow(node).CapturedInside.Select(sym => sym.Name).Where(n => n is not null);

    public static IEnumerable<VarName> GetArgumentNames(this LambdaExpressionSyntax node) {
        if (node is ParenthesizedLambdaExpressionSyntax plambda) {
            return plambda.ParameterList.Parameters.Select(p => p.Identifier.ValueText);
        }
        else if (node is SimpleLambdaExpressionSyntax slambda) {
            return [slambda.Parameter.Identifier.ValueText];
        }
        else {
            throw new NotImplementedException($"Trying to get the argument list for an unknown lambda type: {node.GetType()}");
        }
    }

    public static IEnumerable<VarName> GetArgumentNames(this MethodDeclarationSyntax node) =>
        node.ParameterList.Parameters.Select(p => p.Identifier.ValueText);


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

    public static B GetOrDefault<A, B>(this IDictionary<A,B> mapping, A key, B def) {
        if(mapping.TryGetValue(key, out B? value)) return value;
        return def;
    }

}

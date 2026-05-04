using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis.Types;


public interface DeclarationSyntax {
    IEnumerable<SyntaxNode> DescendantNodes();
    string GetName();
    bool isMethodDecl { get; }
    bool isConstructorDecl { get; }
}

public record class MethodDecl(MethodDeclarationSyntax Defn) : DeclarationSyntax {
    public bool isMethodDecl => true;
    public bool isConstructorDecl => false;
    public IEnumerable<SyntaxNode> DescendantNodes() => Defn.DescendantNodes();
    public string GetName() => Defn.GetMethodName();
}

public record class ConstructorDecl(ConstructorDeclarationSyntax Defn) : DeclarationSyntax {
    public bool isMethodDecl => false;
    public bool isConstructorDecl => true;
    public IEnumerable<SyntaxNode> DescendantNodes() => Defn.DescendantNodes();
    public string GetName() => $"Constructor: {Defn.GetConstructorType()}";
}



public record class AnalysisUnit(ImmutableHashSet<DeclarationSyntax> Defns);


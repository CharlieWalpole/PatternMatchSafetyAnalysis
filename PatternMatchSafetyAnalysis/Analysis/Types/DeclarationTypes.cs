using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis.Types;


public interface DeclarationSyntax {
    IEnumerable<SyntaxNode> DescendantNodes();
    string GetName();
    bool isMethodDecl { get; }
    bool isConstructorDecl { get; }
    CSharpSyntaxNode DeclarationNode { get; }
    IEnumerable<VarName> GetArgumentNames();
    CSharpSyntaxNode GetBody();
}

public record class MethodDecl(MethodDeclarationSyntax Defn) : DeclarationSyntax {
    public bool isMethodDecl => true;
    public bool isConstructorDecl => false;

    public CSharpSyntaxNode DeclarationNode => Defn;

    public IEnumerable<SyntaxNode> DescendantNodes() => Defn.DescendantNodes();

    public IEnumerable<string> GetArgumentNames() => Defn.GetArgumentNames();

    public string GetName() => Defn.GetMethodName();
    public CSharpSyntaxNode GetBody() => Defn.Body ?? (CSharpSyntaxNode?)Defn.ExpressionBody?.Expression ?? throw new ArgumentException("Method declaration does not contain a body.");
}

public record class ConstructorDecl(ConstructorDeclarationSyntax Defn) : DeclarationSyntax {
    public bool isMethodDecl => false;
    public bool isConstructorDecl => true;
    public IEnumerable<SyntaxNode> DescendantNodes() => Defn.DescendantNodes();
    public CSharpSyntaxNode DeclarationNode => Defn;
    public string GetName() => Defn.GetMethodName();
    public IEnumerable<string> GetArgumentNames() => Defn.GetArgumentNames();
    public CSharpSyntaxNode GetBody() => Defn.Body ?? (CSharpSyntaxNode?)Defn.ExpressionBody?.Expression ?? throw new ArgumentException("Constructor declaration does not contain a body.");
}



public record class AnalysisUnit(ImmutableHashSet<DeclarationSyntax> Defns);


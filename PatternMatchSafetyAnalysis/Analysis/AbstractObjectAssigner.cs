using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Analysis.Types;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Analysis;

public class AbstractObjectIDAssigner(SemanticModel semanticModel) : CSharpSyntaxWalker {
    protected Dictionary<AbstractObjID, SyntaxNode> _AbstractObjectIDsToCodeLocations = [];
    protected Optional<ImmutableDictionary<AbstractObjID, SyntaxNode>> __AbstractObjectIDsToCodeLocations = new();
    public ImmutableDictionary<AbstractObjID, SyntaxNode> AbstractObjectIDsToCodeLocations {
        get {
            if (!__AbstractObjectIDsToCodeLocations.HasValue)
                __AbstractObjectIDsToCodeLocations = new(_AbstractObjectIDsToCodeLocations.ToImmutableDictionary());
            return __AbstractObjectIDsToCodeLocations.Value;
        }
    }


    public TypeEnv TypeMap = new([], []);


    protected HashSet<(AbstractObjID, FieldName, IFieldSymbol)> _HeapDomain = [];
    protected Optional<ImmutableHashSet<(AbstractObjID, FieldName, IFieldSymbol)>> __HeapDomain = new();
    public ImmutableHashSet<(AbstractObjID, FieldName, IFieldSymbol)> HeapDomain {
        get {
            if (!__HeapDomain.HasValue)
                __HeapDomain = new([.. _HeapDomain]);
            return __HeapDomain.Value;
        }
    }

    protected Dictionary<SyntaxNode, AbstractObjID> _CodeToID = [];
    protected Optional<ImmutableDictionary<SyntaxNode, AbstractObjID>> __CodeToID = new();
    public ImmutableDictionary<SyntaxNode, AbstractObjID> CodeToID {
        get {
            if (!__CodeToID.HasValue)
                __CodeToID = new([.. _CodeToID]);
            return __CodeToID.Value;
        }
    }

    protected HashSet<AbstractObjID> _MethodObjects = [];
    protected Optional<ImmutableHashSet<AbstractObjID>> __MethodObjects = new();
    public ImmutableHashSet<AbstractObjID> MethodObjects {
        get {
            if (!__MethodObjects.HasValue)
                __MethodObjects = new([.. _MethodObjects]);
            return __MethodObjects.Value;
        }
    }

    protected HashSet<AbstractObjID> _ClosureObjects = [];
    protected Optional<ImmutableHashSet<AbstractObjID>> __ClosureObjects = new();
    public ImmutableHashSet<AbstractObjID> ClosureObjects {
        get {
            if (!__ClosureObjects.HasValue)
                __ClosureObjects = new([.. _ClosureObjects]);
            return __ClosureObjects.Value;
        }
    }

    protected HashSet<AbstractObjID> _ClassObjects = [];
    protected Optional<ImmutableHashSet<AbstractObjID>> __ClassObjects = new();
    public ImmutableHashSet<AbstractObjID> ClassObjects {
        get {
            if (!__ClassObjects.HasValue)
                __ClassObjects = new([.. _ClassObjects]);
            return __ClassObjects.Value;
        }
    }


    protected SemanticModel semanticModel = semanticModel;
    protected AbstractObjID nextID = 0;


    public virtual bool IsClassSubtype(ClassName l, ClassName r) {
        if (l.Equals(r))
            return true;
        throw new NotImplementedException();
    }


    public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node);
        _CodeToID.Add(node, currentID);
        _ClosureObjects.Add(currentID);
        TypeMap = TypeMap.SetTypeArrow(currentID, TypeInference.Create(node));
    }

    public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node);
        _CodeToID.Add(node, currentID);
        _ClosureObjects.Add(currentID);
        TypeMap = TypeMap.SetTypeArrow(currentID, TypeInference.Create(node));
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node);
        _CodeToID.Add(node, currentID);
        _MethodObjects.Add(currentID);
        TypeMap = TypeMap.SetTypeArrow(currentID, TypeInference.Create(node));

        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node);
        _CodeToID.Add(node, currentID);
        _MethodObjects.Add(currentID);
        TypeMap = TypeMap.SetTypeArrow(currentID, TypeInference.Create(node));

        base.VisitConstructorDeclaration(node);
    }

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node);
        _CodeToID.Add(node, currentID);
        _ClassObjects.Add(currentID);

        if (node.Type.Kind() == SyntaxKind.IdentifierName) {
            string val = node.Type.ChildTokens().First().Text;
            TypeMap = TypeMap.SetTypeClass(currentID, new Class(val), node);
        }
        else {
            throw new Exception($"IDK: {node}");
        }

        var info = semanticModel.GetSymbolInfo(node);
        if (info.Symbol is not null) {
            //Console.WriteLine($"Single constructor symbol found for: {TypeMap[currentID]} with ID {currentID}.");
            bool addPrivate = true;
            Optional<INamedTypeSymbol> TypeSymbol = new(info.Symbol.ContainingType);
            //Console.WriteLine($"Containing Type symbol name is: {TypeSymbol.Value.Name}");

            while (TypeSymbol.HasValue) {
                foreach (var mem in TypeSymbol.Value.GetMembers().OfType<IFieldSymbol>()) {
                    if (addPrivate || !mem.DeclaredAccessibility.Equals(Accessibility.Private)) {
                        //Console.WriteLine($"Type symbol's field: {mem.Name}");
                        _HeapDomain.Add((currentID, mem.Name, mem));
                    }
                }
                addPrivate = false;
                if (TypeSymbol.Value.BaseType is INamedTypeSymbol t)
                    TypeSymbol = new(t);
                else
                    TypeSymbol = new();
            }

        }
        else {
            //Console.WriteLine($"Multiple constructor symbol found for: {TypeMap[currentID]} with ID {currentID}.");
            foreach (var item in info.CandidateSymbols) {
                bool addPrivate = true;
                Optional<INamedTypeSymbol> TypeSymbol = new(item.ContainingType);
                //Console.WriteLine($"Containing Type symbol name is: {TypeSymbol.Value.Name}");
                while (TypeSymbol.HasValue) {
                    foreach (var mem in TypeSymbol.Value.GetMembers().OfType<IFieldSymbol>()) {
                        if (addPrivate || !mem.DeclaredAccessibility.Equals(Accessibility.Private)) {
                            //Console.WriteLine($"Type symbol's field: {mem.Name}");
                            _HeapDomain.Add((currentID, mem.Name, mem));
                        }
                    }
                    addPrivate = false;
                    if (TypeSymbol.Value.BaseType is INamedTypeSymbol t)
                        TypeSymbol = new(t);
                    else
                        TypeSymbol = new();
                }
            }
        }
    }

    public IEnumerable<AbstractObjID> GetConstructorFromCreationExpression(ObjectCreationExpressionSyntax node) {
        var info = semanticModel.GetSymbolInfo(node);
        return info.Symbol.Cons(info.CandidateSymbols).Where(sym => sym is not null).Select(sym => sym!)
            .SelectMany(sym => sym.DeclaringSyntaxReferences)
            .Select(r => r.GetSyntax())
            .Select(n => CodeToID[n]);
    }

    public virtual string GetMethodName(AbstractObjID m) {
        if (!CodeToID.ContainsValue(m))
            throw new ArgumentException("Cannot get the name of an abstract object ID that has not been assigned by the AbstractObjectAssigner.");
        SyntaxNode node = CodeToID.First(kv => kv.Value.Equals(m)).Key;
        if (node is MethodDeclarationSyntax decl) {
            return decl.GetMethodName();
        }
        else if (node is ConstructorDeclarationSyntax cDecl) {
            return cDecl.GetMethodName();
        }
        else {
            throw new ArgumentException("Abstract object ID given was not a method or constructor declaration.");
        }
    }
}
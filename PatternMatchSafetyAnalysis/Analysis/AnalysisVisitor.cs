using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Environment = Analysis.Types.Environment;
using CodeDelta = Analysis.AbstractObjectIDAssigner;
using Type = Analysis.Types.Type;

namespace Analysis;


public class AnalysisVisitor : CSharpSyntaxVisitor<AnalysisResult> {
    protected readonly SemanticModel semantics;
    protected CodeDelta Delta;

    protected Dictionary<AbstractObjID, (ImmutableHashSet<InferenceVariable>, ImmutableHashSet<InferenceConstraint>, TypeInference)> MethodSummaries = [];

    public AnalysisVisitor(SemanticModel semanticModel, CodeDelta delta) {
        semantics = semanticModel;
        Delta = delta;
    }

    protected AnalysisResult CombineResults(IEnumerable<AnalysisResult> results, Environment Env, bool CollectReturn = true) {
        Environment In = Env;
        IEnumerable<InferenceConstraint> Cons = [];
        ObjectInference.Var Out = ObjectInference.Create();

        foreach (var res in results) {
            In = res.EndEnv;
            if(CollectReturn)
                Cons = [..Cons, ..res.Constraints, res.Return <= Out];
            else
                Cons = [..Cons, ..res.Constraints];
        }

        return new AnalysisResult([..Cons], CollectReturn ? Out : ObjectInference.Empty, In);
    }

    protected AnalysisResult SequenceAnalysis(IEnumerable<CSharpSyntaxNode> nodes, Environment Env, bool CollectReturn = true) {
        Environment In = Env;
        IEnumerable<InferenceConstraint> Cons = [];
        ObjectInference.Var Out = ObjectInference.Create();

        foreach (var item in nodes) {
            AnalysisResult res = HandleNode(item, In);
            In = res.EndEnv;
            if(CollectReturn)
                Cons = [..Cons, ..res.Constraints, res.Return <= Out];
            else
                Cons = [..Cons, ..res.Constraints];
        }

        return new AnalysisResult([..Cons], CollectReturn ? Out : ObjectInference.Empty, In);
    }


    [return: MaybeNull]
    public override AnalysisResult DefaultVisit(SyntaxNode node) {
        if (node is CSharpSyntaxNode csNode)
            return HandleNode(csNode);
        return null;
    }

    protected virtual AnalysisResult HandleNode(CSharpSyntaxNode node) => HandleNode(node, new Environment());

    public virtual AnalysisResult HandleNode(CSharpSyntaxNode node, Environment Env) => node switch {
        ExpressionSyntax expr => HandleExpression(expr, Env),
        StatementSyntax stmt => HandleStatement(stmt, Env),
        VariableDeclarationSyntax assign => HandleStackDeclaration(assign, Env),
        MethodDeclarationSyntax method => HandleFix(method, Env),
        _ => throw new NotImplementedException()
    };

    protected virtual AnalysisResult HandleFix(MethodDeclarationSyntax decl, Environment Env) {
        throw new NotImplementedException();
    }

    //Handles: Type x, y = e1, e2;
    //Does not handle: x = e;
    protected virtual AnalysisResult HandleStackDeclaration(VariableDeclarationSyntax stmt, Environment Env) => 
        SequenceAnalysis(stmt.Variables, Env, false);





    protected virtual AnalysisResult HandleExpression(ExpressionSyntax expr, Environment Env) => expr switch {
        AssignmentExpressionSyntax assign => HandleAssign(assign, Env),
        IdentifierNameSyntax var => HandleVariable(var, Env),
        ObjectCreationExpressionSyntax cstr => HandleConstructor(cstr, Env),
        InvocationExpressionSyntax app => HandleApplication(app, Env),
        MemberAccessExpressionSyntax field => HandleField(field, Env),
        LambdaExpressionSyntax lambda => HandleClosure(lambda, Env),
        _ => throw new NotImplementedException()
    };

    //Is: e1.e2. ... .en.f = e;
    //For some: 0 <= n
    //This is an expression for some reason.
    //Must handle both stack and heap Updates.
    protected virtual AnalysisResult HandleAssign(AssignmentExpressionSyntax stmt, Environment Env) {
        throw new NotImplementedException();
    }

    protected virtual AnalysisResult HandleVariable(IdentifierNameSyntax expr, Environment Env) {
        //Some method/field lookups may be in this case; M() and f() are syntactically identical. 
        ObjectInference ObjRet = ObjectInference.Create();
        Environment Out = Env.GetFresh();
        string VarName = expr.GetIdentifierName();
        if(Env.StackMap.ContainsKey(VarName)) {
            ObjectInference GetVar = Env[VarName];
            return new AnalysisResult([GetVar <= ObjRet, .. Env <= Out], ObjRet, Out);
        } else {
            SymbolInfo v = semantics.GetSymbolInfo(expr);
            IEnumerable<AnalysisResult> x = v.Symbol.Cons(v.CandidateSymbols)
                .Where(s => s is not null).Select(s => s!)
                .SelectMany(s => s.DeclaringSyntaxReferences)
                .Select(n => n.GetSyntax())
                .Select(n => n is MethodDeclarationSyntax m ? HandleMethodLookup(m, Env) :
                    (n is FieldDeclarationSyntax f ? HandleThisFieldLookup(f.GetFieldName(), Env) : 
                    throw new NotImplementedException($"Identifier {expr.Identifier.ValueText} has an unknown declaration site type: {n.GetType()}."))
                );
            return new AnalysisResult([
                ..x.SelectMany(res => res.Constraints),
                ..x.SelectMany(res => res.EndEnv <= Out),
                ..x.Select(res => res.Return <= ObjRet)
            ], ObjRet, Out);
        }
    }

    protected virtual AnalysisResult HandleMethodLookup(MethodDeclarationSyntax method, Environment Env) {
        if(Env.StackMap.ContainsKey(method.GetMethodName())) { // (Co-)Recursive call/lookup
            return new AnalysisResult([], Env.StackMap[method.GetMethodName()], Env);
        } else { // Summarized method lookup
            AbstractObjID m = Delta.CodeToID[method];
            (ImmutableHashSet<InferenceVariable> vars, ImmutableHashSet<InferenceConstraint> cons, TypeInference t) = MethodSummaries[m];
            ImmutableDictionary<InferenceVariable, InferenceVariable> freshMap = [
                ..vars.Select(v => v is ObjectInference o ? new KeyValuePair<InferenceVariable, InferenceVariable>(o, ObjectInference.Create()) :
                        v is TypeInference t ? new KeyValuePair<InferenceVariable, InferenceVariable>(t, TypeInference.Create()) :
                        new KeyValuePair<InferenceVariable, InferenceVariable>(v, AliasInference.Create())
                )
            ];
            Environment Out = Env.GetFresh();
            ImmutableHashSet<InferenceConstraint> RetCons = [
                ..cons.Select(c => c.Substitute(freshMap)),
                ..Env <= Out,
                (TypeInference)freshMap[t] <= Out.TypeMap[m]
            ];
            
            return new AnalysisResult(RetCons, new ObjectInference.Literal([m]), Out);
        }
    }

    protected virtual AnalysisResult HandleThisFieldLookup(FieldName name, Environment Env) {
        ObjectInference ths = Env.StackMap["this"];
        ObjectInference.Var X = ObjectInference.Create();
        ObjectInference.Var Y = ObjectInference.Create();
        ObjectInference.Var Z = ObjectInference.Create();
        Environment Out = Env.GetFresh();

        return new AnalysisResult([
            ..Env <= Out,
            ths <= Z, Z <= ths,
            (ObjectInference)X <= Y,
            new InferenceConstraint.HeapLookup(X, Env, Z, name)
        ], Y, Out);
    }

    protected virtual AnalysisResult HandleConstructor(ObjectCreationExpressionSyntax expr, Environment Env) {
        throw new NotImplementedException();
    }

    protected virtual AnalysisResult HandleApplication(InvocationExpressionSyntax expr, Environment Env) {
        //Must handle both method and application rules
        //Can assume that the method summary constraints have already been added. 

        ObjectInference.Var X = ObjectInference.Create();
        ObjectInference.Var R = ObjectInference.Create();
        TypeInference.Var Z = TypeInference.Create();
        Environment G2 = Env.GetFresh();
        Environment Gr = Env.GetFresh();

        AnalysisResult res0 = HandleNode(expr.Expression, Env);
        
        IEnumerable<InferenceConstraint> Cons = [..res0.Constraints];
        Environment In = res0.EndEnv;
        List<ObjectInference> ArgumentReturns = [];

        foreach (ArgumentSyntax arg in expr.ArgumentList.Arguments) {
            AnalysisResult res = HandleExpression(arg.Expression, In);
            Cons = [..Cons, ..res.Constraints];
            In = res.EndEnv;
            ArgumentReturns.Add(res.Return);
        }

        ImmutableList<(ObjectInference, ObjectInference.Var)> Args = [..ArgumentReturns.Select(arg => (arg, ObjectInference.Create()))];
        ObjectInference.Var Of = ObjectInference.Create();

        return new AnalysisResult(
            [
                ..Cons, 
                Of <= res0.Return, res0.Return <= Of,
                .. Args.SelectMany<(ObjectInference, ObjectInference.Var), InferenceConstraint>(kv => [kv.Item1 <= kv.Item2, kv.Item2 <= kv.Item1]),
                (ObjectInference)R <= X, 
                new InferenceConstraint.ApplicationResolution(G2, R, Z, Gr, In, Of, [..Args.Select(kv => kv.Item2)])
            ], X, G2);
    }

    protected virtual AnalysisResult HandleField(MemberAccessExpressionSyntax expr, Environment Env) {
        AnalysisResult res = HandleExpression(expr.Expression, Env);
        ObjectInference.Var X = ObjectInference.Create();
        ObjectInference.Var Y = ObjectInference.Create();
        ObjectInference.Var Z = ObjectInference.Create();
        return new AnalysisResult(
            [
                res.Return <= Z,
                new InferenceConstraint.HeapLookup(X, res.EndEnv, Z, expr.Name.GetText().ToString()),
                (ObjectInference)X <= Y,
                ..res.Constraints
            ],
            Y,
            res.EndEnv
        );
    }

    protected virtual AnalysisResult HandleClosure(LambdaExpressionSyntax expr, Environment Env) {
        IEnumerable<VarName> Vc = expr.Body.GetFreeVariables(semantics);
        ObjectInference Xr = ObjectInference.Create();
        Dictionary<VarName, ObjectInference> StackEnv = expr.GetArgumentNames()
            .Select(n => new KeyValuePair<VarName, ObjectInference>(n, ObjectInference.Create())).ToDictionary(); //Arguments
        foreach (VarName capture in Vc) {
            StackEnv.Add(capture, Env[capture]);
        }
        Environment In = Env with { StackMap = new StackEnv([StackEnv.ToImmutableDictionary()]) };
        AnalysisResult res = HandleNode(expr.Body, In);

        if(!Delta.CodeToID.ContainsKey(expr))
            throw new ArgumentException($"Analysing closure that was not assigned an Abstract Object ID. Node occurred at {expr.FullSpan}.");
        AbstractObjID Of = Delta.CodeToID[expr];

        TypeInference TOf = Env[Of];

        return new AnalysisResult(
            [
                Of <= Xr, 
                new Arrow([.. expr.GetArgumentNames()], In, res.EndEnv, res.Return) <= TOf,
                .. res.Constraints
            ], Xr, In);
    }





    protected virtual AnalysisResult HandleStatement(StatementSyntax stmt, Environment Env) => stmt switch {
        ExpressionStatementSyntax exprStmt => HandleExpression((exprStmt.ChildNodes().First() as ExpressionSyntax)!, Env),
        IfStatementSyntax If => HandleIf(If, Env),
        WhileStatementSyntax While => HandleWhile(While, Env),
        SwitchStatementSyntax Match => HandleMatch(Match, Env),
        BlockSyntax Block => HandleBlock(Block, Env),
        ReturnStatementSyntax Return => HandleReturn(Return, Env),
        EmptyStatementSyntax Skip => HandleSkip(Skip, Env),
        _ => throw new NotImplementedException()
    };

    protected virtual AnalysisResult HandleSequence(IEnumerable<StatementSyntax> Stmts, Environment Env) => 
        SequenceAnalysis(Stmts, Env, true);

    protected virtual AnalysisResult HandleIf(IfStatementSyntax stmt, Environment Env) {
        
        TypeInference.Var X = TypeInference.Create();
        ObjectInference.Var Guard = ObjectInference.Create();

        AnalysisResult resG = HandleExpression(stmt.Condition, Env);
        AnalysisResult resIf = HandleStatement(stmt.Statement, resG.EndEnv);

        Environment Out = resIf.EndEnv.GetFresh();
        ObjectInference OutObj = ObjectInference.Create();

        IEnumerable<InferenceConstraint> Cons = [
            resG.Return <= Guard, Guard <= resG.Return,
            new InferenceConstraint.TypeLookup(X, resG.EndEnv, Guard),
            (TypeInference)X <= new Class("Bool"),
            ..resG.Constraints,
            ..resIf.Constraints,
            ..resIf.EndEnv <= Out,
            resIf.Return <= OutObj
        ];

        if(stmt.Else is not null) {
            AnalysisResult resElse = HandleStatement(stmt.Else.Statement, resG.EndEnv);
            Cons = [
                ..Cons,
                ..resElse.Constraints,
                ..resElse.EndEnv <= Out,
                resElse.Return <= OutObj
            ];
        }

        return new AnalysisResult([..Cons], OutObj, Out);
    }

    protected virtual AnalysisResult HandleWhile(WhileStatementSyntax stmt, Environment Env) {
        TypeInference.Var X = TypeInference.Create();
        ObjectInference.Var Guard = ObjectInference.Create();

        AnalysisResult resG = HandleExpression(stmt.Condition, Env);

        Environment Out = resG.EndEnv.GetFresh();

        AnalysisResult Body = HandleStatement(stmt.Statement, Out);

        IEnumerable<InferenceConstraint> Cons = [
            resG.Return <= Guard, Guard <= resG.Return,
            new InferenceConstraint.TypeLookup(X, resG.EndEnv, Guard),
            (TypeInference)X <= new Class("Bool"),
            ..Body.EndEnv <= Out, ..Out <= Body.EndEnv,
            ..Body.EndEnv <= Env,
            ..resG.EndEnv <= Out,
            ..resG.Constraints, ..Body.Constraints
        ];

        return new AnalysisResult([..Cons], Body.Return, Out);
    }

    protected virtual AnalysisResult HandleMatch(SwitchStatementSyntax stmt, Environment Env) {
        throw new NotImplementedException();
    }

    protected virtual AnalysisResult HandleBlock(BlockSyntax stmt, Environment Env) {
        Environment BlockIn = Env.Push();
        IEnumerable<StatementSyntax> stmts = stmt.Statements;
        AnalysisResult res = HandleSequence(stmts, BlockIn);
        return res with { EndEnv = res.EndEnv.Pop() };

    }

    protected virtual AnalysisResult HandleReturn(ReturnStatementSyntax stmt, Environment Env) {
        ExpressionSyntax? expr = stmt.Expression;
        if (expr is ExpressionSyntax exp)
            return HandleExpression(exp, Env);
        else
            return new AnalysisResult([], ObjectInference.Empty, Env);
    }

    protected virtual AnalysisResult HandleSkip(EmptyStatementSyntax stmt, Environment Env) {
        Environment Out = Env.GetFresh();
        return new AnalysisResult([..Env <= Out], ObjectInference.Empty, Out);
    }

}

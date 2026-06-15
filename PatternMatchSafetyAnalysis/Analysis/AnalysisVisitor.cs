using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Environment = Analysis.Types.Environment;
using CodeDelta = Analysis.AbstractObjectIDAssigner;
using Type = Analysis.Types.Type;
using System.Diagnostics;
using System.Data;
using System.Reflection.Metadata;

namespace Analysis;


public class AnalysisVisitor : CSharpSyntaxVisitor<AnalysisResult> {
    protected readonly SemanticModel semantics;
    protected CodeDelta Delta;

    protected Dictionary<AbstractObjID, MethodSummary> MethodSummaries = [];

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
            if (CollectReturn)
                Cons = [.. Cons, .. res.Constraints, res.Return <= Out];
            else
                Cons = [.. Cons, .. res.Constraints];
        }

        return new AnalysisResult([.. Cons], CollectReturn ? Out : ObjectInference.Empty, In);
    }

    protected AnalysisResult SequenceAnalysis(IEnumerable<CSharpSyntaxNode> nodes, Environment Env, bool CollectReturn = true) {
        Environment In = Env;
        IEnumerable<InferenceConstraint> Cons = [];
        ObjectInference.Var Out = ObjectInference.Create();

        foreach (var item in nodes) {
            AnalysisResult res = HandleNode(item, In);
            In = res.EndEnv;
            if (CollectReturn)
                Cons = [.. Cons, .. res.Constraints, res.Return <= Out];
            else
                Cons = [.. Cons, .. res.Constraints];
        }

        return new AnalysisResult([.. Cons], CollectReturn ? Out : ObjectInference.Empty, In);
    }


    [return: MaybeNull]
    public override AnalysisResult DefaultVisit(SyntaxNode node) {
        if (node is CSharpSyntaxNode csNode)
            return HandleNode(csNode);
        return null;
    }

    protected virtual AnalysisResult HandleNode(CSharpSyntaxNode node) => HandleNode(node, MakeInitialEnvironment());

    protected virtual Environment MakeInitialEnvironment() => new Environment(
        new StackEnv([]),
        new HeapEnv(Delta.HeapDomain.Select(kv => new KeyValuePair<(AbstractObjID, FieldName), ObjectSet>((kv.Item1, kv.Item2), ObjectInference.Create())).ToImmutableDictionary()),
        Delta.TypeMap,
        new AliasEnv(Delta.CodeToID.Values.Select(id => new KeyValuePair<AbstractObjID, AliasInference>(id, AliasInference.Create())).ToImmutableDictionary())
    );

    public virtual AnalysisResult HandleNode(CSharpSyntaxNode node, Environment Env) => node switch {
        ExpressionSyntax expr => HandleExpression(expr, Env),
        StatementSyntax stmt => HandleStatement(stmt, Env),
        VariableDeclarationSyntax assign => HandleStackDeclaration(assign, Env),
        MethodDeclarationSyntax method => HandleFix(method, Env),
        _ => throw new NotImplementedException()
    };

    protected virtual AnalysisResult HandleFix(MethodDeclarationSyntax decl, Environment Env) {
        CSharpSyntaxNode body = decl.Body ?? (CSharpSyntaxNode?)decl.ExpressionBody?.Expression ?? throw new ArgumentException("Method declaration does not contain a body.");

        if (!Delta.CodeToID.ContainsKey(decl))
            throw new ArgumentException($"Analysing method declaration that was not assigned an Abstract Object ID. Node occurred at {decl.FullSpan}.");
        AbstractObjID Of = Delta.CodeToID[decl];

        AnalysisResult ret = HandleMethodClosureBody(body, decl.GetArgumentNames(), Of, Env.Push(ImmutableDictionary<VarName,ObjectInference>.Empty.Add(decl.Identifier.ValueText, ObjectInference.Create([Of]))));

        return ret with { EndEnv = ret.EndEnv.Pop() };
    }

    // TODO: How to handle recursive constructors? Names are hard.
    // protected virtual AnalysisResult HandleFix(AnalysisUnit unit) {
    //     Environment Env = MakeInitialEnvironment().Push(unit.Defns.Select(decl => new KeyValuePair<VarName, ObjectInference>(decl.)));

    // }

    protected virtual AnalysisResult HandleMethodClosureBody(CSharpSyntaxNode body, IEnumerable<VarName> ArgNames, AbstractObjID MethodID, Environment Env, bool doCaptures = true) {
        IEnumerable<VarName> Vc = body.GetFreeVariables(semantics);
        ObjectInference Xr = ObjectInference.Create();
        Dictionary<VarName, ObjectInference> StackEnv = ArgNames
            .Select(n => new KeyValuePair<VarName, ObjectInference>(n, ObjectInference.Create())).ToDictionary(); //Arguments

        if (doCaptures) {
            foreach (VarName capture in Vc) {
                StackEnv.Add(capture, Env[capture]); //TODO: Handle implicit 'this' field lookups.
            }
        }

        Environment In = Env with { StackMap = new StackEnv([StackEnv.ToImmutableDictionary()]) };
        AnalysisResult res = HandleNode(body, In);

        // if (!Delta.CodeToID.ContainsKey(expr))
        //     throw new ArgumentException($"Analysing closure that was not assigned an Abstract Object ID. Node occurred at {expr.FullSpan}.");
        // AbstractObjID Of = Delta.CodeToID[expr];

        TypeInference TOf = Env[MethodID];

        return new AnalysisResult(
            [
                MethodID <= Xr,
                new Arrow([.. ArgNames], In, res.EndEnv, res.Return) <= TOf,
                .. res.Constraints
            ], Xr, In);
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
        if (stmt.Left is IdentifierNameSyntax v) {
            AnalysisResult res = HandleNode(stmt.Right, Env);
            if (Env.StackMap.ContainsKey(v.GetIdentifierName())) { //Stack Assignment
                ObjectInference.Var Out = ObjectInference.Create();
                return new AnalysisResult([.. res.Constraints, res.Return <= Out, Out <= res.Return], Out, res.EndEnv with { StackMap = res.EndEnv.StackMap.SetVar(v.Identifier.ValueText, Out) });
            }
            else { //implicit 'this' field update
                AnalysisResult l = HandleNode(stmt.Left, Env);
                AnalysisResult r = HandleNode(stmt.Right, l.EndEnv);
                ObjectInference.Var Out = ObjectInference.Create();
                Environment ret = r.EndEnv.GetFresh();
                return new AnalysisResult(
                    [..l.Constraints, ..r.Constraints, l.Return <= Out, Out <= l.Return,
                    new InferenceConstraint.HeapUpdate(ret, r.EndEnv, Out, "this", r.Return)],
                    Out,
                    ret
                );
            }
        }
        else if (stmt.Left is MemberAccessExpressionSyntax expr) { //Heap Update
            AnalysisResult l = HandleNode(expr.Expression, Env);
            AnalysisResult r = HandleNode(stmt.Right, l.EndEnv);
            ObjectInference.Var Out = ObjectInference.Create();
            Environment ret = r.EndEnv.GetFresh();
            return new AnalysisResult(
                [..l.Constraints, ..r.Constraints, l.Return <= Out, Out <= l.Return,
                new InferenceConstraint.HeapUpdate(ret, r.EndEnv, Out, expr.Name.Identifier.ValueText, r.Return)],
                Out,
                ret
            );
        }
        else {
            throw new NotImplementedException($"Unknown LHS to an assignment at {stmt.FullSpan} of syntax kind: {stmt.Left.Kind}.");
        }
    }

    protected virtual AnalysisResult HandleVariable(IdentifierNameSyntax expr, Environment Env) {
        //Some method/field lookups may be in this case; M() and f() are syntactically identical. 
        ObjectInference ObjRet = ObjectInference.Create();
        Environment Out = Env.GetFresh();
        string VarName = expr.GetIdentifierName();
        if (Env.StackMap.ContainsKey(VarName)) {
            ObjectInference GetVar = Env[VarName];
            return new AnalysisResult([GetVar <= ObjRet, .. Env <= Out], ObjRet, Out);
        }
        else {
            SymbolInfo v = semantics.GetSymbolInfo(expr);
            IEnumerable<AnalysisResult> x = v.Symbol.Cons(v.CandidateSymbols)
                .Where(s => s is not null).Select(s => s!)
                .SelectMany(s => s.DeclaringSyntaxReferences)
                .Select(n => n.GetSyntax())
                .Select(n => n is MethodDeclarationSyntax m ? HandleMethodLookup(m, Env, new Optional<ObjectInference.Var>()) :
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

    protected virtual AnalysisResult HandleMethodLookup(MethodDeclarationSyntax method, Environment Env, Optional<ObjectInference.Var> ThisVariable) {
        if (Env.StackMap.ContainsKey(method.GetMethodName())) { // (Co-)Recursive call/lookup
            return new AnalysisResult([], Env.StackMap[method.GetMethodName()], Env);
        }
        else { // Summarized method lookup
            AbstractObjID m = Delta.CodeToID[method];
            return HandleMethodLookup(m, Env, ThisVariable);
        }
    }

    protected virtual AnalysisResult HandleMethodLookup(AbstractObjID m, Environment Env, Optional<ObjectInference.Var> ThisVariable) {
        MethodSummary sum = MethodSummaries[m];
        //(ImmutableHashSet<InferenceVariable> vars, ImmutableHashSet<InferenceConstraint> cons, TypeInference t) = MethodSummaries[m];
        ImmutableDictionary<InferenceVariable, InferenceVariable> freshMap = [
            ..sum.InferenceVariables.Select(v => v is ObjectInference o ? new KeyValuePair<InferenceVariable, InferenceVariable>(o, ObjectInference.Create()) :
                        v is TypeInference t ? new KeyValuePair<InferenceVariable, InferenceVariable>(t, TypeInference.Create()) :
                        new KeyValuePair<InferenceVariable, InferenceVariable>(v, AliasInference.Create())
                )
        ];
        if (ThisVariable.HasValue)
            freshMap = freshMap.Remove(sum.ThisVariable).Add(sum.ThisVariable, ThisVariable.Value);
        else
            freshMap = freshMap.Remove(sum.ThisVariable).Add(sum.ThisVariable, Env["this"]);

        Environment Out = Env.GetFresh();
        ImmutableHashSet<InferenceConstraint> RetCons = [
            ..sum.Constraints.Select(c => c.Substitute(freshMap)),
            ..Env <= Out,
            (TypeInference)freshMap[sum.MethodType] <= Out.TypeMap[m]
        ];

        return new AnalysisResult(RetCons, new ObjectInference.Literal([m]), Out);
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
        IEnumerable<AbstractObjID> defIDs = Delta.GetConstructorFromCreationExpression(expr);
        ObjectInference.Var Cstr = ObjectInference.Create();
        ObjectInference ID = ObjectInference.Create(Delta.CodeToID[expr]);

        AnalysisResult res0 = CombineResults(defIDs.Select(m => HandleMethodLookup(m, Env, new Optional<ObjectInference.Var>(Cstr))), Env);
        AnalysisResult AppRes = HandleApplication(res0, expr.ArgumentList, res0.EndEnv);

        return AppRes with { Return = ID, Constraints = AppRes.Constraints.Add(Cstr <= ID).Add(ID <= Cstr) };
    }

    protected virtual AnalysisResult HandleApplication(InvocationExpressionSyntax expr, Environment Env) {
        //Must handle both method and application rules
        //Can assume that the method summary constraints have already been added. 

        AnalysisResult res0 = HandleNode(expr.Expression, Env);

        return HandleApplication(res0, expr.ArgumentList, res0.EndEnv);
    }

    protected virtual AnalysisResult HandleApplication(AnalysisResult functions, ArgumentListSyntax? args, Environment Env) {
        ObjectInference.Var X = ObjectInference.Create();
        ObjectInference.Var R = ObjectInference.Create();
        TypeInference.Var Z = TypeInference.Create();
        Environment G2 = Env.GetFresh();
        Environment Gr = Env.GetFresh();

        IEnumerable<InferenceConstraint> Cons = [.. functions.Constraints];
        Environment In = functions.EndEnv;
        List<ObjectInference> ArgumentReturns = [];

        if (args is not null) {
            foreach (ArgumentSyntax arg in args.Arguments) {
                AnalysisResult res = HandleExpression(arg.Expression, In);
                Cons = [.. Cons, .. res.Constraints];
                In = res.EndEnv;
                ArgumentReturns.Add(res.Return);
            }
        }

        ImmutableList<(ObjectInference, ObjectInference.Var)> Args = [.. ArgumentReturns.Select(arg => (arg, ObjectInference.Create()))];
        ObjectInference.Var Of = ObjectInference.Create();

        return new AnalysisResult(
            [
                ..Cons,
                Of <= functions.Return, functions.Return <= Of,
                .. Args.SelectMany<(ObjectInference, ObjectInference.Var), InferenceConstraint>(kv => [kv.Item1 <= kv.Item2, kv.Item2 <= kv.Item1]),
                (ObjectInference)R <= X,
                new InferenceConstraint.ApplicationResolution(G2, R, Z, Gr, In, Of, [..Args.Select(kv => kv.Item2)])
            ], X, G2);
    }

    protected virtual AnalysisResult HandleField(MemberAccessExpressionSyntax expr, Environment Env) { //Needs to handle both field and method lookups
        AnalysisResult res = HandleExpression(expr.Expression, Env);
        ObjectInference.Var Z = ObjectInference.Create();

        var info = semantics.GetSymbolInfo(expr);
        IEnumerable<SyntaxNode> decls = info.Symbol.Cons(info.CandidateSymbols).Where(sym => sym is not null)
            .SelectMany(sym => sym!.DeclaringSyntaxReferences)
            .Select(r => r.GetSyntax());
        bool isMethod = decls
            .Any(node => node is MethodDeclarationSyntax);

        if (isMethod) { //Is method lookup
            return CombineResults(decls
                .Select(node => (node as MethodDeclarationSyntax)!)
                .Select(node => HandleMethodLookup(node, res.EndEnv, Z)), res.EndEnv, true);
        }
        else { //Is field lookup
            ObjectInference.Var X = ObjectInference.Create();
            ObjectInference.Var Y = ObjectInference.Create();
            return new AnalysisResult(
                [
                    res.Return <= Z, Z <= res.Return,
                new InferenceConstraint.HeapLookup(X, res.EndEnv, Z, expr.Name.GetText().ToString()),
                (ObjectInference)X <= Y,
                ..res.Constraints
                ],
                Y,
                res.EndEnv
            );
        }
    }

    protected virtual AnalysisResult HandleClosure(LambdaExpressionSyntax expr, Environment Env) {
        if (!Delta.CodeToID.ContainsKey(expr))
            throw new ArgumentException($"Analysing closure that was not assigned an Abstract Object ID. Node occurred at {expr.FullSpan}.");
        AbstractObjID Of = Delta.CodeToID[expr];

        return HandleMethodClosureBody(expr.Body, expr.GetArgumentNames(), Of, Env);
    }





    protected virtual AnalysisResult HandleStatement(StatementSyntax stmt, Environment Env) => stmt switch {
        ExpressionStatementSyntax exprStmt => HandleExpression(exprStmt.Expression, Env) with { Return = ObjectInference.Empty },
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

        if (stmt.Else is not null) {
            AnalysisResult resElse = HandleStatement(stmt.Else.Statement, resG.EndEnv);
            Cons = [
                ..Cons,
                ..resElse.Constraints,
                ..resElse.EndEnv <= Out,
                resElse.Return <= OutObj
            ];
        }

        return new AnalysisResult([.. Cons], OutObj, Out);
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

        return new AnalysisResult([.. Cons], Body.Return, Out);
    }

    protected virtual AnalysisResult HandleMatch(SwitchStatementSyntax stmt, Environment Env) {
        if (!stmt.Sections.All(c => c.Labels.All(p => p is CasePatternSwitchLabelSyntax pat && IsPatternValid(pat))))
            throw new ArgumentException($"Switch statement contains a case label that is not supported: {stmt}");

        var data = stmt.Sections.SelectMany(s => s.Labels.Select(l => (GetPatternData(l), s.Statements)));
        TypeInference branchBound = TypeInference.Create([..data.Select(kkv => kkv.Item1.Item1)]);


        AnalysisResult expr = HandleExpression(stmt.Expression, Env);
        ObjectInference.Var exprOut = ObjectInference.Create();
        TypeInference.Var exprType = TypeInference.Create();

        Environment branchOut = expr.EndEnv.GetFresh();
        HashSet<InferenceConstraint> cons = [
            .. expr.Constraints,
            exprOut <= expr.Return, expr.Return <= exprOut,
            new InferenceConstraint.TypeLookup(exprType, expr.EndEnv, exprOut),
            exprType <= branchBound
        ];
        ObjectInference.Var ret = ObjectInference.Create();

        foreach (var d in data) {
            ObjectInference.Var resOut = ObjectInference.Create();

            InferenceConstraint restrict = new InferenceConstraint.Restriction(resOut, expr.EndEnv, exprOut, d.Item1.Item1);
            Environment branchIn = expr.EndEnv.Push(ImmutableDictionary<VarName, ObjectInference>.Empty.Add(d.Item1.Item2, resOut));

            AnalysisResult res = HandleSequence(d.Statements, branchIn);
            cons.Add(restrict);
            cons.Add(new InferenceConstraint.Conditional(
                [new InferenceConstraint.SubTyping(TypeInference.Create([d.Item1.Item1]), exprType)],
                [],
                [.. res.Constraints, .. res.EndEnv.Pop() <= branchOut, res.Return <= ret])
            );
        }

        return new AnalysisResult([..cons], ret, branchOut);
    }

    protected virtual bool IsPatternValid(CasePatternSwitchLabelSyntax lbl) {
        if (lbl is CasePatternSwitchLabelSyntax c) {
            if (c.Pattern is DeclarationPatternSyntax decl) {
                return decl.Type is IdentifierNameSyntax && decl.Designation is SingleVariableDesignationSyntax;
            }
        }
        return false;
    }
    protected virtual bool IsPatternValid(SwitchExpressionArmSyntax arm) {
        if (arm.Pattern is DeclarationPatternSyntax decl) {
            return decl.Type is IdentifierNameSyntax && decl.Designation is SingleVariableDesignationSyntax;
        }
        return false;
    }
    protected virtual (Class, VarName) GetPatternData(SwitchLabelSyntax lbl) {
        DeclarationPatternSyntax pat = (lbl as CasePatternSwitchLabelSyntax)?.Pattern as DeclarationPatternSyntax
            ?? throw new ArgumentException($"Pattern does not match expected form: {lbl}");
        return GetPatternData(pat);
    }
    protected virtual (Class, VarName) GetPatternData(SwitchExpressionArmSyntax arm) {
        DeclarationPatternSyntax pat = arm.Pattern as DeclarationPatternSyntax
            ?? throw new ArgumentException($"Pattern does not match expected form: {arm}");
        return GetPatternData(pat);
    }
    protected virtual (Class, VarName) GetPatternData(DeclarationPatternSyntax pat) {
        string type = (pat.Type as IdentifierNameSyntax)?.Identifier.ValueText ?? throw new ArgumentException($"Pattern does not match expected form: {pat}");
        string name = (pat.Designation as SingleVariableDesignationSyntax)?.Identifier.ValueText ?? throw new ArgumentException($"Pattern does not match expected form: {pat}");
        return (new Class(type), name);
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
        return new AnalysisResult([.. Env <= Out], ObjectInference.Empty, Out);
    }

}

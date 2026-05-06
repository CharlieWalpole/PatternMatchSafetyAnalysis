using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Environment = Analysis.Types.Environment;

namespace Analysis;


public class AnalysisVisitor : CSharpSyntaxVisitor<AnalysisResult> {
    [return: MaybeNull]
    public override AnalysisResult DefaultVisit(SyntaxNode node) {
        if (node is CSharpSyntaxNode csNode)
            return HandleNode(csNode);
        return null;
    }

    protected virtual AnalysisResult HandleNode(CSharpSyntaxNode node) => HandleNode(node, new Environment());

    protected virtual AnalysisResult HandleNode(CSharpSyntaxNode node, Environment Env) => node switch {
        ExpressionSyntax expr => HandleExpression(expr, Env),
        StatementSyntax stmt => HandleStatement(stmt, Env),
        VariableDeclarationSyntax assign => HandleStackDeclaration(assign, Env),
        MethodDeclarationSyntax method => HandleFix(method, Env),
        _ => throw new NotImplementedException()
    };

    protected virtual AnalysisResult HandleFix(MethodDeclarationSyntax decl, Environment Env) {
        throw new NotImplementedException();
    }

    //Handles: Type x = e;
    //Does not handle: x = e;
    protected virtual AnalysisResult HandleStackDeclaration(VariableDeclarationSyntax stmt, Environment Env) {
        throw new NotImplementedException();
    }





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
        ObjectInference ObjRet = ObjectInference.Create();
        Environment Out = Env.GetFresh();
        ObjectInference GetVar = Env[expr.GetIdentifierName()];
        return new AnalysisResult([GetVar <= ObjRet, .. Env <= Out], ObjRet, Out);
    }

    protected virtual AnalysisResult HandleConstructor(ObjectCreationExpressionSyntax expr, Environment Env) {
        throw new NotImplementedException();
    }

    protected virtual AnalysisResult HandleApplication(InvocationExpressionSyntax expr, Environment Env) {
        //Must handle both method and application rules
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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

    protected virtual AnalysisResult HandleSequence(IEnumerable<StatementSyntax> Stmts, Environment Env) {
        Environment In = Env;
        IEnumerable<InferenceConstraint> constraints = [];
        ObjectInference ret = ObjectInference.Empty;

        foreach (StatementSyntax stmt in Stmts) {
            AnalysisResult res = HandleStatement(stmt, In);
            In = res.EndEnv;
            constraints = constraints.Append(res.Constraints);
            ret = res.Return;
        }

        return new AnalysisResult([.. constraints], ret, In);
    }

    protected virtual AnalysisResult HandleIf(IfStatementSyntax stmt, Environment Env) {
        throw new NotImplementedException();
    }

    protected virtual AnalysisResult HandleWhile(WhileStatementSyntax stmt, Environment Env) {
        throw new NotImplementedException();
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

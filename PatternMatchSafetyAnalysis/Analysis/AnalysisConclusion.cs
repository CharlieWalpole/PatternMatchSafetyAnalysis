using System.Collections.Immutable;
using Microsoft.CodeAnalysis;


namespace Analysis;


public record class AnalysisError(string SourceFile, SyntaxNode Source, string SinkFile, SyntaxNode Sink);


public record class AnalysisConclusion(ImmutableList<AnalysisError> Errors);

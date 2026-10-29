namespace KillerBench.Engine.Infrastructure;

// These live in Engine.Core so RecipeRunner can tell a tool that never started from one that ran.

/// <summary>Proves that the requested native executable was never started.</summary>
public sealed class ToolNotStartedException(string message, Exception? innerException = null) : IOException(message, innerException);

/// <summary>Cancellation prevented the requested executable from reaching process startup.</summary>
public sealed class ToolStartCanceledException(CancellationToken token, Exception? innerException = null)
    : OperationCanceledException("The native executable was canceled before process startup.", innerException, token);

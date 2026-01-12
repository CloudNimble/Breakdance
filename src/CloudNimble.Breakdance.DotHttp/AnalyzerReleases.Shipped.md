; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 8.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DOTHTTP001 | DotHttp | Error | RequestLineErrorDescriptor, HTTP request line parsing errors
DOTHTTP002 | DotHttp | Error | HeaderErrorDescriptor, HTTP header parsing errors
DOTHTTP003 | DotHttp | Error | VariableErrorDescriptor, HTTP variable resolution errors
DOTHTTP004 | DotHttp | Warning | BodyWarningDescriptor, HTTP body parsing warnings
DOTHTTP005 | DotHttp | Warning | UnknownMethodWarningDescriptor, Unknown HTTP method warnings

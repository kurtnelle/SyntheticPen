// The whole executable is Windows-only (Win32 P/Invoke for input injection,
// keyboard hooks, click-through overlays). Suppresses CA1416 throughout the
// assembly so we don't have to annotate every call site.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

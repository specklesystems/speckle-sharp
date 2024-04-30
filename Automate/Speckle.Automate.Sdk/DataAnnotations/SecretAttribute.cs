namespace Speckle.Automate.Sdk.DataAnnotations;

/// <summary>
/// If specified, the given function input will be redacted in all contexts.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class SecretAttribute : Attribute { }

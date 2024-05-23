

#pragma warning disable IDE0005
using System;
using System.Diagnostics;

#nullable enable

namespace Speckle.InterfaceGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [Conditional("CodeGeneration")]
    internal sealed class GenerateAutoInterfaceAttribute : Attribute
    {
        public string? VisibilityModifier { get; init; }
        public string? Name { get; init; }

        public GenerateAutoInterfaceAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
    [Conditional("CodeGeneration")]
    internal sealed class AutoInterfaceIgnoreAttribute : Attribute
    {
    }
}

#pragma warning restore IDE0005

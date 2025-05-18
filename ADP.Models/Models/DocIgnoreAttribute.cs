using System;

namespace ShiftSoftware.ADP.Models;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
public class DocIgnoreAttribute : Attribute
{
}
namespace ShiftSoftware.ADP.Darlastic.Shared;

/// <summary>
/// Lifecycle of a golden identity. IDs are append-only and never reused: a merged identity is
/// <see cref="Redirected"/> (readers follow the redirect, one hop — chains are compressed on
/// write), and an identity whose source records all disappeared is <see cref="Inactive"/>
/// (revived in place if its records return).
/// </summary>
public enum IdentityStatus : byte
{
    Active = 1,
    Redirected = 2,
    Inactive = 3,
}

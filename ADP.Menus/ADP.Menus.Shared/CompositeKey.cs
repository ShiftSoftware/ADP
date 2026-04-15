namespace ShiftSoftware.ADP.Menus.Shared;

public class CompositeKey<TKey1, TKey2>
{
    public TKey1 KeyPart1 { get; }
    public TKey2 KeyPart2 { get; }

    public CompositeKey(TKey1 keyPart1, TKey2 keyPart2)
    {
        KeyPart1 = keyPart1;
        KeyPart2 = keyPart2;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (CompositeKey<TKey1, TKey2>)obj;
        return EqualityComparer<TKey1>.Default.Equals(KeyPart1, other.KeyPart1) &&
               EqualityComparer<TKey2>.Default.Equals(KeyPart2, other.KeyPart2);
    }

    public override int GetHashCode()
    {
        int hashKeyPart1 = KeyPart1 == null ? 0 : EqualityComparer<TKey1>.Default.GetHashCode(KeyPart1);
        int hashKeyPart2 = KeyPart2 == null ? 0 : EqualityComparer<TKey2>.Default.GetHashCode(KeyPart2);

        return hashKeyPart1 ^ hashKeyPart2;
    }

    public override string ToString()
    {
        return $"{KeyPart1}|{KeyPart2}";
    }
}

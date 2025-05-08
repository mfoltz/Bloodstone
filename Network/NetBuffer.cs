namespace Bloodstone.Network;
internal sealed class NetBuffer(string typeKey, int tot)
{
    readonly int _totalParts = tot;
    readonly string _typeKey = typeKey;
    readonly string[] _parts = new string[tot];
    int _received;
    public bool AddPart(int idx, string payload)
    {
        if (_parts[idx] is not null) return false;
        _parts[idx] = payload;
        _received++;
        return _received == _totalParts;
    }
    public string Concat() => string.Concat(_parts);
}

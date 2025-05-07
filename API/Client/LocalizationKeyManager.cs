using Stunlock.Core;
using Stunlock.Localization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Guid = Il2CppSystem.Guid;

namespace Bloodstone.API.Client;
public static class LocalizationKeyManager
{
    const string KEYBINDS_HEADER = MyPluginInfo.PLUGIN_NAME;
    public static LocalizationKey _sectionHeader;

    // public static IReadOnlyDictionary<string, LocalizationKey> SectionHeaders => _sectionHeaders;
    // static readonly Dictionary<string, LocalizationKey> _sectionHeaders = [];
    public static IReadOnlyDictionary<AssetGuid, string> AssetGuids => _assetGuids;
    static readonly Dictionary<AssetGuid, string> _assetGuids = [];
    public static void LocalizeText()
    {
        _sectionHeader = GetLocalizationKey(KEYBINDS_HEADER);

        foreach (var keyValuePair in AssetGuids)
        {
            AssetGuid assetGuid = keyValuePair.Key;
            string localizedString = keyValuePair.Value;

            Localization._LocalizedStrings.TryAdd(assetGuid, localizedString);
        }
    }
    static AssetGuid GetAssetGuid(string text)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

        Guid uniqueGuid = new(hashBytes[..16]);
        return AssetGuid.FromGuid(uniqueGuid);
    }
    public static LocalizationKey GetLocalizationKey(string value)
    {
        AssetGuid assetGuid = GetAssetGuid(value);
        _assetGuids.TryAdd(assetGuid, value);

        return new(assetGuid);
    }
}

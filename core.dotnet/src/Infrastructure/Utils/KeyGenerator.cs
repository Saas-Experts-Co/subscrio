namespace Subscrio.Core.Infrastructure.Utils;

/// <summary>
/// Utility for generating unique keys
/// Equivalent to TypeScript uuid.ts generateKey function
/// </summary>
public static class KeyGenerator
{
    /// <summary>
    /// Generate a short, unique key with prefix for external reference
    /// Example: "sub_a1b2c3d4" for subscriptions, "plan_x9y8z7" for plans
    /// </summary>
    public static string GenerateKey(string prefix)
    {
        // Generate a GUID and take first 12 characters (without hyphens) for uniqueness
        var guid = Guid.NewGuid().ToString("N"); // "N" format removes hyphens
        var shortId = guid.Substring(0, 12);
        return $"{prefix}_{shortId}";
    }
}


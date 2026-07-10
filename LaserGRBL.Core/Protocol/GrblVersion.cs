namespace LaserGRBL.Core.Protocol;

public sealed record GrblVersion(int Major, int Minor, char Build = '\0', string? VendorInfo = null, string? VendorVersion = null, bool IsHal = false) : IComparable<GrblVersion>
{
    public bool IsOrtur => VendorInfo?.Contains("Ortur", StringComparison.OrdinalIgnoreCase) == true || VendorInfo?.Contains("Aufero", StringComparison.OrdinalIgnoreCase) == true;

    public bool IsLonger => VendorInfo?.Contains("Longer", StringComparison.OrdinalIgnoreCase) == true || VendorInfo?.Contains("NanoDuo", StringComparison.OrdinalIgnoreCase) == true;

    public int CompareTo(GrblVersion? other)
    {
        if (other is null) return 1;
        var major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor != other.Minor ? Minor.CompareTo(other.Minor) : Build.CompareTo(other.Build);
    }

    public override string ToString() => Build == '\0' ? $"{Major}.{Minor}" : $"{Major}.{Minor}{Build}";
}

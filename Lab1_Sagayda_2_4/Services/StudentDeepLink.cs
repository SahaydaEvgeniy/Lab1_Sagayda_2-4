namespace Lab1_Sagayda_2_4.Services;

public static class StudentDeepLink
{
    public const string Scheme = "studentcabinet";
    public const string Host = "student";

    public static string Create(Guid studentId) => $"{Scheme}://{Host}/{studentId:D}";

    public static bool TryGetStudentId(Uri uri, out Guid studentId)
    {
        studentId = Guid.Empty;
        return uri.IsAbsoluteUri
            && uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals(Host, StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && uri.AbsolutePath.Length == 37
            && Guid.TryParseExact(uri.AbsolutePath[1..], "D", out studentId)
            && studentId != Guid.Empty;
    }
}

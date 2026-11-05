namespace Lab1_Sagayda_2_4.Services;

public interface IStudentCardProtection
{
    Task<string> EncryptAsync(string value, Guid studentId);
    Task<string> DecryptAsync(string? encryptedValue, Guid studentId);
}

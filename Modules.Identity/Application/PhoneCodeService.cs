using System.Security.Cryptography;
using System.Text;

namespace Modules.Identity.Application;

public interface IPhoneCodeService
{
    string GenerateCode();
    string HashCode(string code);
}

public sealed class PhoneCodeService : IPhoneCodeService
{
    public string GenerateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    public string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace Modules.Identity.Application;

public interface IPhoneVerificationService
{
    Task<PhoneVerificationResult> SendCodeAsync(string phoneNumber, CancellationToken ct);
    Task<PhoneVerificationResult> CheckCodeAsync(string phoneNumber, string code, CancellationToken ct);
}

public sealed record PhoneVerificationResult(bool Succeeded, string? Error, string? VerificationSid);

public sealed class TwilioVerifyService : IPhoneVerificationService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _verifyServiceSid;
    private readonly ILogger<TwilioVerifyService> _logger;
    private readonly bool _isConfigured;

    public TwilioVerifyService(IConfiguration configuration, ILogger<TwilioVerifyService> logger)
    {
        _accountSid = configuration["TwilioVerify:AccountSid"] ?? string.Empty;
        _authToken = configuration["TwilioVerify:AuthToken"] ?? string.Empty;
        _verifyServiceSid = configuration["TwilioVerify:VerifyServiceSid"] ?? string.Empty;
        _logger = logger;
        _isConfigured = !string.IsNullOrWhiteSpace(_accountSid)
                        && !string.IsNullOrWhiteSpace(_authToken)
                        && !string.IsNullOrWhiteSpace(_verifyServiceSid);

        if (_isConfigured)
        {
            TwilioClient.Init(_accountSid, _authToken);
        }
    }

    public async Task<PhoneVerificationResult> SendCodeAsync(string phoneNumber, CancellationToken ct)
    {
        if (!_isConfigured)
        {
            return new PhoneVerificationResult(false, "Twilio Verify is not configured.", null);
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return new PhoneVerificationResult(false, "Phone number is required.", null);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            var verification = await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: "sms",
                pathServiceSid: _verifyServiceSid);

            return new PhoneVerificationResult(true, null, verification.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Twilio Verify code.");
            return new PhoneVerificationResult(false, "Failed to send verification code.", null);
        }
    }

    public async Task<PhoneVerificationResult> CheckCodeAsync(string phoneNumber, string code, CancellationToken ct)
    {
        if (!_isConfigured)
        {
            return new PhoneVerificationResult(false, "Twilio Verify is not configured.", null);
        }

        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
        {
            return new PhoneVerificationResult(false, "Phone number and code are required.", null);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            var check = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _verifyServiceSid);

            var approved = string.Equals(check.Status, "approved", StringComparison.OrdinalIgnoreCase);
            return approved
                ? new PhoneVerificationResult(true, null, check.Sid)
                : new PhoneVerificationResult(false, "Invalid or expired code.", check.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Twilio Verify code.");
            return new PhoneVerificationResult(false, "Invalid or expired code.", null);
        }
    }
}

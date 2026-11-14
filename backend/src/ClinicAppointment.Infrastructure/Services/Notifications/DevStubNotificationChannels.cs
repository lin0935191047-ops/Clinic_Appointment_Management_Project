using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services.Notifications;

public class DevStubSmsChannel : INotificationChannel
{
    private readonly ILogger<DevStubSmsChannel> _logger;
    private readonly IIdentifierMaskingService _maskingService;

    public NotificationChannel Channel => NotificationChannel.Sms;
    public bool SimulateFailure { get; set; } = false;
    public string SimulatedErrorMessage { get; set; } = "Lỗi kết nối cổng SMS Gateway nhà mạng (Simulated).";

    public List<NotificationMessage> SentMessages { get; } = new();

    public DevStubSmsChannel(ILogger<DevStubSmsChannel> logger, IIdentifierMaskingService maskingService)
    {
        _logger = logger;
        _maskingService = maskingService;
    }

    public Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var maskedPhone = _maskingService.MaskPhone(message.Recipient);

        if (SimulateFailure)
        {
            _logger.LogWarning("DevStub SMS gửi thất bại tới {MaskedPhone} do SimulateFailure = true. Lỗi: {Error}", 
                maskedPhone, SimulatedErrorMessage);
            return Task.FromResult(NotificationSendResult.Failure(SimulatedErrorMessage));
        }

        lock (SentMessages)
        {
            SentMessages.Add(message);
        }

        var providerMsgId = $"SMS_DEV_{Guid.NewGuid():N}";
        _logger.LogInformation("DevStub SMS gửi thành công tới {MaskedPhone}. MsgId: {MsgId}, Template: {Template}", 
            maskedPhone, providerMsgId, message.TemplateCode);

        return Task.FromResult(NotificationSendResult.Success(providerMsgId, DateTime.UtcNow));
    }
}

public class DevStubEmailChannel : INotificationChannel
{
    private readonly ILogger<DevStubEmailChannel> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;
    public bool SimulateFailure { get; set; } = false;
    public string SimulatedErrorMessage { get; set; } = "Lỗi kết nối máy chủ SMTP Mail (Simulated).";

    public List<NotificationMessage> SentMessages { get; } = new();

    public DevStubEmailChannel(ILogger<DevStubEmailChannel> logger)
    {
        _logger = logger;
    }

    public Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            _logger.LogWarning("DevStub Email gửi thất bại tới {Recipient} do SimulateFailure = true. Lỗi: {Error}", 
                message.Recipient, SimulatedErrorMessage);
            return Task.FromResult(NotificationSendResult.Failure(SimulatedErrorMessage));
        }

        lock (SentMessages)
        {
            SentMessages.Add(message);
        }

        var providerMsgId = $"EMAIL_DEV_{Guid.NewGuid():N}";
        _logger.LogInformation("DevStub Email gửi thành công tới {Recipient}. MsgId: {MsgId}, Subject: {Subject}", 
            message.Recipient, providerMsgId, message.Subject);

        return Task.FromResult(NotificationSendResult.Success(providerMsgId, DateTime.UtcNow));
    }
}

public class DevStubInAppChannel : INotificationChannel
{
    private readonly ILogger<DevStubInAppChannel> _logger;

    public NotificationChannel Channel => NotificationChannel.InApp;
    public bool SimulateFailure { get; set; } = false;
    public string SimulatedErrorMessage { get; set; } = "Lỗi gửi thông báo In-App WebSocket (Simulated).";

    public List<NotificationMessage> SentMessages { get; } = new();

    public DevStubInAppChannel(ILogger<DevStubInAppChannel> logger)
    {
        _logger = logger;
    }

    public Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            _logger.LogWarning("DevStub InApp gửi thất bại tới {Recipient} do SimulateFailure = true. Lỗi: {Error}", 
                message.Recipient, SimulatedErrorMessage);
            return Task.FromResult(NotificationSendResult.Failure(SimulatedErrorMessage));
        }

        lock (SentMessages)
        {
            SentMessages.Add(message);
        }

        var providerMsgId = $"INAPP_DEV_{Guid.NewGuid():N}";
        _logger.LogInformation("DevStub InApp gửi thành công tới {Recipient}. MsgId: {MsgId}", 
            message.Recipient, providerMsgId);

        return Task.FromResult(NotificationSendResult.Success(providerMsgId, DateTime.UtcNow));
    }
}

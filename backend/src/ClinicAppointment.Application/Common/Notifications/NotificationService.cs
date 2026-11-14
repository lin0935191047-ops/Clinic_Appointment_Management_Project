using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Common.Notifications;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly INotificationSafetyGuard _safetyGuard;
    private readonly IPiiEncryptionService? _encryptionService;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly ILogger<NotificationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public NotificationService(
        IApplicationDbContext context,
        IEnumerable<INotificationChannel> channels,
        INotificationSafetyGuard safetyGuard,
        IIdentifierMaskingService maskingService,
        ILogger<NotificationService> logger,
        IPiiEncryptionService? encryptionService = null)
    {
        _context = context;
        _channels = channels;
        _safetyGuard = safetyGuard;
        _maskingService = maskingService;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<Result<NotificationDto>> EnqueueNotificationAsync(
        EnqueueNotificationCommand request, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Recipient))
            return Result<NotificationDto>.Failure("Địa chỉ người nhận không được để trống.", 400);

        if (string.IsNullOrWhiteSpace(request.TemplateCode))
            return Result<NotificationDto>.Failure("Mã mẫu thông báo (TemplateCode) không được để trống.", 400);

        // 1. Kiểm soát an toàn y tế và dữ liệu cá nhân (No diagnosis / identifier in message)
        var msg = new NotificationMessage(
            Recipient: request.Recipient,
            Channel: request.Channel,
            TemplateCode: request.TemplateCode,
            Content: request.Content,
            Subject: request.Subject,
            Payload: request.Payload,
            AppointmentId: request.AppointmentId,
            PatientId: request.PatientId
        );

        try
        {
            _safetyGuard.ValidateMessageSafety(msg);
        }
        catch (NotificationSafetyViolationException ex)
        {
            _logger.LogWarning("Thông báo bị từ chối do vi phạm an toàn y tế: {Message}", ex.Message);
            return Result<NotificationDto>.Failure(ex.Message, 400);
        }

        // 2. Che giấu mặt nạ địa chỉ nhận và mã hóa bảo mật
        string maskedRecipient = MaskRecipient(request.Recipient, request.Channel);
        byte[]? ciphertext = null;
        if (_encryptionService != null)
        {
            try
            {
                ciphertext = _encryptionService.Encrypt(request.Recipient);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể mã hóa recipient ciphertext: {Message}", ex.Message);
            }
        }

        // 3. Chuẩn bị payload JSON an toàn
        var payloadDict = request.Payload != null 
            ? new Dictionary<string, object>(request.Payload) 
            : new Dictionary<string, object>();

        if (!payloadDict.ContainsKey("Content"))
            payloadDict["Content"] = request.Content;

        if (!string.IsNullOrWhiteSpace(request.Subject) && !payloadDict.ContainsKey("Subject"))
            payloadDict["Subject"] = request.Subject;

        string payloadJson = JsonSerializer.Serialize(payloadDict, JsonOptions);

        // 4. Khởi tạo thực thể Notification
        var notification = Notification.Create(
            channel: request.Channel,
            templateCode: request.TemplateCode,
            recipientMasked: maskedRecipient,
            recipientCiphertext: ciphertext,
            payloadJson: payloadJson,
            appointmentId: request.AppointmentId,
            patientId: request.PatientId,
            maxRetries: request.MaxRetries,
            createdAtUtc: DateTime.UtcNow
        );

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Đã xếp hàng thông báo {NotificationId} cho kênh {Channel} tới {MaskedRecipient} (Template: {TemplateCode}).",
            notification.Id, notification.Channel, notification.RecipientMasked, notification.TemplateCode);

        return Result<NotificationDto>.Success(MapToDto(notification));
    }

    public async Task<NotificationSendResult> DispatchNotificationAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
            return NotificationSendResult.Failure("Không tìm thấy thông báo trong hàng đợi.");

        if (notification.Status == NotificationStatus.Sent)
            return NotificationSendResult.Success(notification.ProviderMessageId ?? "ALREADY_SENT", notification.SentAt);

        // Tìm kênh gửi tương ứng
        var channel = _channels.FirstOrDefault(c => c.Channel == notification.Channel);
        if (channel == null)
        {
            var err = $"Không tìm thấy Channel Adapter cho kênh {notification.Channel}.";
            notification.MarkFailed(err);
            await _context.SaveChangesAsync(cancellationToken);
            return NotificationSendResult.Failure(err);
        }

        // Giải mã người nhận
        string rawRecipient = notification.RecipientMasked ?? string.Empty;
        if (_encryptionService != null && notification.RecipientCiphertext != null)
        {
            try
            {
                rawRecipient = _encryptionService.Decrypt(notification.RecipientCiphertext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể giải mã RecipientCiphertext cho thông báo {Id}: {Error}", notification.Id, ex.Message);
            }
        }

        // Trích xuất nội dung từ PayloadJson
        string content = string.Empty;
        string? subject = null;
        Dictionary<string, object>? payload = null;

        if (!string.IsNullOrWhiteSpace(notification.PayloadJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.PayloadJson, JsonOptions);
                if (payload != null)
                {
                    if (payload.TryGetValue("Content", out var cVal))
                        content = cVal?.ToString() ?? string.Empty;
                    if (payload.TryGetValue("Subject", out var sVal))
                        subject = sVal?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể phân tích PayloadJson cho thông báo {Id}: {Error}", notification.Id, ex.Message);
            }
        }

        var message = new NotificationMessage(
            Recipient: rawRecipient,
            Channel: notification.Channel,
            TemplateCode: notification.TemplateCode,
            Content: content,
            Subject: subject,
            Payload: payload,
            AppointmentId: notification.AppointmentId,
            PatientId: notification.PatientId
        );

        // Gửi qua kênh được decoupled
        NotificationSendResult sendResult;
        try
        {
            sendResult = await channel.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi ngoại lệ khi gửi thông báo {Id} qua kênh {Channel}.", notification.Id, notification.Channel);
            sendResult = NotificationSendResult.Failure($"Ngoại lệ provider: {ex.Message}");
        }

        if (sendResult.IsSuccess)
        {
            notification.MarkSent(sendResult.ProviderMessageId ?? Guid.NewGuid().ToString(), sendResult.SentAt);
            _logger.LogInformation("Gửi thông báo {Id} thành công qua {Channel}. ProviderMsgId: {MsgId}", 
                notification.Id, notification.Channel, notification.ProviderMessageId);
        }
        else
        {
            notification.MarkFailed(sendResult.ErrorMessage ?? "Lỗi không xác định khi gửi thông báo.");
            _logger.LogWarning("Gửi thông báo {Id} qua {Channel} thất bại: {Reason}. (Retry {Count}/{Max})", 
                notification.Id, notification.Channel, notification.FailureReason, notification.RetryCount, notification.MaxRetries);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return sendResult;
    }

    public async Task<int> DispatchPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default)
    {
        var pendingList = await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        int successCount = 0;
        foreach (var notification in pendingList)
        {
            var result = await DispatchNotificationAsync(notification.Id, cancellationToken);
            if (result.IsSuccess)
                successCount++;
        }

        return successCount;
    }

    public async Task<Result<NotificationDto>> RetryNotificationAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
            return Result<NotificationDto>.Failure("Không tìm thấy thông báo.", 404);

        if (!notification.CanRetry)
        {
            return Result<NotificationDto>.Failure(
                $"Thông báo không thể thử lại. Trạng thái: {notification.Status}, Số lần thử: {notification.RetryCount}/{notification.MaxRetries}.", 
                400);
        }

        notification.ResetForRetry();
        await _context.SaveChangesAsync(cancellationToken);

        // Điều phối gửi lại
        await DispatchNotificationAsync(notification.Id, cancellationToken);

        var updated = await _context.Notifications
            .FirstAsync(n => n.Id == notificationId, cancellationToken);

        return Result<NotificationDto>.Success(MapToDto(updated));
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsByAppointmentIdAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default)
    {
        var list = await _context.Notifications
            .Where(n => n.AppointmentId == appointmentId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<Result<NotificationDto>> GetNotificationByIdAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
            return Result<NotificationDto>.Failure("Không tìm thấy thông báo.", 404);

        return Result<NotificationDto>.Success(MapToDto(notification));
    }

    private string MaskRecipient(string recipient, NotificationChannel channel)
    {
        if (channel == NotificationChannel.Sms)
        {
            return _maskingService.MaskPhone(recipient);
        }

        if (channel == NotificationChannel.Email)
        {
            var parts = recipient.Split('@');
            if (parts.Length == 2 && parts[0].Length > 0)
            {
                var user = parts[0];
                var domain = parts[1];
                var maskedUser = user.Length <= 2 ? user[0] + "***" : user.Substring(0, 2) + "***";
                return $"{maskedUser}@{domain}";
            }
            return "***@***";
        }

        return recipient.Length > 4 ? recipient.Substring(0, 4) + "***" : "***";
    }

    private static NotificationDto MapToDto(Notification n) => new(
        Id: n.Id,
        Channel: n.Channel,
        TemplateCode: n.TemplateCode,
        RecipientMasked: n.RecipientMasked,
        Status: n.Status,
        ProviderMessageId: n.ProviderMessageId,
        SentAt: n.SentAt,
        FailureReason: n.FailureReason,
        RetryCount: n.RetryCount,
        MaxRetries: n.MaxRetries,
        AppointmentId: n.AppointmentId,
        PatientId: n.PatientId,
        CreatedAt: n.CreatedAt,
        UpdatedAt: n.UpdatedAt
    );
}

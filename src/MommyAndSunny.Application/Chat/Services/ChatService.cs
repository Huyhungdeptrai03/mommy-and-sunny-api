using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Chat.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Chat.Services;

public sealed class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;

    public ChatService(IApplicationDbContext context) => _context = context;

    public async Task<ConversationDto> GetOrCreateForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == customerId && user.Role == UserRole.Customer, cancellationToken)
            ?? throw new UnauthorizedException("Chỉ tài khoản khách hàng mới có thể mở cuộc trò chuyện");

        var conversation = await ConversationQuery()
            .FirstOrDefaultAsync(item => item.CustomerId == customerId && item.Status != ConversationStatus.Closed,
                cancellationToken);
        if (conversation is not null)
        {
            return await MapConversationAsync(conversation, customerId, cancellationToken);
        }

        conversation = new Conversation
        {
            CustomerId = customerId,
            Customer = customer,
            Status = ConversationStatus.Waiting,
            LastMessageAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapConversationAsync(conversation, customerId, cancellationToken);
    }

    public async Task<ConversationDto> GetConversationAsync(
        Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await GetAuthorizedConversationAsync(conversationId, userId, cancellationToken);
        return await MapConversationAsync(conversation, userId, cancellationToken);
    }

    public async Task<PagedResult<ConversationDto>> GetAdminConversationsAsync(
        Guid adminId, ConversationFilterRequest filter, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId, cancellationToken);
        var query = ConversationQuery();
        if (filter.Status.HasValue) query = query.Where(item => item.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(item => item.Customer.FullName.ToLower().Contains(search) ||
                                        item.Customer.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var conversations = await query.OrderByDescending(item => item.LastMessageAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        var items = new List<ConversationDto>(conversations.Count);
        foreach (var conversation in conversations)
        {
            items.Add(await MapConversationAsync(conversation, adminId, cancellationToken));
        }

        return new PagedResult<ConversationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<ChatMessageDto>> GetMessagesAsync(
        Guid conversationId, Guid userId, ChatMessageFilterRequest filter, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedConversationAsync(conversationId, userId, cancellationToken);
        var query = _context.ChatMessages.Where(message => message.ConversationId == conversationId);
        var totalCount = await query.CountAsync(cancellationToken);
        var messages = await query.Include(message => message.Sender)
            .OrderByDescending(message => message.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        messages.Reverse();
        return new PagedResult<ChatMessageDto>
        {
            Items = messages.Select(MapMessage).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        Guid conversationId, Guid senderId, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var sender = await _context.Users.FirstOrDefaultAsync(user => user.Id == senderId, cancellationToken)
            ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
        var conversation = await ConversationQuery().FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("cuộc trò chuyện", conversationId);

        if (conversation.Status == ConversationStatus.Closed)
            throw new ConflictException("Cuộc trò chuyện đã đóng");
        if (sender.Role == UserRole.Customer && conversation.CustomerId != senderId)
            throw new UnauthorizedException("Bạn không có quyền truy cập cuộc trò chuyện này");
        if (sender.Role == UserRole.Admin)
        {
            if (conversation.AssignedAdminId.HasValue && conversation.AssignedAdminId != senderId)
                throw new ConflictException("Cuộc trò chuyện đang do nhân viên khác phụ trách");
            conversation.AssignedAdminId = senderId;
            conversation.Status = ConversationStatus.Active;
        }

        var tooFast = await _context.ChatMessages.AnyAsync(message =>
            message.ConversationId == conversationId && message.SenderId == senderId &&
            message.CreatedAt > DateTime.UtcNow.AddSeconds(-1), cancellationToken);
        if (tooFast) throw new ConflictException("Bạn đang gửi tin nhắn quá nhanh");

        var content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim();
        var productTags = await ResolveProductTagsAsync(content, cancellationToken);
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Sender = sender,
            Content = content,
            ProductTagsJson = productTags.Count == 0 ? null : JsonSerializer.Serialize(productTags),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
        };
        conversation.LastMessageAt = DateTime.UtcNow;
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return MapMessage(message);
    }

    public async Task<ConversationDto> AssignToMeAsync(
        Guid conversationId, Guid adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId, cancellationToken);
        var conversation = await ConversationQuery().FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("cuộc trò chuyện", conversationId);
        if (conversation.Status == ConversationStatus.Closed) throw new ConflictException("Cuộc trò chuyện đã đóng");
        if (conversation.AssignedAdminId.HasValue && conversation.AssignedAdminId != adminId)
            throw new ConflictException("Cuộc trò chuyện đang do nhân viên khác phụ trách");
        conversation.AssignedAdminId = adminId;
        conversation.Status = ConversationStatus.Active;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapConversationAsync(conversation, adminId, cancellationToken);
    }

    public async Task CloseAsync(Guid conversationId, Guid adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId, cancellationToken);
        var conversation = await _context.Conversations.FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("cuộc trò chuyện", conversationId);
        if (conversation.AssignedAdminId.HasValue && conversation.AssignedAdminId != adminId)
            throw new ConflictException("Cuộc trò chuyện đang do nhân viên khác phụ trách");
        conversation.AssignedAdminId ??= adminId;
        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> MarkAsReadAsync(Guid conversationId, Guid readerId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedConversationAsync(conversationId, readerId, cancellationToken);
        var unread = await _context.ChatMessages
            .Where(message => message.ConversationId == conversationId && message.SenderId != readerId && !message.IsRead)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var message in unread)
        {
            message.IsRead = true;
            message.ReadAt = now;
        }
        if (unread.Count > 0) await _context.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task<bool> CanAccessAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return false;
        return await _context.Conversations.AsNoTracking().AnyAsync(
            item => item.Id == conversationId && (user.Role == UserRole.Admin || item.CustomerId == userId),
            cancellationToken);
    }

    private IQueryable<Conversation> ConversationQuery() => _context.Conversations
        .Include(item => item.Customer).Include(item => item.AssignedAdmin);

    private async Task<Conversation> GetAuthorizedConversationAsync(
        Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
        var conversation = await ConversationQuery().FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("cuộc trò chuyện", conversationId);
        if (user.Role != UserRole.Admin && conversation.CustomerId != userId)
            throw new UnauthorizedException("Bạn không có quyền truy cập cuộc trò chuyện này");
        return conversation;
    }

    private async Task RequireAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AsNoTracking().AnyAsync(
                user => user.Id == userId && user.Role == UserRole.Admin, cancellationToken))
            throw new UnauthorizedException("Yêu cầu quyền quản trị viên");
    }

    private async Task<ConversationDto> MapConversationAsync(
        Conversation conversation, Guid viewerId, CancellationToken cancellationToken)
    {
        var unreadCount = await _context.ChatMessages.CountAsync(
            message => message.ConversationId == conversation.Id && message.SenderId != viewerId && !message.IsRead,
            cancellationToken);
        return new ConversationDto
        {
            Id = conversation.Id,
            CustomerId = conversation.CustomerId,
            CustomerName = conversation.Customer.FullName,
            CustomerEmail = conversation.Customer.Email,
            AssignedAdminId = conversation.AssignedAdminId,
            AssignedAdminName = conversation.AssignedAdmin?.FullName,
            Status = conversation.Status.ToString(),
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount
        };
    }

    private async Task<List<ChatProductTagDto>> ResolveProductTagsAsync(
        string? content, CancellationToken cancellationToken)
    {
        var tags = new List<ChatProductTagDto>();
        if (content is null || !content.Contains('#')) return tags;

        var products = await _context.Products.AsNoTracking()
            .Where(product => product.Status == ProductStatus.Published && product.Name != "")
            .Select(product => new { product.Id, product.Name, product.Slug })
            .ToListAsync(cancellationToken);
        // Prefer the longest name when catalog names share a prefix.
        var candidates = products.OrderByDescending(product => product.Name.Length)
            .ThenBy(product => product.Id).ToList();
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] != '#' ||
                (index > 0 && (char.IsLetterOrDigit(content[index - 1]) || content[index - 1] == '_')))
                continue;

            var product = candidates.FirstOrDefault(candidate =>
                content.AsSpan(index + 1).StartsWith(candidate.Name.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                (index + 1 + candidate.Name.Length == content.Length ||
                 (!char.IsLetterOrDigit(content[index + 1 + candidate.Name.Length]) &&
                  content[index + 1 + candidate.Name.Length] != '_')));
            if (product is null) continue;
            var length = product.Name.Length + 1;
            tags.Add(new ChatProductTagDto
            {
                ProductId = product.Id, ProductName = product.Name, Slug = product.Slug,
                Start = index, Length = length
            });
            index += length - 1;
        }
        return tags;
    }

    private static ChatMessageDto MapMessage(ChatMessage message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        SenderName = message.Sender.FullName,
        SenderRole = message.Sender.Role.ToString(),
        Content = message.Content,
        ImageUrl = message.ImageUrl,
        ProductTags = message.ProductTagsJson is null
            ? [] : JsonSerializer.Deserialize<List<ChatProductTagDto>>(message.ProductTagsJson) ?? [],
        IsRead = message.IsRead,
        ReadAt = message.ReadAt,
        CreatedAt = message.CreatedAt
    };
}

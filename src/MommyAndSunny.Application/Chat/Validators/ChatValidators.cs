using FluentValidation;
using MommyAndSunny.Application.Chat.DTOs;

namespace MommyAndSunny.Application.Chat.Validators;

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("Tin nhắn phải có nội dung hoặc hình ảnh");
        RuleFor(x => x.Content).MaximumLength(2_000).WithMessage("Tin nhắn tối đa 2000 ký tự");
        RuleFor(x => x.ImageUrl)
            .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("URL hình ảnh không hợp lệ");
    }
}

public sealed class ConversationFilterRequestValidator : AbstractValidator<ConversationFilterRequest>
{
    public ConversationFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class ChatMessageFilterRequestValidator : AbstractValidator<ChatMessageFilterRequest>
{
    public ChatMessageFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

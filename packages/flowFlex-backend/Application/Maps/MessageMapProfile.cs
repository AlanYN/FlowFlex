using System.Text.Json;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Message AutoMapper Profile
    /// </summary>
    public class MessageMapProfile : Profile
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public MessageMapProfile()
        {
            // Message Entity to MessageListItemDto
            CreateMap<Message, MessageListItemDto>()
                .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => ParseLabels(src.Labels)));

            // Message Entity to MessageDetailDto
            CreateMap<Message, MessageDetailDto>()
                .ForMember(dest => dest.Labels, opt => opt.MapFrom(src => ParseLabels(src.Labels)))
                .ForMember(dest => dest.Recipients, opt => opt.MapFrom(src => ParseRecipients(src.Recipients)))
                .ForMember(dest => dest.CcRecipients, opt => opt.MapFrom(src => ParseRecipients(src.CcRecipients)))
                .ForMember(dest => dest.BccRecipients, opt => opt.MapFrom(src => ParseRecipients(src.BccRecipients)))
                .ForMember(dest => dest.Attachments, opt => opt.Ignore()); // Attachments loaded separately

            // MessageCreateDto to Message Entity
            CreateMap<MessageCreateDto, Message>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.AppCode, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Recipients, opt => opt.MapFrom(src => SerializeRecipients(src.Recipients)))
                .ForMember(dest => dest.CcRecipients, opt => opt.MapFrom(src => SerializeRecipients(src.CcRecipients)))
                .ForMember(dest => dest.BccRecipients, opt => opt.MapFrom(src => SerializeRecipients(src.BccRecipients)))
                .ForMember(dest => dest.BodyPreview, opt => opt.MapFrom(src => GetBodyPreview(src.Body)))
                .ForMember(dest => dest.IsDraft, opt => opt.MapFrom(src => src.SaveAsDraft))
                .ForMember(dest => dest.Folder, opt => opt.MapFrom(src => src.SaveAsDraft ? "Archive" : "Sent"));

            // MessageAttachment Entity to MessageAttachmentDto
            CreateMap<MessageAttachment, MessageAttachmentDto>();
        }

        private static List<string> ParseLabels(string? labelsJson)
        {
            if (string.IsNullOrEmpty(labelsJson)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(labelsJson, _jsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static List<RecipientDto> ParseRecipients(string? recipientsJson)
        {
            if (string.IsNullOrEmpty(recipientsJson)) return new List<RecipientDto>();
            try
            {
                return JsonSerializer.Deserialize<List<RecipientDto>>(recipientsJson, _jsonOptions) ?? new List<RecipientDto>();
            }
            catch
            {
                return new List<RecipientDto>();
            }
        }

        private static string SerializeRecipients(List<RecipientDto>? recipients)
        {
            if (recipients == null || !recipients.Any()) return "[]";
            return JsonSerializer.Serialize(recipients);
        }

        private static string GetBodyPreview(string? body)
        {
            if (string.IsNullOrEmpty(body)) return string.Empty;

            // Strip HTML tags for preview
            var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
        }
    }
}

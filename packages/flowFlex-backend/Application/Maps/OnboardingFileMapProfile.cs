using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// OnboardingFile 映射配置
    /// </summary>
    public class OnboardingFileMapProfile : Profile
    {
        public OnboardingFileMapProfile()
        {
            // Entity -> OutputDto
            CreateMap<OnboardingFile, OnboardingFileOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.OriginalFileName))
                .ForMember(dest => dest.StoredFileName, opt => opt.MapFrom(src => src.StoredFileName))
                .ForMember(dest => dest.FileExtension, opt => opt.MapFrom(src => src.FileExtension))
                .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.AccessUrl, opt => opt.MapFrom(src => src.AccessUrl))
                .ForMember(dest => dest.UploadedById, opt => opt.MapFrom(src => src.UploadedById))
                .ForMember(dest => dest.UploadedByName, opt => opt.MapFrom(src => src.UploadedByName))
                .ForMember(dest => dest.UploadedDate, opt => opt.MapFrom(src => src.UploadedDate))
                .ForMember(dest => dest.LastModifiedDate, opt => opt.MapFrom(src => src.LastModifiedDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.IsExternalImport, opt => opt.MapFrom(src => src.IsExternalImport))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source))
                // 忽略DTO中有但实体中没有的属性
                .ForMember(dest => dest.StageName, opt => opt.Ignore())
                .ForMember(dest => dest.FileSizeFormatted, opt => opt.Ignore())
                .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore());

            // InputDto -> Entity 映射 - 只映射基本属性，忽略InputDto中不存在的属�?
            CreateMap<OnboardingFileInputDto, OnboardingFile>()
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category ?? "Document"))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"))
                // 忽略所有其他属�?
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AttachmentId, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalFileName, opt => opt.Ignore())
                .ForMember(dest => dest.StoredFileName, opt => opt.Ignore())
                .ForMember(dest => dest.FileExtension, opt => opt.Ignore())
                .ForMember(dest => dest.FileSize, opt => opt.Ignore())
                .ForMember(dest => dest.ContentType, opt => opt.Ignore())
                .ForMember(dest => dest.AccessUrl, opt => opt.Ignore())
                .ForMember(dest => dest.StoragePath, opt => opt.Ignore())
                .ForMember(dest => dest.UploadedById, opt => opt.Ignore())
                .ForMember(dest => dest.UploadedByName, opt => opt.Ignore())
                .ForMember(dest => dest.UploadedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Version, opt => opt.Ignore())
                .ForMember(dest => dest.FileHash, opt => opt.Ignore())
                .ForMember(dest => dest.SortOrder, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedProperties, opt => opt.Ignore());
        }
    }
}

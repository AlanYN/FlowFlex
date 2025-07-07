using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Questionnaire Section service implementation
    /// </summary>
    public class QuestionnaireSectionService : IQuestionnaireSectionService, IScopedService
    {
        private readonly IQuestionnaireSectionRepository _sectionRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;

        public QuestionnaireSectionService(
            IQuestionnaireSectionRepository sectionRepository,
            IQuestionnaireRepository questionnaireRepository,
            IMapper mapper,
            UserContext userContext)
        {
            _sectionRepository = sectionRepository;
            _questionnaireRepository = questionnaireRepository;
            _mapper = mapper;
            _userContext = userContext;
        }

        public async Task<List<QuestionnaireSectionDto>> GetByQuestionnaireIdAsync(long questionnaireId)
        {
            var sections = await _sectionRepository.GetOrderedByQuestionnaireIdAsync(questionnaireId);
            return _mapper.Map<List<QuestionnaireSectionDto>>(sections);
        }

        public async Task<QuestionnaireSectionDto> GetByIdAsync(long id)
        {
            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Section with ID {id} not found");
            }
            return _mapper.Map<QuestionnaireSectionDto>(section);
        }

        public async Task<long> CreateAsync(QuestionnaireSectionInputDto input)
        {
            // Validate questionnaire exists
            var questionnaire = await _questionnaireRepository.GetByIdAsync(input.QuestionnaireId);
            if (questionnaire == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {input.QuestionnaireId} not found");
            }

            // Validate title uniqueness
            if (await _sectionRepository.ExistsTitleInQuestionnaireAsync(input.QuestionnaireId, input.Title))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Section title '{input.Title}' already exists in this questionnaire");
            }

            var section = _mapper.Map<QuestionnaireSection>(input);

            // If no Order specified, get next Order value
            if (section.Order <= 0)
            {
                section.Order = await _sectionRepository.GetNextOrderAsync(input.QuestionnaireId);
            }

            // Initialize create information with proper ID and timestamps
            section.InitCreateInfo(_userContext);

            await _sectionRepository.InsertAsync(section);
            return section.Id;
        }

        public async Task<bool> UpdateAsync(long id, QuestionnaireSectionInputDto input)
        {
            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Section with ID {id} not found");
            }

            // Validate title uniqueness (exclude current record)
            if (await _sectionRepository.ExistsTitleInQuestionnaireAsync(section.QuestionnaireId, input.Title, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Section title '{input.Title}' already exists in this questionnaire");
            }

            // Update fields
            section.Title = input.Title;
            section.Description = input.Description;
            section.IsActive = input.IsActive;
            section.Icon = input.Icon;
            section.Color = input.Color;
            section.IsCollapsible = input.IsCollapsible;
            section.IsExpanded = input.IsExpanded;

            // Initialize update information with proper timestamps
            section.InitUpdateInfo(_userContext);

            return await _sectionRepository.UpdateAsync(section);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                return false;
            }

            return await _sectionRepository.DeleteAsync(section);
        }

        public async Task<bool> UpdateOrderAsync(long id, int order)
        {
            return await _sectionRepository.UpdateOrderAsync(id, order);
        }

        public async Task<bool> BatchUpdateOrderAsync(List<(long SectionId, int Order)> orderUpdates)
        {
            return await _sectionRepository.BatchUpdateOrderAsync(orderUpdates);
        }
    }
}
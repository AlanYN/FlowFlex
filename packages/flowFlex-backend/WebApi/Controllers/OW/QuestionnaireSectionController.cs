using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.IServices.OW;

using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Questionnaire Section management API
    /// </summary>

    [ApiController]

    [Route("ow/questionnaire-sections/v{version:apiVersion}")]
    [Display(Name = "questionnaire-sections")]

    public class QuestionnaireSectionController : Controllers.ControllerBase
    {
        private readonly IQuestionnaireSectionService _sectionService;

        public QuestionnaireSectionController(IQuestionnaireSectionService sectionService)
        {
            _sectionService = sectionService;
        }

        /// <summary>
        /// Get sections by questionnaire ID
        /// </summary>
        [HttpGet("questionnaire/{questionnaireId}")]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireSectionDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByQuestionnaireId(long questionnaireId)
        {
            var data = await _sectionService.GetByQuestionnaireIdAsync(questionnaireId);
            return Success(data);
        }

        /// <summary>
        /// Get section by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<QuestionnaireSectionDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _sectionService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Create section
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] QuestionnaireSectionInputDto input)
        {
            var id = await _sectionService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update section
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] QuestionnaireSectionInputDto input)
        {
            var result = await _sectionService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete section
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _sectionService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Update section order
        /// </summary>
        [HttpPut("{id}/order")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateOrder(long id, [FromBody] UpdateOrderRequest request)
        {
            var result = await _sectionService.UpdateOrderAsync(id, request.Order);
            return Success(result);
        }

        /// <summary>
        /// Batch update section orders
        /// </summary>
        [HttpPut("batch-order")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchUpdateOrder([FromBody] BatchUpdateOrderRequest request)
        {
            var result = await _sectionService.BatchUpdateOrderAsync(request.OrderUpdates);
            return Success(result);
        }
    }

    /// <summary>
    /// Update order request
    /// </summary>
    public class UpdateOrderRequest
    {
        /// <summary>
        /// New order
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Batch update order request
    /// </summary>
    public class BatchUpdateOrderRequest
    {
        /// <summary>
        /// Order updates
        /// </summary>
        public List<(long SectionId, int Order)> OrderUpdates { get; set; } = new List<(long, int)>();
    }
}

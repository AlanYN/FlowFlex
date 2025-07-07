using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.WebApi.Converters;

using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Questionnaire management API
    /// </summary>
 
    [ApiController]
 
    [Route("ow/questionnaires/v{version:apiVersion}")]
    [Display(Name = "questionnaire")]
   
    public class QuestionnaireController : Controllers.ControllerBase
    {
        private readonly IQuestionnaireService _questionnaireService;

        public QuestionnaireController(IQuestionnaireService questionnaireService)
        {
            _questionnaireService = questionnaireService;
        }

        /// <summary>
        /// Create questionnaire
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] QuestionnaireInputDto input)
        {
            var id = await _questionnaireService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update questionnaire
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id)
        {
            // Add debug logging
            Console.WriteLine($"[DEBUG] Update questionnaire - ID: {id}");
            
            QuestionnaireInputDto input = null;
            
            try
            {
                // Read raw body directly
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                Console.WriteLine($"[DEBUG] Raw request body: {rawBody}");
                
                // Try to extract the first JSON object if there are multiple separated by &
                if (!string.IsNullOrEmpty(rawBody))
                {
                    var firstJsonPart = rawBody.Split('&')[0];
                    Console.WriteLine($"[DEBUG] First JSON part: {firstJsonPart}");
                    
                    // Try to deserialize the first part
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };
                    options.Converters.Add(new NullableLongConverter());
                    
                    input = JsonSerializer.Deserialize<QuestionnaireInputDto>(firstJsonPart, options);
                    Console.WriteLine($"[DEBUG] Parsed input successfully: {input != null}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error parsing raw body: {ex.Message}");
            }
            
            if (input != null)
            {
                Console.WriteLine($"[DEBUG] Input Name: {input.Name}");
                Console.WriteLine($"[DEBUG] Input Description: {input.Description}");
                Console.WriteLine($"[DEBUG] StructureJson length: {input.StructureJson?.Length ?? 0}");
            }
            
            var result = await _questionnaireService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete questionnaire (with confirmation)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _questionnaireService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get questionnaire by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<QuestionnaireOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _questionnaireService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get questionnaire list by category
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList([FromQuery] string category = null)
        {
            var data = await _questionnaireService.GetListAsync(category);
            return Success(data);
        }

        /// <summary>
        /// Get questionnaires by stage ID
        /// </summary>
        [HttpGet("by-stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageId(long stageId)
        {
            var data = await _questionnaireService.GetByStageIdAsync(stageId);
            return Success(data);
        }

        /// <summary>
        /// Query questionnaire (paged)
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PagedResult<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] QuestionnaireQueryRequest query)
        {
            var data = await _questionnaireService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Duplicate questionnaire
        /// </summary>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateQuestionnaireInputDto input)
        {
            var newId = await _questionnaireService.DuplicateAsync(id, input);
            return Success(newId);
        }

        /// <summary>
        /// Preview questionnaire
        /// </summary>
        [HttpGet("{id}/preview")]
        [ProducesResponseType<SuccessResponse<QuestionnaireOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Preview(long id)
        {
            var data = await _questionnaireService.PreviewAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Publish questionnaire
        /// </summary>
        [HttpPost("{id}/publish")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Publish(long id)
        {
            var result = await _questionnaireService.PublishAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Archive questionnaire
        /// </summary>
        [HttpPost("{id}/archive")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Archive(long id)
        {
            var result = await _questionnaireService.ArchiveAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get questionnaire templates
        /// </summary>
        [HttpGet("templates")]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTemplates()
        {
            var data = await _questionnaireService.GetTemplatesAsync();
            return Success(data);
        }

        /// <summary>
        /// Create questionnaire from template
        /// </summary>
        [HttpPost("templates/{templateId}/create")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateFromTemplate(long templateId, [FromBody] QuestionnaireInputDto input)
        {
            var id = await _questionnaireService.CreateFromTemplateAsync(templateId, input);
            return Success(id);
        }

        /// <summary>
        /// Validate questionnaire structure
        /// </summary>
        [HttpPost("{id}/validate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateStructure(long id)
        {
            var result = await _questionnaireService.ValidateStructureAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        [HttpPost("{id}/update-statistics")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatistics(long id)
        {
            var result = await _questionnaireService.UpdateStatisticsAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Batch get questionnaires by stage IDs
        /// </summary>
        [HttpPost("batch/by-stages")]
        [ProducesResponseType<SuccessResponse<BatchStageQuestionnaireResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageIdsBatch([FromBody] BatchStageQuestionnaireRequest request)
        {
            var result = await _questionnaireService.GetByStageIdsBatchAsync(request);
            return Success(result);
        }
    }
}


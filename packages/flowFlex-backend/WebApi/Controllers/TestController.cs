using Microsoft.AspNetCore.Mvc;
using FlowFlex.Domain.Entities.Base;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared.JsonConverters;

namespace FlowFlex.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("test-long-conversion")]
    public IActionResult TestLongConversion()
    {
        var testData = new TestEntity
        {
            Id = 1942076577294716928,
            Name = "Test Entity",
            CreateUserId = 123456789,
            ModifyUserId = 987654321
        };

        return Ok(testData);
    }
}

public class TestEntity : IdEntityBase
{
    public string Name { get; set; }
    
    [JsonConverter(typeof(LongToStringConverter))]
    public long CreateUserId { get; set; }
    
    [JsonConverter(typeof(LongToStringConverter))]
    public long ModifyUserId { get; set; }
} 

using Application.Contracts.IServices.Action;
using Infrastructure.CodeGenerator;
using Item.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Action
{
    public class ActionCodeGeneratorService : CodeGeneratorAbstract, IActionCodeGeneratorService
    {
        private readonly ILogger<ActionCodeGeneratorService> _logger;

        public ActionCodeGeneratorService(
            IRedisService redisService,
            IConfiguration configuration,
            ILogger<ActionCodeGeneratorService> logger) : base(redisService, configuration)
        {
            _logger = logger;
        }

        protected override string CodeSeparator => "-";

        protected override int CodeMaxNum => 10;

        protected override int MinCodeLength => 8;

        protected override string CodePrefix { get; set; } = "ACT";

        public async Task<string> GeneratorActionCodeAsync()
        {
            var code = await GenerateCodeAsync();
            return code;
        }
    }
}

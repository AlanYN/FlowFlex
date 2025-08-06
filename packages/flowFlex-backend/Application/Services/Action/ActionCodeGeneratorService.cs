using Application.Contracts.IServices.Action;
using Infrastructure.CodeGenerator;
using Item.Redis;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Action
{
    public class ActionCodeGeneratorService : CodeGeneratorAbstract, IActionCodeGeneratorService
    {
        public ActionCodeGeneratorService(IRedisService redisService,
            IConfiguration configuration) : base(redisService, configuration)
        { }

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

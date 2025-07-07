using System;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public interface IDynamicDataModel
{
    long Id { get; set; }

    int ModuleId { get; }

    bool DynamicDataFullFinish { get; set; }

    IServiceProvider Service { get; set; }

    void DynamicFullFinish();
}

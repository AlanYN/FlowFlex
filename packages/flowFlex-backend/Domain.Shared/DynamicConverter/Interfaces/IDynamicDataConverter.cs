using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter.Interfaces;

public interface IDynamicDataConverter
{
    object Convert(object value);

    object Get(IDynamicValue dynamicValue);

    void Set(IDynamicValue dynamicValue, object value);
}

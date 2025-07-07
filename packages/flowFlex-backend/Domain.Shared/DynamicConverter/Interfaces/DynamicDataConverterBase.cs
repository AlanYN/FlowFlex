using FlowFlex.Domain.Shared.Models.DynamicData;

namespace FlowFlex.Domain.Shared.DynamicConverter.Interfaces;

public abstract class DynamicDataConverterBase<T> : IDynamicDataConverter
{
    public object Convert(object value) => Converter(value);

    public object Get(IDynamicValue dynamicValue) => Getter(dynamicValue);

    public void Set(IDynamicValue dynamicValue, object value) => Setter(dynamicValue, value);

    protected abstract T Converter(object value);
    protected abstract T Getter(IDynamicValue dynamicValue);
    protected abstract void Setter(IDynamicValue dynamicValue, object value);
}

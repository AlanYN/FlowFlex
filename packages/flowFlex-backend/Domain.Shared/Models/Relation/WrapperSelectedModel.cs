namespace FlowFlex.Domain.Shared.Models.Relation
{
    /// <summary>
    /// Wrapper object with selected property
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WrapperSelectedModel<T>
    {
        public T Value { get; set; }
        public bool Selected { get; set; }
        public long Id { get; set; }
    }
}

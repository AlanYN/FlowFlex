using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Represents a collection of FieldDataItems as a dynamic data object
/// </summary>
[DebuggerTypeProxy(typeof(DynamicDataObjectDebugView))]
public class DynamicDataObject : List<FieldDataItem>
{
    /// <summary>
    /// Initializes a new instance of the DynamicDataObject class
    /// </summary>
    public DynamicDataObject() { }

    /// <summary>
    /// Initializes a new instance of the DynamicDataObject class with a specified business ID
    /// </summary>
    /// <param name="businessId">The business identifier</param>
    public DynamicDataObject(long businessId)
    {
        BusinessId = businessId;
    }

    public DynamicDataObject(long businessId, int moduleId)
        : this(businessId)
    {
        ModuleId = moduleId;
    }

    /// <summary>
    /// Initializes a new instance of the DynamicDataObject class with a list of FieldDataItems
    /// </summary>
    /// <param name="dataItems">The list of FieldDataItems</param>
    public DynamicDataObject(List<FieldDataItem> dataItems)
    {
        if (dataItems.Count != 0)
        {
            BusinessId = dataItems.First().BusinessId;
            AddRange(dataItems);
        }
    }

    private long _businessId;

    /// <summary>
    /// Gets or sets the business identifier for all FieldDataItems in the collection
    /// </summary>
    public long BusinessId
    {
        get
        {
            if (Count == 0)
                return _businessId;
            var businessId = this.First().BusinessId;
            _businessId = businessId;

            return _businessId;
        }
        set
        {
            _businessId = value;
            foreach (var item in this)
            {
                item.BusinessId = _businessId;
            }
        }
    }

    public int ModuleId { get; private set; }

    public JObject InternalData { get; set; }

    public new void Sort()
    {
        Sort((self, target) => self.Sort.CompareTo(target.Sort));
    }

    public DateTimeOffset CreateDate { get; set; }

    public DateTimeOffset ModifyDate { get; set; }

    public string CreateBy { get; set; }

    public string ModifyBy { get; set; }

    public long CreateUserId { get; set; }

    public long ModifyUserId { get; set; }

    public UserPermissionsModel UserPermissions { get; set; }

    #region Methods

    /// <summary>
    /// Gets the value of a FieldDataItem by its field name
    /// </summary>
    /// <param name="name">The field name</param>
    /// <returns>The value of the field, or null if not found</returns>
    public object this[string name]
    {
        get
        {
            var item = Find(x => x.FieldName == name);
            if (item != null) return item.Value;
            else return null;
        }
        set
        {
            var item = Find(x => x.FieldName == name);
            if (item != null) item.Value = value;
        }
    }

    public T GetValueOrDefault<T>(string name)
    {
        var item = Find(x => x.FieldName == name);
        if (item != null && item.Value != null)
        {
            return (T)Convert.ChangeType(item.Value, typeof(T));
        }
        else return default;
    }

    public T GetValueOrDefault<T>(string name, T defaultValue)
    {
        var item = Find(x => x.FieldName == name);
        if (item != null && item.Value != null)
        {
            return (T)Convert.ChangeType(item.Value, typeof(T));
        }
        else return defaultValue;
    }

    public bool Contains(string name)
    {
        var item = Find(x => x.FieldName == name);
        return item != null;
    }

    public void Remove(Func<FieldDataItem, bool> func)
    {
        var items = this.Where(func).ToList();
        foreach (var item in items)
        {
            Remove(item);
        }
    }

    /// <summary>
    /// Adds a new FieldDataItem to the collection
    /// </summary>
    /// <param name="item">The FieldDataItem to add</param>
    public new void Add(FieldDataItem item)
    {
        if (item.BusinessId == 0)
            item.BusinessId = _businessId;
        else if (item.BusinessId != 0 && _businessId == 0)
            _businessId = item.BusinessId;

        var temp = this.FirstOrDefault(x => x.FieldName == item.FieldName);
        if (temp != null)
            Remove(temp);

        base.Add(item);
    }

    /// <summary>
    /// Adds a range of FieldDataItems to the collection
    /// </summary>
    /// <param name="items">The collection of FieldDataItems to add</param>
    public new void AddRange(IEnumerable<FieldDataItem> items)
    {
        if (items == null)
            return;

        foreach (var item in items)
            Add(item);
    }

    #endregion
}

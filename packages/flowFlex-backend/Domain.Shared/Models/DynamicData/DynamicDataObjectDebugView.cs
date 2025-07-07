using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class DynamicDataObjectDebugView(DynamicDataObject dynamicObj)
{
    private readonly DynamicDataObject _dynamicObj = dynamicObj;

    public Dictionary<string, object> DisplayItems => _dynamicObj.ToDictionary(x => x.FieldName, x => x.Value);

    public int Count => _dynamicObj.Count;

    public long BusinessId => _dynamicObj.BusinessId;

    public string CreateBy => _dynamicObj.CreateBy;

    public string ModifyBy => _dynamicObj.ModifyBy;

    public long CreateUserId => _dynamicObj.CreateUserId;

    public long ModifyUserId => _dynamicObj.ModifyUserId;
}

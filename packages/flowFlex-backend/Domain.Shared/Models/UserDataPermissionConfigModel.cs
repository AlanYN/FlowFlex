using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models;

public class UserDataPermissionConfigModel
{
    public List<ConfigModel> DataPermissions { get; set; } = new List<ConfigModel>();
    public class ConfigModel
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public bool? IsAdd { get; set; }
        public int? ViewType { get; set; }
        public int? EditType { get; set; }
        public int? DeleteType { get; set; }
        public bool? IsApproval { get; set; }
    }
}

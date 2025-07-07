

namespace FlowFlex.Domain.Shared.Models
{
    public class UserPermissionsModel
    {
        public UserPermissionsModel() { }
        public UserPermissionsModel(
            bool currentUserCanEdit,
            bool currentUserCanView,
            bool currentUserCanDelete
            )
        {
            CurrentUserCanEdit = currentUserCanEdit;
            CurrentUserCanView = currentUserCanView;
            CurrentUserCanDelete = currentUserCanDelete;
        }
        public bool CurrentUserCanEdit { get; set; } = false;
        public bool CurrentUserCanView { get; set; } = false;
        public bool CurrentUserCanDelete { get; set; } = false;
    }
}

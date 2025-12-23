namespace Domain.Shared.Enums
{
    /// <summary>
    /// URL参数值来源类型
    /// </summary>
    public enum UrlParameterValueSource
    {
        /// <summary>
        /// 页面参数
        /// </summary>
        PageParameter = 0,

        /// <summary>
        /// 登录用户信息
        /// </summary>
        LoginUserInfo = 1,

        /// <summary>
        /// 固定值
        /// </summary>
        FixedValue = 2,

        /// <summary>
        /// 系统变量
        /// </summary>
        SystemVariable = 3
    }
}


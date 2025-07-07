using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.Item
{
    [SugarTable("operate_log")]
    public class OperateLog : EntityBaseCreateInfo
    {
        [SugarColumn(ColumnName = "OperateId")]
        public override long Id { get; set; }

        /// <summary>
        /// 业务ID
        /// </summary>
        public string OperateKey
        {
            set; get;
        }

        /// <summary>
        /// log信息体
        /// </summary>
        public string OperateDescription
        {
            set; get;
        }

        /// <summary>
        /// Operate Title
        /// </summary>
        public string OperateTitle
        {
            set; get;
        }
    }
}

using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Const
{
    public class CreditLimitCosts
    {
        public static readonly Dictionary<int, string> ApproverRole = new Dictionary<int, string>
                                                                                        {
                                                                                            {1, "Supervisor"},
                                                                                            {2, "Manager"},
                                                                                            {3, "Controller"},
                                                                                            {4, "CEO/COO"}
                                                                                        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Abstracts;

public interface IValidFilter
{
    bool IsValid { get; set; }
}

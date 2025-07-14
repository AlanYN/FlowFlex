using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Abstracts;

/// <summary>
/// Application filter interface for app-level data isolation
/// </summary>
public interface IAppFilter
{
    /// <summary>
    /// Application code
    /// </summary>
    string AppCode { get; set; }
} 
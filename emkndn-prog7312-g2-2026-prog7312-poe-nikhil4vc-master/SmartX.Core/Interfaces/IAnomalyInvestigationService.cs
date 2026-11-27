using SmartX.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.Interfaces
{
    public interface IAnomalyInvestigationService
    {
        Task<IReadOnlyList<AnomalyInvestigationResponse>>
            AnalyseAsync(
                CancellationToken cancellationToken = default);
    }
}

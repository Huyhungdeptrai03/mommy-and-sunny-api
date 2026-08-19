using MommyAndSunny.Application.PublicStats.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PublicStats.Services
{
    public interface IPublicStatsService
    {
        Task<PublicStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    }
}

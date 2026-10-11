
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IBasicMenuRepository
    {
        Task<IEnumerable<BasicMenu>> GetBasicMenusByUserAndDeviceAsync(long userId, int deviceType);
        
    }
}

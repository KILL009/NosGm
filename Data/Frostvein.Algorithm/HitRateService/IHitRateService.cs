using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public interface IHitRateService
    {
        long GetHitRate(ClassType entityClass, byte level);
    }
}

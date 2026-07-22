using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public interface ISecondaryHitRateService
    {
        long GetSecondaryHitRate(ClassType entityClass, byte level);
    }
}

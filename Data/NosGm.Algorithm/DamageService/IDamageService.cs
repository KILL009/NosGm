using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public interface IDamageService
    {
        long GetMinDamage(ClassType entityClass, byte level);

        long GetMaxDamage(ClassType entityClass, byte level);
    }
}

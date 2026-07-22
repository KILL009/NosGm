using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public interface IMagicDefenceService
    {
        long GetMagicDefence(ClassType entityClass, byte level);
    }
}

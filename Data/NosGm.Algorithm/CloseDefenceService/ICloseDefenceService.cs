using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public interface ICloseDefenceService
    {
        long GetCloseDefence(ClassType entityClass, byte level);
    }
}

using NosGm.Domain;
using System;

namespace NosGm.GameObject.EventArguments
{
    public class FinishScriptedInstanceEventArgs : EventArgs
    {
        public FinishScriptedInstanceEventArgs(ScriptedInstanceType type, int id, int score = 0)
        {
            ScriptedInstanceType = type;
            Id = id;
            Score = score;
        }

        public ScriptedInstanceType ScriptedInstanceType { get; }

        public int Id { get; }

        public int Score { get; }
    }
}

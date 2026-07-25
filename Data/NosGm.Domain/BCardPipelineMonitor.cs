using System.Threading;

namespace NosGm.Domain
{
    public sealed class BCardPipelineSnapshot
    {
        public long Executed { get; internal set; }
        public long PassiveSkipped { get; internal set; }
        public long Missing { get; internal set; }
        public long MissingUnique { get; internal set; }
        public long PreInitializationAttempts { get; internal set; }
        public long HandlerFailures { get; internal set; }
    }

    public static class BCardPipelineMonitor
    {
        private sealed class CounterState
        {
            internal long Executed;
            internal long PassiveSkipped;
            internal long Missing;
            internal long MissingUnique;
            internal long PreInitializationAttempts;
            internal long HandlerFailures;
        }

        private static CounterState _state = new CounterState();

        public static void RecordExecuted() =>
            Interlocked.Increment(ref Volatile.Read(ref _state).Executed);

        public static void RecordPassiveSkipped() =>
            Interlocked.Increment(ref Volatile.Read(ref _state).PassiveSkipped);

        public static void RecordMissing(bool unique)
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.Missing);
            if (unique)
            {
                Interlocked.Increment(ref state.MissingUnique);
            }
        }

        public static void RecordPreInitializationAttempt() =>
            Interlocked.Increment(ref Volatile.Read(ref _state).PreInitializationAttempts);

        public static void RecordHandlerFailure() =>
            Interlocked.Increment(ref Volatile.Read(ref _state).HandlerFailures);

        public static BCardPipelineSnapshot Capture()
        {
            CounterState state = Volatile.Read(ref _state);
            return new BCardPipelineSnapshot
            {
                Executed = Interlocked.Read(ref state.Executed),
                PassiveSkipped = Interlocked.Read(ref state.PassiveSkipped),
                Missing = Interlocked.Read(ref state.Missing),
                MissingUnique = Interlocked.Read(ref state.MissingUnique),
                PreInitializationAttempts = Interlocked.Read(ref state.PreInitializationAttempts),
                HandlerFailures = Interlocked.Read(ref state.HandlerFailures)
            };
        }

        public static void Reset() =>
            Interlocked.Exchange(ref _state, new CounterState());
    }
}

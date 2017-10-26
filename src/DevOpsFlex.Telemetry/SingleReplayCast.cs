namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    /// <summary>
    /// Replays a single time from an original <see cref="IObservable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the original stream.</typeparam>
    public class SingleReplayCast<T> : IDisposable
    {
        internal readonly object Gate = new object();

        internal ReplaySubject<T> Replay = new ReplaySubject<T>();

        internal IDisposable ReplayConnection;

        internal IDisposable ReplaySubscription;

        /// <summary>
        /// Initializes a new instance of <see cref="SingleReplayCast{T}"/>
        /// </summary>
        /// <param name="origin">The original stream that we want to replay a single time.</param>
        public SingleReplayCast(IObservable<T> origin)
        {
            var connection = origin.Multicast(Replay);
            ReplayConnection = connection.Connect();
        }

        /// <summary>
        /// Subscribes to a single replay buffer.
        ///     This will check if there's already a single active subscription, and if it is returns the existing one instead.
        /// </summary>
        /// <param name="action">The <see cref="Action{T}"/> we want to perform during the subscription.</param>
        /// <returns>The subscription <see cref="IDisposable"/>.</returns>
        public IDisposable Subscribe(Action<T> action)
        {
            lock (Gate)
            {
                if (ReplaySubscription == null)
                {
                    ReplaySubscription = Replay.Subscribe(action, Dispose);
                    Replay.OnCompleted();
                }

                return ReplaySubscription;
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            ReplayConnection?.Dispose();
            ReplaySubscription?.Dispose();
            Replay?.Dispose();
        }
    }
}

#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;
using System.Threading;

namespace Apocraphy.Assets.Scripts.Threading
{
    /// <summary>
    ///     Signals to a <see cref="CancellationToken" /> that it should be cancelled.
    /// </summary>
    public sealed class CancellationTokenSource
    {
        // Legal values for _state
        private const int _cannotBeCanceled = 0;
        private const int _notCanceled = 1;
        private const int _cancelled = 2;


        /// <summary>
        /// An already cancelled <see cref="CancellationTokenSource"/>.
        /// </summary>
        internal static readonly CancellationTokenSource Cancelled = new CancellationTokenSource(true);

        /// <summary>
        /// A <see cref="CancellationTokenSource"/> that can never be cancelled.
        /// </summary>
        internal static readonly CancellationTokenSource NotCancellable = new CancellationTokenSource(false);

        /// <summary>
        ///     The timestamp to cancel after (if any)
        /// </summary>
        private long _cancelAfter;
        
        /// <summary>
        /// The internal state.
        /// </summary>
        private int _state;

        private CancellationTokenSource(bool canBeCancelled = true)
        {
            _state = canBeCancelled ? _cancelled : _cannotBeCanceled;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellationTokenSource"/> class.
        /// </summary>
        /// <param name="cancelAfter">The duration to cancel after.</param>
        public CancellationTokenSource(TimeSpan cancelAfter)
            : this((long) cancelAfter.TotalMilliseconds)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellationTokenSource"/> class.
        /// </summary>
        /// <param name="cancelAfterMs">The duration in milliseconds to cancel after.</param>
        public CancellationTokenSource(long cancelAfterMs = -1)
        {
            if (cancelAfterMs > 0)
            {
                _state = _notCanceled;
                _cancelAfter = TaskManager.Time + cancelAfterMs;
            }
            else if (cancelAfterMs == 0)
                _state = _cancelled;
            else
                _state = _notCanceled;
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>The token.</value>
        public CancellationToken Token
        {
            get { return new CancellationToken(this); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is cancelled.
        /// </summary>
        /// <value><c>true</c> if this instance is cancelled; otherwise, <c>false</c>.</value>
        public bool IsCancellationRequested
        {
            get
            {
                if (_state > _notCanceled)
                    return true;
                if ((_state == _cannotBeCanceled) ||
                    (_cancelAfter < 1) ||
                    (_cancelAfter > TaskManager.Time))
                    return false;

                // Update state to cancelled as time has elapsed.
                Interlocked.CompareExchange(ref _state, _cancelled, _notCanceled);
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can be cancelled.
        /// </summary>
        /// <value><c>true</c> if this instance can be cancelled; otherwise, <c>false</c>.</value>
        public bool CanBeCancelled
        {
            get { return _state != _cannotBeCanceled; }
        }

        /// <summary>
        ///     Cancels this instance.
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref _state, _cancelled, _notCanceled) == _cannotBeCanceled)
                throw new InvalidOperationException("The cancellation token source cannot be cancelled.");
        }

        /// <summary>
        ///     Cancels the token after the set delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <exception cref="ArgumentOutOfRangeException">delay</exception>
        public void CancelAfter(TimeSpan delay)
        {
            long totalMilliseconds = (long) delay.TotalMilliseconds;
            if ((totalMilliseconds < -1) ||
                (totalMilliseconds > int.MaxValue))
                throw new ArgumentOutOfRangeException("delay");

            CancelAfter((int) totalMilliseconds);
        }

        /// <summary>
        ///     Cancels the token after he set number of seconds.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <exception cref="ArgumentOutOfRangeException">millisecondsDelay</exception>
        public void CancelAfter(int millisecondsDelay)
        {
            if (millisecondsDelay < -1)
                throw new ArgumentOutOfRangeException("millisecondsDelay");

            switch (_state)
            {
                case _cannotBeCanceled:
                    throw new InvalidOperationException("The cancellation token source cannot be cancelled.");
                case _cancelled:
                    // Note we don't check elapsed time as we are already updating the wait duration.
                    return;
            }

            _cancelAfter = TaskManager.Time + millisecondsDelay;
        }
    }
}
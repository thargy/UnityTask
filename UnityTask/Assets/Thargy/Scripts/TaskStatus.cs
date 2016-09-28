#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

namespace Apocraphy.Assets.Scripts.Threading
{
    /// <summary>
    ///     Represents the current stage in the life cycle of a <see cref="Task" />.
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        ///     The task has been initialized but has not yet been scheduled.
        /// </summary>
        Created = 0,

        /// <summary>
        ///     The task has been scheduled for execution but has not yet begun executing.
        /// </summary>
        WaitingToRun = 1,

        /// <summary>
        ///     The task is running but has not yet completed.
        /// </summary>
        Running = 2,

        /// <summary>
        ///     The task completed execution successfully.
        /// </summary>
        RanToCompletion = 3,

        /// <summary>
        ///     The task completed due to an unhandled exception.
        /// </summary>
        Faulted = 4,

        /// <summary>
        ///     The task acknowledged cancellation by throwing an OperationCanceledException with its own CancellationToken
        ///     while the token was in signalled state, or the task's CancellationToken was already signalled before the
        ///     task started executing.
        /// </summary>
        Cancelled = 5
    }
}
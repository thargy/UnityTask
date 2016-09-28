#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;

namespace Apocraphy.Assets.Scripts.Threading
{
    /// <summary>
    /// Interface ITaskScheduler must be implemented by any class wishing to be used to schedule <see cref="Task">tasks</see>.
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// Schedules the specified action to run.
        /// </summary>
        /// <param name="action">The action to run.</param>
        void Schedule(Action action);
    }
}
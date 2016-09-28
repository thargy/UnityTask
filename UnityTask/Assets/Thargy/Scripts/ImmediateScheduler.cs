#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;

namespace Apocraphy.Assets.Scripts.Threading
{
    public partial class TaskManager
    {
        /// <summary>
        /// The ImmediateScheduler runs <see cref="Task">tasks</see> immediately.
        /// </summary>
        /// <remarks><para>This should be used for very short running operations only; otherwise consider <see cref="ThreadPoolScheduler"/>.</para>
        /// <para>The primary use for this scheduler is for short continuation code which will effectively run in the same thread as the previous
        /// task, and should run immediately following the previous task, and on the same thread.</para>
        /// <para>Any interactions with the Unity framework should be scheduled to run using the <see cref="TaskManager"/>.</para></remarks>
        /// <seealso cref="ITaskScheduler" />
        private class ImmediateScheduler : ITaskScheduler
        {
            public void Schedule(Action action)
            {
                // Run immediately!
                action();
            }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return "Immediate Scheduler";
            }
        }
    }
}
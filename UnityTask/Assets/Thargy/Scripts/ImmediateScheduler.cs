#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// Written by Craig Anthony Dean <support@thargy.com>

#endregion

using System;

namespace Thargy.UnityTask
{
    public partial class TaskManager
    {
        /// <summary>
        ///     The ImmediateScheduler runs <see cref="Task">tasks</see> immediately.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This should be used for very short running operations only; otherwise consider
        ///         <see cref="ThreadPoolScheduler" />.
        ///     </para>
        ///     <para>
        ///         The primary use for this scheduler is for short continuation code which will effectively run in the same thread
        ///         as the previous
        ///         task, and should run immediately following the previous task, and on the same thread.
        ///     </para>
        ///     <para>Any interactions with the Unity framework should be scheduled to run using the <see cref="TaskManager" />.</para>
        /// </remarks>
        /// <seealso cref="ITaskScheduler" />
        private class ImmediateScheduler : ITaskScheduler
        {
            public void Schedule(Action action)
            {
                // Run immediately!
                action();
            }

            /// <summary>
            ///     Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return "Immediate Scheduler";
            }
        }
    }
}
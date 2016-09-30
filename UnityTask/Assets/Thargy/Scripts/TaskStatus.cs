#region Copyright (C) Craig Anthony Dean 2016

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

namespace Thargy.UnityTask
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
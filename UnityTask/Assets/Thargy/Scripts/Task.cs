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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Thargy.UnityTask
{
    /// <summary>
    ///     Task's make it easy to schedule, chain and execute operations.
    /// </summary>
    public class Task : ITask
    {
        public delegate void FailedTaskDelegate([NotNull] Exception exception);

        public delegate void FailedTaskWithCancellationDelegate(
            [NotNull] Exception exception,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Delegate of method for creating a task that runs but doesn't return a result.
        /// </summary>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     <para>
        ///         This overload should only be used for very quick tasks, ideally you should accept and regularly check
        ///         a cancellation token.
        ///     </para>
        /// </remarks>
        public delegate void TaskDelegate();

        /// <summary>
        ///     Delegate of method for creating a task that runs but doesn't return a result.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     <para>
        ///         During long running operations, checking <paramref name="cancellationToken" /> allows the task to voluntarily
        ///         execute
        ///         as soon as possible and free resources. This should be done relatively frequently.
        ///     </para>
        /// </remarks>
        public delegate void TaskWithCancellationDelegate(CancellationToken cancellationToken);

        /// <summary>
        ///     All active tasks.
        /// </summary>
        [NotNull] private static readonly OrderedDictionary _all = new OrderedDictionary();

        /// <summary>
        ///     The Task ID Counter, for generating unique ids
        /// </summary>
        private static long _taskCounter;

        /// <summary>
        ///     A task in the cancelled state.
        /// </summary>
        [NotNull] [UsedImplicitly] public static readonly Task Cancelled = new Task(
            (TaskDelegate) null,
            TaskManager.Immediate,
            CancellationTokenSource.Cancelled.Token);

        /// <summary>
        ///     The Task ID.
        /// </summary>
        private readonly long _id = Interlocked.Increment(ref _taskCounter);

        /// <summary>
        ///     The tasks to execute on failure.
        /// </summary>
        [NotNull] private readonly Queue<Task> _onFailure = new Queue<Task>();

        /// <summary>
        ///     The tasks to execute on success.
        /// </summary>
        [NotNull] private readonly Queue<Task> _onSuccess = new Queue<Task>();

        /// <summary>
        ///     The scheduler that the task is scheduled to run on.
        /// </summary>
        [NotNull] private readonly ITaskScheduler _scheduler;

        /// <summary>
        ///     The associated cancellation token (if any).
        /// </summary>
        protected readonly CancellationToken Token;

        private int _state;

        /// <summary>
        ///     The action to execute (if any).
        /// </summary>
        [CanBeNull] protected Action Action;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [UsedImplicitly]
        public Task(
            [NotNull] TaskDelegate action,
            [CanBeNull] ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
            : this(scheduler, cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    action();
                    SetResult(null);
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Task" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task(
            [NotNull] TaskWithCancellationDelegate action,
            [CanBeNull] ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
            : this(scheduler, cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    action(cancellationToken);
                    SetResult(null);
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task" /> class.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        ///     <para>This task needs to be controlled manually as there is no action to execute.</para>
        ///     <para>
        ///         The state can be progressed, either by canceling the <paramref name="cancellationToken" />,
        ///         or calling <see cref="SetResult" /> or <see cref="SetException" />.
        ///     </para>
        /// </remarks>
        public Task(
            [CanBeNull] ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            _scheduler = scheduler ?? TaskManager.ThreadPool;
            Token = cancellationToken;

            // Add to the tasks collection.
            lock (_all)
                _all.Add(_id, this);
        }

        /// <summary>
        ///     Gets all active tasks (in <see cref="Id" /> order).
        /// </summary>
        /// <value>The active tasks.</value>
        /// <remarks>
        ///     <para>These are all tasks where the <see cref="State" /> is <see cref="TaskStatus.Running">running or less</see>.</para>
        /// </remarks>
        [NotNull]
        public static IEnumerable<Task> All
        {
            get
            {
                lock (_all)
                    return _all.Values.Cast<Task>().Where(t => t.State <= TaskStatus.Running).ToArray();
            }
        }

        /// <summary>
        ///     Gets the Task's unique identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     The associated cancellation token, if any; otherwise <see cref="UnityTask.CancellationToken.None" />.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return Token; }
        }

        /// <summary>
        ///     The scheduler that the task is scheduled to run on.
        /// </summary>
        /// <value>The scheduler.</value>
        [NotNull]
        public ITaskScheduler Scheduler
        {
            get { return _scheduler; }
        }

        /// <summary>
        ///     Gets the exception, if the task failed.
        /// </summary>
        /// <value>The exception.</value>
        [CanBeNull]
        public Exception Exception { get; private set; }

        [CanBeNull]
        public object Result { get; private set; }

        public bool IsCancelled
        {
            get { return State == TaskStatus.Cancelled; }
        }

        public bool IsRunning
        {
            get { return State == TaskStatus.Running; }
        }

        public bool IsCompleted
        {
            get { return State == TaskStatus.RanToCompletion; }
        }

        public bool IsFaulted
        {
            get { return State == TaskStatus.Faulted; }
        }

        public bool IsFinished
        {
            get { return State > TaskStatus.Running; }
        }

        public TaskStatus State
        {
            get
            {
                // Check for cancellation
                if (Token.IsCancellationRequested &&
                    (_state != (int) TaskStatus.Cancelled))
                    ChangeState(TaskStatus.Cancelled);
                return (TaskStatus) _state;
            }
        }

        /// <summary>
        ///     Adds a delay following success of the task.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        [NotNull]
        public ITask Wait(TimeSpan delay)
        {
            return
                OnSuccess(
                    new Task(
                        (TaskDelegate) null,
                        TaskManager.AfterDelay(delay, TaskManager.Immediate),
                        Token));
        }

        /// <summary>
        ///     Adds a delay following success of the task.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        [NotNull]
        public ITask Wait(int millisecondsDelay)
        {
            return
                OnSuccess(
                    new Task(
                        (TaskDelegate) null,
                        TaskManager.AfterDelay(millisecondsDelay, TaskManager.Immediate),
                        Token));
        }

        /// <summary>
        ///     Cancels this instance.
        /// </summary>
        public void Cancel()
        {
            ChangeState(TaskStatus.Cancelled);
        }

        [NotNull]
        public ITask OnSuccess(TaskDelegate action, ITaskScheduler scheduler = null)
        {
            return OnSuccess(new Task(action, scheduler, Token));
        }


        [NotNull]
        public ITask OnSuccess(TaskWithCancellationDelegate action, ITaskScheduler scheduler = null)
        {
            return OnSuccess(new Task(action, scheduler, Token));
        }

        [NotNull]
        public ITask<TResult> OnSuccess<TResult>(
            Task<TResult>.ResultTaskDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult>) OnSuccess(new Task<TResult>(function, scheduler, Token));
        }

        [NotNull]
        public ITask<TResult> OnSuccess<TResult>(
            Task<TResult>.ResultTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult>) OnSuccess(new Task<TResult>(function, scheduler, Token));
        }

        [NotNull]
        public ITask<TResult, TProgress> OnSuccess<TResult, TProgress>(
            Task<TResult, TProgress>.ProgressTaskDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult, TProgress>) OnSuccess(new Task<TResult, TProgress>(function, scheduler, Token));
        }

        [NotNull]
        public ITask<TResult, TProgress> OnSuccess<TResult, TProgress>(
            Task<TResult, TProgress>.ProgressTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult, TProgress>) OnSuccess(new Task<TResult, TProgress>(function, scheduler, Token));
        }

        [NotNull]
        public ITask OnFailure(FailedTaskDelegate action, ITaskScheduler scheduler = null)
        {
            return OnFailure(new Task(() => action(Exception), scheduler, Token));
        }


        [NotNull]
        public ITask OnFailure(FailedTaskWithCancellationDelegate action, ITaskScheduler scheduler = null)
        {
            return OnFailure(new Task(t => action(Exception, t), scheduler, Token));
        }

        [NotNull]
        public ITask<TResult> OnFailure<TResult>(
            Task<TResult>.FailedResultTaskDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult>) OnFailure(new Task<TResult>(() => function(Exception), scheduler, Token));
        }

        [NotNull]
        public ITask<TResult> OnFailure<TResult>(
            Task<TResult>.FailedResultTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<TResult>) OnFailure(new Task<TResult>(t => function(Exception, t), scheduler, Token));
        }

        [NotNull]
        public ITask<TResult, TProgress> OnFailure<TResult, TProgress>(
            Task<TResult, TProgress>.FailedProgressTaskDelegate function,
            ITaskScheduler scheduler = null)
        {
            return
                (ITask<TResult, TProgress>)
                    OnFailure(new Task<TResult, TProgress>(n => function(Exception, n), scheduler, Token));
        }

        [NotNull]
        public ITask<TResult, TProgress> OnFailure<TResult, TProgress>(
            Task<TResult, TProgress>.FailedProgressTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null)
        {
            return
                (ITask<TResult, TProgress>)
                    OnFailure(new Task<TResult, TProgress>((n, t) => function(Exception, n, t), scheduler, Token));
        }

        [NotNull]
        public static Task Completed(CancellationToken token = default(CancellationToken))
        {
            return new Task(TaskManager.Immediate, token).SetResult(null);
        }

        [NotNull]
        public static Task FromException(Exception excetpion, CancellationToken token = default(CancellationToken))
        {
            return new Task(TaskManager.Immediate, CancellationToken.None).SetException(null);
        }

        [NotNull]
        public static Task<T> FromException<T>(
            Exception excetpion,
            CancellationToken token = default(CancellationToken))
        {
            return
                new Task<T>(TaskManager.Immediate, CancellationToken.None).
                    SetException(null);
        }

        public static Task<T> FromResult<T>(T result, CancellationToken token = default(CancellationToken))
        {
            return
                new Task<T>(TaskManager.Immediate, CancellationToken.None).
                    SetResult(result);
        }

        /// <summary>
        ///     A task that will take at least the delay to complete (unless cancelled).
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static ITask Delay(
            TimeSpan delay,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Start((TaskDelegate) null, TaskManager.AfterDelay(delay), cancellationToken);
        }

        /// <summary>
        ///     A task that will take at least the specified milliseconds delay to complete (unless cancelled).
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static ITask Delay(
            int millisecondsDelay,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Start((TaskDelegate) null, TaskManager.AfterDelay(millisecondsDelay), cancellationToken);
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Task" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static ITask Start(
            TaskDelegate action,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task(action, scheduler, cancellationToken).Run();
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Task" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static ITask Start(
            TaskWithCancellationDelegate action,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task(action, scheduler, cancellationToken).Run();
        }

        public static ITask<TResult> Start<TResult>(
            Task<TResult>.ResultTaskDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task<TResult>(function, scheduler, cancellationToken).Run();
        }

        public static ITask<TResult> Start<TResult>(
            Task<TResult>.ResultTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task<TResult>(function, scheduler, cancellationToken).Run();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task{TResult, TProgress}" /> class.
        /// </summary>
        /// <param name="function">The function to call when the task is run.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static ITask<TResult, TProgress> Start<TResult, TProgress>(
            Task<TResult, TProgress>.ProgressTaskDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task<TResult, TProgress>(function, scheduler, cancellationToken).Run();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task{TResult, TProgress}" /> class.
        /// </summary>
        /// <param name="function">The function to call when the task is run.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static ITask<TResult, TProgress> Start<TResult, TProgress>(
            Task<TResult, TProgress>.ProgressTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new Task<TResult, TProgress>(function, scheduler, cancellationToken).Run();
        }

        public ITask Run()
        {
            Action action = Action;
            if (ReferenceEquals(action, null))
                throw new InvalidOperationException(
                    "The task could not be scheduled to run as it was not created with an action.");

            if (!ChangeState(TaskStatus.WaitingToRun))
                throw new InvalidOperationException(
                    string.Format("The task could not be scheduled to run as it was in the '{0}' state.", State));

            _scheduler.Schedule(action);
            return this;
        }

        public bool TryRun()
        {
            Action action = Action;
            if (ReferenceEquals(action, null) ||
                !ChangeState(TaskStatus.WaitingToRun))
                return false;

            _scheduler.Schedule(action);
            return true;
        }

        public Task SetResult(object result = null)
        {
            if (!ChangeState(TaskStatus.RanToCompletion))
                return this;

            Result = result;

            // Grab success tasks
            Task[] tasks;
            lock (_onSuccess)
            {
                tasks = _onSuccess.ToArray();
                _onSuccess.Clear();
            }

            Close();

            // Schedule tasks to run
            foreach (Task task in tasks)
                task.TryRun();

            return this;
        }

        public Task SetException(Exception exception)
        {
            if (!ChangeState(TaskStatus.Faulted))
                return this;

            Exception = exception;

            // Grab failure tasks
            Task[] tasks;
            lock (_onFailure)
            {
                tasks = _onFailure.ToArray();
                _onFailure.Clear();
            }

            // Schedule tasks to run
            foreach (Task task in tasks)
                task.TryRun();

            Close();

            return this;
        }

        /// <summary>
        ///     Tests a task before running.
        /// </summary>
        /// <returns><c>true</c> the task cannot be ran, <c>false</c> otherwise.</returns>
        protected bool ChangeState(TaskStatus nextState)
        {
            // Check we're not being cancelled, which is always valid.
            if (!Token.IsCancellationRequested &&
                (nextState != TaskStatus.Cancelled))
            {
                TaskStatus previoustState;

                // If we have no action we should only transition from the created state, and we never enter the WaitingToRun or Running states.
                if (ReferenceEquals(Action, null))
                    switch (nextState)
                    {
                        case TaskStatus.RanToCompletion:
                        case TaskStatus.Faulted:
                            previoustState = TaskStatus.Created;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                "nextState",
                                nextState,
                                string.Format(
                                    "Cannot transition to the '{0}' state as there is no action defined.",
                                    nextState));
                    }
                else
                    switch (nextState)
                    {
                        case TaskStatus.WaitingToRun:
                            previoustState = TaskStatus.Created;
                            break;
                        case TaskStatus.Running:
                            previoustState = TaskStatus.WaitingToRun;
                            break;
                        case TaskStatus.RanToCompletion:
                            previoustState = TaskStatus.Running;
                            break;
                        case TaskStatus.Faulted:
                            previoustState = TaskStatus.Running;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                "nextState",
                                nextState,
                                string.Format("Cannot transition to the '{0}' state.", nextState));
                    }

                // Update state to new state.
                if (Interlocked.CompareExchange(ref _state, (int) nextState, (int) previoustState) ==
                    (int) previoustState)
                    return true;

                // Set the task to failed - note we won't call callbacks as this is a bad state transition
                // If we were already transition to failed then we don't call SetException again to prevent
                // infinite recursion.
                if (nextState != TaskStatus.Faulted)
                    SetException(
                        new InvalidOperationException(
                            string.Format(
                                "The task could not move to the '{1}' state as it was in the '{0}' state, and should have been in the {2} state.",
                                (TaskStatus) _state,
                                nextState,
                                previoustState)));
                else
                    Interlocked.Exchange(ref _state, (int) TaskStatus.Faulted);
            }
            else
            {
                // Set to cancelled.
                Interlocked.Exchange(ref _state, (int) TaskStatus.Cancelled);
                Exception = null;
                Result = null;
            }

            // Clean up
            Close();
            return false;
        }

        protected Task OnSuccess(Task task)
        {
            if (State <= TaskStatus.Running)
                lock (_onSuccess)
                    if (State <= TaskStatus.Running)
                        _onSuccess.Enqueue(task);

            if (State == TaskStatus.RanToCompletion)
                task.TryRun();

            return task;
        }

        private Task OnFailure(Task task)
        {
            if (State <= TaskStatus.Running)
                lock (_onSuccess)
                    if (State <= TaskStatus.Running)
                        _onFailure.Enqueue(task);

            if (State == TaskStatus.Faulted)
                task.TryRun();

            return task;
        }

        protected virtual void Close()
        {
            // Remove the task from the active tasks.
            lock (_all)
                _all.Remove(_id);

            if (_onSuccess.Count > 0)
                lock (_onSuccess)
                    _onSuccess.Clear();

            if (_onFailure.Count > 0)
                lock (_onFailure)
                    _onFailure.Clear();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            string state;
            if (ReferenceEquals(Action, null))
                switch (State)
                {
                    case TaskStatus.Created:
                        state = "has been created without an action and is awaiting manual completion";
                        break;
                    case TaskStatus.WaitingToRun:
                        state = "is waiting to run, which is an invalid state as there is no action";
                        break;
                    case TaskStatus.Running:
                        state = "is running, which is an invalid state as there is no action";
                        break;
                    case TaskStatus.RanToCompletion:
                        state = string.Format("had it's result set to {0}", Result);
                        break;
                    case TaskStatus.Faulted:
                        state = string.Format(
                            "was manually set to failed. {0}",
                            Exception != null ? Exception.Message : "<null>");
                        break;
                    case TaskStatus.Cancelled:
                        state = "has not action and is now in the cancelled state";
                        break;
                    default:
                        state = string.Format("has no action and is in an unknown state '{0}'", State);
                        break;
                }
            else
                switch (State)
                {
                    case TaskStatus.Created:
                        state = string.Format("has been created, and will be scheduled on the {0}", _scheduler);
                        break;
                    case TaskStatus.WaitingToRun:
                        state = string.Format("has been scheduled to run on the {0}", _scheduler);
                        break;
                    case TaskStatus.Running:
                        state = string.Format("is running on the {0}", _scheduler);
                        break;
                    case TaskStatus.RanToCompletion:
                        state = string.Format("ran on the {0}.  Result: {1}", _scheduler, Result);
                        break;
                    case TaskStatus.Faulted:
                        state = string.Format(
                            "ran on the {0} and failed.  Exception: {1}",
                            _scheduler,
                            Exception != null ? Exception.Message : "<null>");
                        break;
                    case TaskStatus.Cancelled:
                        state = string.Format("was scheduled to run on the {0}, but is now cancelled", _scheduler);
                        break;
                    default:
                        state = string.Format(
                            "was scheduled to run on the {0}, but is in an unknown state {1}",
                            _scheduler,
                            State);
                        break;
                }

            return string.Format("Task '{0}' {1}.", _id, state);
        }
    }

    public class Task<TResult> : Task, ITask<TResult>
    {
        public delegate T ContinuationResultTaskDelegate<out T>(TResult result);

        public delegate T ContinuationResultTaskWithCancellationDelegate<out T>(
            TResult result,
            CancellationToken cancellationToken);

        public delegate void ContinuationTaskDelegate(TResult result);

        public delegate void ContinuationTaskWithCancellationDelegate(
            TResult result,
            CancellationToken cancellationToken);

        public delegate TResult FailedResultTaskDelegate(Exception exception);

        public delegate TResult FailedResultTaskWithCancellationDelegate(
            Exception exception,
            CancellationToken cancellationToken);

        public delegate TResult ResultTaskDelegate();

        public delegate TResult ResultTaskWithCancellationDelegate(CancellationToken cancellationToken);

        public Task(
            ResultTaskDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken)) : base(scheduler, cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    SetResult(function());
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }

        public Task(
            ResultTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken)) : base(scheduler, cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    SetResult(function(cancellationToken));
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }

        public Task(ITaskScheduler scheduler = null, CancellationToken cancellationToken = default(CancellationToken))
            : base(scheduler, cancellationToken)
        {
        }

        public new TResult Result
        {
            get { return (TResult) base.Result; }
        }

        /// <summary>
        ///     Adds a delay following success of the task.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        public new ITask<TResult> Wait(TimeSpan delay)
        {
            return
                (ITask<TResult>)
                    OnSuccess(
                        new Task<TResult>(() => Result, TaskManager.AfterDelay(delay, TaskManager.Immediate), Token));
        }

        /// <summary>
        ///     Adds a delay following success of the task.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        public new ITask<TResult> Wait(int millisecondsDelay)
        {
            return
                (ITask<TResult>)
                    OnSuccess(
                        new Task<TResult>(
                            () => Result,
                            TaskManager.AfterDelay(millisecondsDelay, TaskManager.Immediate),
                            Token));
        }

        public ITask OnSuccess(ContinuationTaskDelegate action, ITaskScheduler scheduler = null)
        {
            return OnSuccess(new Task(() => action(Result), scheduler, Token));
        }

        public ITask OnSuccess(ContinuationTaskWithCancellationDelegate action, ITaskScheduler scheduler = null)
        {
            return OnSuccess(new Task(t => action(Result, t), scheduler, Token));
        }

        public ITask<T> OnSuccess<T>(ContinuationResultTaskDelegate<T> function, ITaskScheduler scheduler = null)
        {
            return (ITask<T>) OnSuccess(new Task<T>(() => function(Result), scheduler, Token));
        }

        public ITask<T> OnSuccess<T>(
            ContinuationResultTaskWithCancellationDelegate<T> function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<T>) OnSuccess(new Task<T>(t => function(Result, t), scheduler, Token));
        }

        public ITask<T, TProgress> OnSuccess<T, TProgress>(
            Task<T, TProgress>.ContinuationProgressTaskDelegate<TResult> function,
            ITaskScheduler scheduler = null)
        {
            return (ITask<T, TProgress>) OnSuccess(new Task<T, TProgress>(n => function(Result, n), scheduler, Token));
        }

        public ITask<T, TProgress> OnSuccess<T, TProgress>(
            Task<T, TProgress>.ContinuationProgressTaskWithCancellationDelegate<TResult> function,
            ITaskScheduler scheduler = null)
        {
            return
                (ITask<T, TProgress>)
                    OnSuccess(new Task<T, TProgress>((n, t) => function(Result, n, t), scheduler, Token));
        }

        public new ITask<TResult> Run()
        {
            return (ITask<TResult>) base.Run();
        }

        public Task<TResult> SetResult(TResult result = default(TResult))
        {
            return (Task<TResult>) base.SetResult(result);
        }

        public new Task<TResult> SetException(Exception exception)
        {
            return (Task<TResult>) base.SetException(exception);
        }
    }

    /// <summary>
    ///     The <see cref="Task{TResult, TProgress}" /> allows for the creation of <see cref="Task">tasks</see> that can notify
    ///     subscribers of progress, and ultimately
    ///     return a <typeparamref name="TResult">result</typeparamref>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <typeparam name="TProgress">The type of the progress.</typeparam>
    /// <seealso cref="Threading.Task" />
    /// <seealso cref="Threading.Task{TResult}" />
    public sealed class Task<TResult, TProgress> : Task<TResult>, ITask<TResult, TProgress>
    {
        /// <summary>
        ///     Delegate of method for creating a task that accepts the result of a previous task and allows for updating
        ///     subscribers of it's progress,
        ///     before ultimately returning it's own result.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Task{T}.Result">preceeding task's result</see>.</typeparam>
        /// <param name="result">The result of the preceeding task.</param>
        /// <param name="notifyProgress">The notify progress method.</param>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     Calling the <paramref name="notifyProgress" /> method will update subscribers of the task's current progress.
        ///     It can be called repeatedly whilst the task is running.
        /// </remarks>
        public delegate TResult ContinuationProgressTaskDelegate<in T>(T result, NotifyProgressDelegate notifyProgress);

        /// <summary>
        ///     Delegate of method for creating a task that accepts the result of a previous task and allows for updating
        ///     subscribers of it's progress,
        ///     before ultimately returning it's own result.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Task{T}.Result">preceeding task's result</see>.</typeparam>
        /// <param name="result">The result of the preceeding task.</param>
        /// <param name="notifyProgress">The notify progress method.</param>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     Calling the <paramref name="notifyProgress" /> method will update subscribers of the task's current progress.
        ///     It can be called repeatedly whilst the task is running.
        /// </remarks>
        public delegate TResult ContinuationProgressTaskWithCancellationDelegate<in T>(
            T result,
            NotifyProgressDelegate notifyProgress,
            CancellationToken cancellationToken);

        public delegate TResult FailedProgressTaskDelegate(Exception exception, NotifyProgressDelegate notifyProgress);

        public delegate TResult FailedProgressTaskWithCancellationDelegate(
            Exception exception,
            NotifyProgressDelegate notifyProgress,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Delegate of method that is passed into a task to allow it to notify subscribers of progress.
        /// </summary>
        /// <param name="progress">The progress of the task.</param>
        /// <remarks>
        ///     <para>This can be called repeatedly as the task makes progress.</para>
        /// </remarks>
        public delegate void NotifyProgressDelegate(TProgress progress);

        /// <summary>
        ///     Delegate of method that is called when a task makes progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <remarks>
        ///     <para>This may be called repeatedly as the task makes progress.</para>
        ///     <para>
        ///         Although the associaed <see cref="Task{TResult, TProgress}" /> has a
        ///         <see cref="Task{TResult,TProgress}.LatestProgress" />
        ///         property, race conditions mean it may not match the supplied <paramref name="progress" /> property (which may
        ///         be out of date).  Depending
        ///         on the <see cref="ITaskScheduler">scheduler</see> specified when adding the handler it isn't impossible that
        ///         the various invocations
        ///         occur out of order, though it is unlikely.
        ///     </para>
        ///     <para>
        ///         If order is absolutely critical, consider using a <see cref="TProgress" /> type that incorporates an order
        ///         field.
        ///     </para>
        /// </remarks>
        public delegate void OnProgressDelegate(TProgress progress);

        /// <summary>
        ///     Delegate of method that is called when a task makes progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        ///     <para>This may be called repeatedly as the task makes progress.</para>
        ///     <para>
        ///         Although the associaed <see cref="Task{TResult, TProgress}" /> has a
        ///         <see cref="Task{TResult,TProgress}.LatestProgress" />
        ///         property, race conditions mean it may not match the supplied <paramref name="progress" /> property (which may
        ///         be out of date).  Depending
        ///         on the <see cref="ITaskScheduler">scheduler</see> specified when adding the handler it isn't impossible that
        ///         the various invocations
        ///         occur out of order, though it is unlikely.
        ///     </para>
        ///     <para>
        ///         If order is absolutely critical, consider using a <see cref="TProgress" /> type that incorporates an order
        ///         field.
        ///     </para>
        ///     <para>
        ///         The progress methods should endevour to be relatively quick and shouldn't be called when the
        ///         <paramref name="cancellationToken" />
        ///         is <see cref="CancellationToken.IsCancellationRequested">cancelled</see>.  As such this overload should be used
        ///         with caution.
        ///     </para>
        /// </remarks>
        public delegate void OnProgressWithCancellationDelegate(TProgress progress, CancellationToken cancellationToken);

        /// <summary>
        ///     Delegate of method for creating a task the allows for updating subscribers of it's progress, and ultimately returns
        ///     a result.
        /// </summary>
        /// <param name="notifyProgress">The notify progress method.</param>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     <para>
        ///         Calling the <paramref name="notifyProgress" /> method will update subscribers of the task's current progress.
        ///         It can be called repeatedly whilst the task is running.
        ///     </para>
        /// </remarks>
        public delegate TResult ProgressTaskDelegate(NotifyProgressDelegate notifyProgress);

        /// <summary>
        ///     Delegate of method for creating a task the allows for updating subscribers of it's progress, and ultimately returns
        ///     a result.
        /// </summary>
        /// <param name="notifyProgress">The notify progress method.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task result.</returns>
        /// <remarks>
        ///     Calling the <paramref name="notifyProgress" /> method will update subscribers of the task's current progress.
        ///     It can be called repeatedly whilst the task is running.
        /// </remarks>
        public delegate TResult ProgressTaskWithCancellationDelegate(
            NotifyProgressDelegate notifyProgress,
            CancellationToken cancellationToken);

        /// <summary>
        ///     The actions (and their preferred <see cref="ITaskScheduler" /> to call when a task makes progress.
        /// </summary>
        private readonly Queue<KeyValuePair<OnProgressDelegate, ITaskScheduler>> _onProgress =
            new Queue<KeyValuePair<OnProgressDelegate, ITaskScheduler>>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task{TResult, TProgress}" /> class.
        /// </summary>
        /// <param name="function">The function to call when the task is run.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task(
            ProgressTaskDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken)) : base(scheduler, cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    SetResult(function(Notify));
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Task{TResult, TProgress}" /> class.
        /// </summary>
        /// <param name="function">The function to call when the task is run.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task(
            ProgressTaskWithCancellationDelegate function,
            ITaskScheduler scheduler = null,
            CancellationToken cancellationToken = default(CancellationToken)) : base(scheduler, cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Action = () =>
            {
                if (!ChangeState(TaskStatus.Running))
                    return;
                try
                {
                    SetResult(function(Notify, cancellationToken));
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            };
        }

        public Task(ITaskScheduler scheduler = null, CancellationToken cancellationToken = default(CancellationToken))
            : base(scheduler, cancellationToken)
        {
        }

        /// <summary>
        ///     Gets the last progress reported by the task.
        /// </summary>
        /// <value>The last progress.</value>
        public TProgress LatestProgress { get; private set; }

        /// <summary>
        ///     Add an action that will be executed (on the specified <paramref name="scheduler" /> whenever the task's
        ///     function calls the notify action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler (defaults to <see cref="Task.Scheduler">the task's scheduler</see>.</param>
        /// <returns>This <see cref="Task{TResult, TProgress}" /></returns>
        /// <remarks>
        ///     <para>
        ///         Note, unlike the <see cref="Task.OnSuccess(Task.TaskDelegate, ITaskScheduler" /> and
        ///         <see cref="Task.OnFailure(Task.FailedTaskDelegate, ITaskScheduler" />
        ///         methods (and overloads) this method returns this task and not a new task.  This is because the action may be
        ///         called multiple times,
        ///         as progress is reported, and so there is no single task that represents the progress notification tasks that
        ///         are created.
        ///     </para>
        /// </remarks>
        public ITask<TResult, TProgress> OnProgress(
            OnProgressWithCancellationDelegate action,
            ITaskScheduler scheduler = null)
        {
            return OnProgress(p => action(p, Token), scheduler);
        }

        /// <summary>
        ///     Add an action that will be executed (on the specified <paramref name="scheduler" /> whenever the task's
        ///     function calls the notify action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler (defaults to <see cref="Task.Scheduler">the task's scheduler</see>.</param>
        /// <returns>This <see cref="Task{TResult, TProgress}" /></returns>
        /// <remarks>
        ///     <para>
        ///         Note, unlike the <see cref="Task.OnSuccess(Task.TaskDelegate, ITaskScheduler" /> and
        ///         <see cref="Task.OnFailure(Task.FailedTaskDelegate, ITaskScheduler" />
        ///         methods (and overloads) this method returns this task and not a new task.  This is because the action may be
        ///         called multiple times,
        ///         as progress is reported, and so there is no single task that represents the progress notification tasks that
        ///         are created.
        ///     </para>
        /// </remarks>
        public ITask<TResult, TProgress> OnProgress(OnProgressDelegate action, ITaskScheduler scheduler = null)
        {
            // We add the actions and schedulers into the queue rather than the task as each action can be used to create
            // multiple tasks.
            lock (_onProgress)
                if (State <= TaskStatus.Running)
                    _onProgress.Enqueue(new KeyValuePair<OnProgressDelegate, ITaskScheduler>(action, scheduler));

            return this;
        }

        public new ITask<TResult, TProgress> Run()
        {
            return (ITask<TResult, TProgress>) base.Run();
        }

        /// <summary>
        ///     Called by the task function whenever it wishes to notify subscribers of progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        private void Notify(TProgress progress)
        {
            if ((_onProgress.Count < 1) ||
                (State != TaskStatus.Running))
                return;

            // Update the last progress
            LatestProgress = progress;

            KeyValuePair<OnProgressDelegate, ITaskScheduler>[] actions;
            lock (_onProgress)
            {
                if (_onProgress.Count < 1)
                    return;
                actions = _onProgress.ToArray();
            }

            Close();

            // Create tasks for actions and then run
            foreach (Task task in actions.Select(kvp => new Task(() => kvp.Key(progress), kvp.Value, Token))
                // We to array to drive the select statement before iterating.
                .ToArray())
                task.TryRun();
        }

        protected override void Close()
        {
            if (_onProgress.Count > 0)
                lock (_onProgress)
                    _onProgress.Clear();
            base.Close();
        }
    }
}
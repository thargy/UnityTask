using System;

namespace Apocraphy.Assets.Scripts.Threading
{
    public interface ITask
    {
        /// <summary>
        /// Gets the Task's unique identifier.
        /// </summary>
        /// <value>The identifier.</value>
        long Id { get; }

        /// <summary>
        /// Gets the exception, if the task failed.
        /// </summary>
        /// <value>The exception.</value>
        Exception Exception { get; }

        object Result { get; }
        bool IsCancelled { get; }
        bool IsRunning { get; }
        bool IsCompleted { get; }
        TaskStatus State { get; }

        /// <summary>
        ///     The associated cancellation token, if any; otherwise <see cref="Threading.CancellationToken.None"/>.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// The scheduler that the task is scheduled to run on.
        /// </summary>
        /// <value>The scheduler.</value>
        ITaskScheduler Scheduler { get; }

        /// <summary>
        /// Adds a delay following success of the task.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        ITask Wait(TimeSpan delay);

        /// <summary>
        /// Adds a delay following success of the task.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        ITask Wait(int millisecondsDelay);

        /// <summary>
        /// Cancels this instance.
        /// </summary>
        void Cancel();

        ITask OnSuccess(Task.TaskDelegate action, ITaskScheduler scheduler = null);
        ITask OnSuccess(Task.TaskWithCancellationDelegate action, ITaskScheduler scheduler = null);
        ITask<TResult> OnSuccess<TResult>(Task<TResult>.ResultTaskDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult> OnSuccess<TResult>(Task<TResult>.ResultTaskWithCancellationDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult, TProgress> OnSuccess<TResult, TProgress>(Task<TResult, TProgress>.ProgressTaskDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult, TProgress> OnSuccess<TResult, TProgress>(Task<TResult, TProgress>.ProgressTaskWithCancellationDelegate function, ITaskScheduler scheduler = null);
        ITask OnFailure(Task.FailedTaskDelegate action, ITaskScheduler scheduler = null);
        ITask OnFailure(Task.FailedTaskWithCancellationDelegate action, ITaskScheduler scheduler = null);
        ITask<TResult> OnFailure<TResult>(Task<TResult>.FailedResultTaskDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult> OnFailure<TResult>(Task<TResult>.FailedResultTaskWithCancellationDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult, TProgress> OnFailure<TResult, TProgress>(Task<TResult, TProgress>.FailedProgressTaskDelegate function, ITaskScheduler scheduler = null);
        ITask<TResult, TProgress> OnFailure<TResult, TProgress>(Task<TResult, TProgress>.FailedProgressTaskWithCancellationDelegate function, ITaskScheduler scheduler = null);
    }


    public interface ITask<TResult> : ITask
    {
        new TResult Result { get; }

        /// <summary>
        /// Adds a delay following success of the task.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        new ITask<TResult> Wait(TimeSpan delay);

        /// <summary>
        /// Adds a delay following success of the task.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <returns>A new Task that succeeds after a delay.</returns>
        new ITask<TResult> Wait(int millisecondsDelay);

        ITask OnSuccess(Task<TResult>.ContinuationTaskDelegate action, ITaskScheduler scheduler = null);
        ITask OnSuccess(Task<TResult>.ContinuationTaskWithCancellationDelegate action, ITaskScheduler scheduler = null);
        ITask<T> OnSuccess<T>(Task<TResult>.ContinuationResultTaskDelegate<T> function, ITaskScheduler scheduler = null);
        ITask<T> OnSuccess<T>(Task<TResult>.ContinuationResultTaskWithCancellationDelegate<T> function, ITaskScheduler scheduler = null);
        ITask<T, TProgress> OnSuccess<T, TProgress>(Task<T, TProgress>.ContinuationProgressTaskDelegate<TResult> function, ITaskScheduler scheduler = null);
        ITask<T, TProgress> OnSuccess<T, TProgress>(Task<T, TProgress>.ContinuationProgressTaskWithCancellationDelegate<TResult> function, ITaskScheduler scheduler = null);
    }

    public interface ITask<TResult, TProgress> : ITask<TResult>
    {
        /// <summary>
        ///     Gets the last progress reported by the task.
        /// </summary>
        /// <value>The last progress.</value>
        TProgress LatestProgress { get; }

        /// <summary>
        /// Add an action that will be executed (on the specified <paramref name="scheduler"/> whenever the task's
        /// function calls the notify action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler (defaults to <see cref="Task.Scheduler">the task's scheduler</see>.</param>
        /// <returns>This <see cref="Task{TResult, TProgress}"/></returns>
        /// <remarks><para>Note, unlike the <see cref="Task.OnSuccess(Task.TaskDelegate, ITaskScheduler"/> and <see cref="Task.OnFailure(Task.FailedTaskDelegate, ITaskScheduler"/>
        /// methods (and overloads) this method returns this task and not a new task.  This is because the action may be called multiple times,
        /// as progress is reported, and so there is no single task that represents the progress notification tasks that are created.</para></remarks>
        ITask<TResult, TProgress> OnProgress(Task<TResult, TProgress>.OnProgressWithCancellationDelegate action, ITaskScheduler scheduler = null);

        /// <summary>
        /// Add an action that will be executed (on the specified <paramref name="scheduler"/> whenever the task's
        /// function calls the notify action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="scheduler">The scheduler (defaults to <see cref="Task.Scheduler">the task's scheduler</see>.</param>
        /// <returns>This <see cref="Task{TResult, TProgress}"/></returns>
        /// <remarks><para>Note, unlike the <see cref="Task.OnSuccess(Task.TaskDelegate, ITaskScheduler"/> and <see cref="Task.OnFailure(Task.FailedTaskDelegate, ITaskScheduler"/>
        /// methods (and overloads) this method returns this task and not a new task.  This is because the action may be called multiple times,
        /// as progress is reported, and so there is no single task that represents the progress notification tasks that are created.</para></remarks>
        ITask<TResult, TProgress> OnProgress(Task<TResult, TProgress>.OnProgressDelegate action, ITaskScheduler scheduler = null);
    }
}
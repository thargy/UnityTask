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
using System.Diagnostics;

namespace Thargy.UnityTask
{
    /// <summary>
    ///     Propagates notification that operations should be cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A <see cref="CancellationToken" /> may be created directly in an unchangeable cancelled or non-cancelled state
    ///         using the CancellationToken's constructors. However, to have a Token that can change
    ///         from a non-cancelled to a cancelled state,
    ///         <see cref="CancellationTokenSource">CancellationTokenSource</see> must be used.
    ///         CancellationTokenSource exposes the associated CancellationToken that may be cancelled by the source through
    ///         its
    ///         <see cref="CancellationTokenSource.Token">Token</see> property.
    ///     </para>
    ///     <para>
    ///         Once cancelled, a token may not transition to a non-cancelled state, and a token whose
    ///         <see cref="CanBeCancelled" /> is false will never change to one that can be cancelled.
    ///     </para>
    ///     <para>
    ///         All members of this struct are thread-safe and may be used concurrently from multiple threads.
    ///     </para>
    /// </remarks>
    [DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
    public struct CancellationToken
    {
        // The backing TokenSource.  
        // if null, it implicitly represents the same thing as new CancellationToken(false).
        // When required, it will be instantiated to reflect this.
        private readonly CancellationTokenSource _source;

        /// <summary>
        ///     Returns an empty CancellationToken value.
        /// </summary>
        /// <remarks>
        ///     The <see cref="CancellationToken" /> value returned by this property will be non-cancellable by default.
        /// </remarks>
        public static CancellationToken None
        {
            get { return default(CancellationToken); }
        }

        /// <summary>
        ///     Gets whether cancellation has been requested for this token.
        /// </summary>
        /// <value>Whether cancellation has been requested for this token.</value>
        /// <remarks>
        ///     <para>
        ///         This property indicates whether cancellation has been requested for this token,
        ///         either through the token initially being construted in a cancelled state, or through
        ///         calling <see cref="CancellationTokenSource.Cancel()">Cancel</see>
        ///         on the token's associated <see cref="CancellationTokenSource" />.
        ///     </para>
        ///     <para>
        ///         If this property is true, it only guarantees that cancellation has been requested.
        ///         It does not guarantee that every registered handler
        ///         has finished executing, nor that cancellation requests have finished propagating
        ///         to all registered handlers.  Additional synchronization may be required,
        ///         particularly in situations where related objects are being cancelled concurrently.
        ///     </para>
        /// </remarks>
        public bool IsCancellationRequested
        {
            get { return (_source != null) && _source.IsCancellationRequested; }
        }

        /// <summary>
        ///     Gets whether this token is capable of being in the cancelled state.
        /// </summary>
        /// <remarks>
        ///     If CanBeCancelled returns false, it is guaranteed that the token will never transition
        ///     into a cancelled state, meaning that <see cref="IsCancellationRequested" /> will never
        ///     return true.
        /// </remarks>
        public bool CanBeCancelled
        {
            get { return (_source != null) && _source.CanBeCancelled; }
        }

        // public CancellationToken()
        // this constructor is implicit for structs
        //   -> this should behaves exactly as for new CancellationToken(false)

        /// <summary>
        ///     Internal constructor only a CancellationTokenSource should create a CancellationToken
        /// </summary>
        internal CancellationToken(CancellationTokenSource source)
        {
            _source = source;
        }

        /// <summary>
        ///     Initializes the <see cref="T:System.Threading.CancellationToken">Token</see>.
        /// </summary>
        /// <param name="cancelled">
        ///     The cancelled state for the token.
        /// </param>
        /// <remarks>
        ///     Tokens created with this constructor will remain in the cancelled state specified
        ///     by the <paramref name="cancelled" /> parameter.  If <paramref name="cancelled" /> is false,
        ///     both <see cref="CanBeCancelled" /> and <see cref="IsCancellationRequested" /> will be false.
        ///     If <paramref name="cancelled" /> is true,
        ///     both <see cref="CanBeCancelled" /> and <see cref="IsCancellationRequested" /> will be true.
        /// </remarks>
        public CancellationToken(bool cancelled) :
            this()
        {
            _source = cancelled ? CancellationTokenSource.Cancelled : CancellationTokenSource.NotCancellable;
        }

        /// <summary>
        ///     Determines whether the current <see cref="T:System.Threading.CancellationToken">Token</see> instance is
        ///     equal to the
        ///     specified token.
        /// </summary>
        /// <param name="other">
        ///     The other <see cref="T:System.Threading.CancellationToken">Token</see> to which to compare this
        ///     instance.
        /// </param>
        /// <returns>
        ///     True if the instances are equal; otherwise, false. Two tokens are equal if they are associated
        ///     with the same <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> or if they were
        ///     both constructed
        ///     from public CancellationToken constructors and their <see cref="IsCancellationRequested" /> values are equal.
        /// </returns>
        public bool Equals(CancellationToken other)
        {
            //if both sources are null, then both tokens represent the Empty token.
            if ((_source == null) &&
                (other._source == null))
                return true;

            // one is null but other has inflated the default source
            // these are only equal if the inflated one is the staticSource(false)
            if (_source == null)
                return other._source == CancellationTokenSource.NotCancellable;

            if (other._source == null)
                return _source == CancellationTokenSource.NotCancellable;

            // general case, we check if the sources are identical

            return _source == other._source;
        }

        /// <summary>
        ///     Determines whether the current <see cref="T:System.Threading.CancellationToken">Token</see> instance is
        ///     equal to the
        ///     specified <see cref="T:System.Object" />.
        /// </summary>
        /// <param name="other">The other object to which to compare this instance.</param>
        /// <returns>
        ///     True if <paramref name="other" /> is a <see cref="T:System.Threading.CancellationToken">Token</see>
        ///     and if the two instances are equal; otherwise, false. Two tokens are equal if they are associated
        ///     with the same <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> or if they were
        ///     both constructed
        ///     from public CancellationToken constructors and their <see cref="IsCancellationRequested" /> values are equal.
        /// </returns>
        /// <exception cref="T:System.ObjectDisposedException">
        ///     An associated
        ///     <see
        ///         cref="T:System.Threading.CancellationTokenSource">
        ///         CancellationTokenSource
        ///     </see>
        ///     has been disposed.
        /// </exception>
        public override bool Equals(object other)
        {
            if (other is CancellationToken)
                return Equals((CancellationToken) other);

            return false;
        }

        /// <summary>
        ///     Serves as a hash function for a <see cref="T:System.Threading.CancellationToken">Token</see>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="T:System.Threading.CancellationToken">Token</see> instance.</returns>
        public override int GetHashCode()
        {
            return _source == null ? CancellationTokenSource.NotCancellable.GetHashCode() : _source.GetHashCode();
        }

        /// <summary>
        ///     Determines whether two <see cref="T:System.Threading.CancellationToken">Token</see> instances are
        ///     equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">
        ///     An associated
        ///     <see
        ///         cref="T:System.Threading.CancellationTokenSource">
        ///         CancellationTokenSource
        ///     </see>
        ///     has been disposed.
        /// </exception>
        public static bool operator ==(CancellationToken left, CancellationToken right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Determines whether two <see cref="T:System.Threading.CancellationToken">Token</see> instances are not
        ///     equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">
        ///     An associated
        ///     <see
        ///         cref="T:System.Threading.CancellationTokenSource">
        ///         CancellationTokenSource
        ///     </see>
        ///     has been disposed.
        /// </exception>
        public static bool operator !=(CancellationToken left, CancellationToken right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///     Throws a <see cref="T:System.OperationCancelledException">OperationCancelledException</see> if
        ///     this token has had cancellation requested.
        /// </summary>
        /// <remarks>
        ///     This method provides functionality equivalent to:
        ///     <code>
        /// if (token.IsCancellationRequested) 
        ///    throw new OperationCancelledException(token);
        /// </code>
        /// </remarks>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                throw new OperationCanceledException("The operation was cancelled.");
        }
    }
}
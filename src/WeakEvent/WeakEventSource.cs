using System;
using System.Collections.Generic;
using System.Linq;
using WN.DependencyInjection;
using WN.Utils.Contract.Interfaces;
using static WeakEvent.WeakEventSourceHelper;

namespace WeakEvent
{
    /// <summary>
    /// An event with weak subscription, i.e. it won't keep handlers from being garbage collected.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event's arguments.</typeparam>
    public class WeakEventSource<TEventArgs>
#if NET40
        where TEventArgs : EventArgs
#endif
    {

        private static IElevatorService? elevatorService;
        internal DelegateCollection? _handlers;

        public WeakEventSource()
        {
        }
        private void checkElevator()
        {
            if (ServiceProviderLocator.ServiceProvider != null && elevatorService == null)
            {
                elevatorService = ServiceProviderLocator.ServiceProvider.GetService(typeof(IElevatorService)) as IElevatorService;
            }
        }


        /// <summary>
        /// Raises the event by invoking each handler that hasn't been garbage collected.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">An object that contains the event data.</param>
        /// <remarks>The handlers are invoked one after the other, in the order they were subscribed in.</remarks>
        public void Raise(object? sender, TEventArgs args, IEnumerable<Delegate>? excludedHandler = null)
        {
            checkElevator();
            var validHandlers = GetValidHandlers(_handlers, excludedHandler);
            foreach (StrongHandler handler in validHandlers)
            {
                
                if (elevatorService == null)
                    handler.Invoke(sender, args);
                else
                    elevatorService?.Elevat(handler.Target.GetType().Module.Name, (a) => handler.Invoke(sender, args));
            }
        }

        /// <summary>
        /// Raises the event by invoking each handler that hasn't been garbage collected. Exceptions thrown by
        /// individual handlers are passed to the specified <c>exceptionHandler</c> to decide what to do with them.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">An object that contains the event data.</param>
        /// <param name="exceptionHandler">A delegate that handles exceptions thrown by individual handlers.
        /// Return <c>true</c> to indicate that the exception was handled.</param>
        /// <remarks>The handlers are invoked one after the other, in the order they were subscribed in.</remarks>
        public void Raise(object? sender, TEventArgs args, Func<Exception, bool> exceptionHandler)
        {
            checkElevator();
            if (exceptionHandler is null) throw new ArgumentNullException(nameof(exceptionHandler));
            var validHandlers = GetValidHandlers(_handlers);
            foreach (var handler in validHandlers)
            {
                try
                {
                    if (elevatorService == null)
                        handler.Invoke(sender, args);
                    else
                        elevatorService?.Elevat(handler.Target.GetType().Module.Name, (a) => handler.Invoke(sender, args));
                }
                catch (Exception ex) when (exceptionHandler(ex))
                {
                }
            }
        }

        /// <summary>
        /// Removes all event handlers.
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// Adds an event handler.
        /// </summary>
        /// <param name="handler">The handler to subscribe.</param>
        /// <remarks>Only a weak reference to the handler's <c>Target</c> is kept, so that it can be garbage collected.</remarks>
        public void Subscribe(EventHandler<TEventArgs> handler, Boolean uniqueRegistration = false)
        {
            Subscribe(null, handler, uniqueRegistration);
        }

        /// <summary>
        /// Adds an event handler, specifying a lifetime object.
        /// </summary>
        /// <param name="lifetimeObject">An object that keeps the handler alive as long as it's alive.</param>
        /// <param name="handler">The handler to subscribe.</param>
        /// <remarks>Only a weak reference to the handler's <c>Target</c> is kept, so that it can be garbage collected.
        /// However, as long as the <c>lifetime</c> object is alive, the handler will be kept alive. This is useful for
        /// subscribing with anonymous methods (e.g. lambda expressions).</remarks>
        public void Subscribe(object? lifetimeObject, EventHandler<TEventArgs> handler, Boolean uniqueRegistration = false)
        {
            Subscribe<DelegateCollection, OpenEventHandler, StrongHandler>(lifetimeObject, ref _handlers, handler, uniqueRegistration);
        }

        /// <summary>
        /// Removes an event handler.
        /// </summary>
        /// <param name="handler">The handler to unsubscribe.</param>
        /// <remarks>The behavior is the same as that of <see cref="Delegate.Remove(Delegate, Delegate)"/>. Only the last instance
        /// of the handler's invocation list is removed. If the exact invocation list is not found, nothing is removed.</remarks>
        public void Unsubscribe(EventHandler<TEventArgs> handler)
        {
            Unsubscribe<OpenEventHandler, StrongHandler>(_handlers, handler);
        }

        /// <summary>
        /// Removes an event handler that was subscribed with a lifetime object.
        /// </summary>
        /// <param name="lifetimeObject">The lifetime object that was associated with the handler.</param>
        /// <param name="handler">The handler to unsubscribe.</param>
        /// <remarks>The behavior is the same as that of <see cref="Delegate.Remove(Delegate, Delegate)"/>. Only the last instance
        /// of the handler's invocation list is removed. If the exact invocation list is not found, nothing is removed.</remarks>
        [Obsolete("This method is obsolete and will be removed in a future version. Use the Unsubscribe overload that doesn't take a lifetime object instead.")]
        public void Unsubscribe(object? lifetimeObject, EventHandler<TEventArgs> handler)
        {
            Unsubscribe(handler);
        }

        internal delegate void OpenEventHandler(object? target, object? sender, TEventArgs e);

        internal struct StrongHandler
        {
            public readonly object? Target { get; }
            public readonly OpenEventHandler OpenHandler { get; }

            public StrongHandler(object? target, OpenEventHandler openHandler)
            {
                Target = target;
                OpenHandler = openHandler;
            }

            public void Invoke(object? sender, TEventArgs e)
            {
                OpenHandler(Target, sender, e);
            }
        }

        internal class DelegateCollection : DelegateCollectionBase<OpenEventHandler, StrongHandler>
        {
            public DelegateCollection()
                : base((target, openHandler) => new StrongHandler(target, openHandler))
            {
            }
        }
    }
}

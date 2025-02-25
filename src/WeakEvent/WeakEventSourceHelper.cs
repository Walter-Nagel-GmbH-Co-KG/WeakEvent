using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WeakEvent
{
    internal static class WeakEventSourceHelper
    {
        public static IEnumerable<TStrongHandler> GetValidHandlers<TOpenEventHandler, TStrongHandler>(
            DelegateCollectionBase<TOpenEventHandler, TStrongHandler>? handlers,
            IEnumerable<Delegate>? excludedHandler = null)
            where TOpenEventHandler : Delegate
            where TStrongHandler : struct
        {
            if (handlers is null)
                return Enumerable.Empty<TStrongHandler>();

            List<TStrongHandler> validHandlers;
            lock (handlers)
            {
                validHandlers = new List<TStrongHandler>(handlers.Count);
                for (int i = 0; i < handlers.Count; i++)
                {
                    var weakHandler = handlers[i];
                    if (weakHandler is {})
                    {
                        if(excludedHandler != null &&
                            excludedHandler.Any(handler => weakHandler.IsMatch(handler)))
                            continue;
                        if (weakHandler.TryGetStrongHandler() is TStrongHandler handler)
                            validHandlers.Add(handler);
                        else
                            handlers.Invalidate(i);
                    }
                }

                handlers.CompactHandlerList();
                handlers.ResetDeadHandlerScanCountdown();
            }

            return validHandlers;
        }

        public static void Subscribe<TDelegateCollection, TOpenEventHandler, TStrongHandler>(
            object? lifetimeObject,
            ref TDelegateCollection? handlers,
            Delegate handler,
            Boolean uniqueRegistation = false)
            where TDelegateCollection : DelegateCollectionBase<TOpenEventHandler, TStrongHandler>, new()
            where TOpenEventHandler : Delegate
            where TStrongHandler : struct
        {
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var invocationList = handler.GetInvocationList();

            LazyInitializer.EnsureInitialized(ref handlers);
            lock (handlers!)
            {
                handlers.Add(lifetimeObject, invocationList, uniqueRegistation);
            }
        }

        public static void Unsubscribe<TOpenEventHandler, TStrongHandler>(
            DelegateCollectionBase<TOpenEventHandler, TStrongHandler>? handlers,
            Delegate handler)
            where TOpenEventHandler : Delegate
            where TStrongHandler : struct
        {
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            if (handlers is null)
                return;

            var invocationList = handler.GetInvocationList();

            lock (handlers)
            {
                handlers.Remove(invocationList);
                handlers.CompactHandlerList();
            }
        }
    }
}

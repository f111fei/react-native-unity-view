using System;

namespace ReactNative
{
    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs() { }

        public bool Handled { get; set; }
    }

    public class RoutedEventArgs<T> : RoutedEventArgs
    {
        public RoutedEventArgs(T data)
        {
            this.Data = data;
        }

        public T Data { get; }
    }

    public static class RoutedEventArgsHelper
    {
        public static void Invoke<TSender>(this EventHandler<RoutedEventArgs> @event, TSender sender)
        {
            var invocationList = @event?.GetInvocationList();
            if (invocationList != null)
            {
                RoutedEventArgs routedArgs = null;
                foreach (EventHandler<RoutedEventArgs> @delegate in invocationList)
                {
                    if (routedArgs == null)
                    {
                        routedArgs = new RoutedEventArgs();
                    }

                    @delegate.Invoke(sender, routedArgs);

                    if (routedArgs.Handled)
                    {
                        break;
                    }
                }
            }
        }

        public static void Invoke<TSender, TArgs>(this EventHandler<RoutedEventArgs<TArgs>> @event, TSender sender, TArgs args)
        {
            var invocationList = @event?.GetInvocationList();
            if (invocationList != null)
            {
                RoutedEventArgs<TArgs> routedArgs = null;
                foreach (EventHandler<RoutedEventArgs<TArgs>> @delegate in invocationList)
                {
                    if (routedArgs == null)
                    {
                        routedArgs = new RoutedEventArgs<TArgs>(args);
                    }

                    @delegate.Invoke(sender, routedArgs);

                    if (routedArgs.Handled)
                    {
                        break;
                    }
                }
            }
        }
    }
}

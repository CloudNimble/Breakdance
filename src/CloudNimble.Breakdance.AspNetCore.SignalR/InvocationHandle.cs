using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.AspNetCore.SignalR
{
    /// <summary>
    /// Record for the types, handlers and the invocation state of a HubConnection handler.
    /// </summary>
    /// <param name="ParameterTypes"></param>
    /// <param name="Handler"></param>
    /// <param name="State"></param>
    public record InvocationHandle(Type[] ParameterTypes, Func<object[], object, Task> Handler, object State);
}

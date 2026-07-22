using System;
using System.Threading.Tasks;

namespace ChickenAPI.Events
{
    public delegate Task AsyncTypedSenderEventHandler<in TSender, in TEventArgs>(TSender sender, TEventArgs e)
        where TSender : class
        where TEventArgs : EventArgs;
}
using Frostvein.Core;
using Frostvein.GameObject._Event;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri
{
    public class BaseGuriHandler : IGuriHandlerContainer
    {
        protected readonly Dictionary<long, IGuriHandler> HandlersByDialogId;

        public BaseGuriHandler()
        {
            HandlersByDialogId = new Dictionary<long, IGuriHandler>();
        }

        public async Task Register(IGuriHandler handler)
        {
            if (HandlersByDialogId.ContainsKey(handler.GuriEffectId)) return;

            HandlersByDialogId.Add(handler.GuriEffectId, handler);
        }

        public async Task Unregister(long guriEffectId)
        {
            HandlersByDialogId.Remove(guriEffectId);
        }

        public void Handle(EventEntity player, GuriEvent args)
        {
            if (!HandlersByDialogId.TryGetValue(args.Type, out var handler))
            {
                Logger.Log.Debug($"[HANDLER_NOT_FOUND] GURI_EFFECT : {args.Type} ");
                return;
            }

            handler.Execute(player.Character.Session, args);
        }
    }
}
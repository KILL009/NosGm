using FluentValidation;
using MediatR;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Modules.Bazaar.Queries;
using NosGm.GameObject.Networking;
using NosTale.Module.Bazaar;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetRcsList
{
    internal class GetRcsListQueryHandler : IRequestHandler<GetRcsListQuery, string>
    {
        private readonly GetRcsListQueryValidator _requestValidator = new();
        private readonly BazaarManager _bazaarManager;

        public GetRcsListQueryHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<string> Handle(GetRcsListQuery request, CancellationToken cancellation)
        {
            await _requestValidator.ValidateAndThrowAsync(request);

            string list = "";

            foreach (BazaarItemLink bz in _bazaarManager.BazaarItemLinks.Values.Where(s => s != null && (s.BazaarItem.DateStart.AddHours(s.BazaarItem.Duration).AddDays(s.BazaarItem.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes > 0 && s.BazaarItem.SellerId == request.Model.CharacterId).Skip(request.Model.Packet.Index * 50).Take(50))
            {
                if (bz.Item != null)
                {
                    int soldedAmount = bz.BazaarItem.Amount - bz.Item.Amount;
                    int amount = bz.BazaarItem.Amount;
                    bool package = bz.BazaarItem.IsPackage;
                    bool isNosbazar = bz.BazaarItem.MedalUsed;
                    long price = bz.BazaarItem.Price;
                    long minutesLeft = (long)(bz.BazaarItem.DateStart.AddHours(bz.BazaarItem.Duration) - DateTime.Now).TotalMinutes;
                    byte Status = minutesLeft >= 0 ? (soldedAmount < amount ? (byte)BazaarType.OnSale : (byte)BazaarType.Solded) : (byte)BazaarType.DelayExpired;
                    if (Status == (byte)BazaarType.DelayExpired)
                    {
                        minutesLeft = (long)(bz.BazaarItem.DateStart.AddHours(bz.BazaarItem.Duration).AddDays(isNosbazar ? 30 : 7) - DateTime.Now).TotalMinutes;
                    }

                    var itm = bz.Item.Item ?? ServerManager.GetItem(bz.Item.ItemVNum);

                    string info = "";
                    if (itm?.Type == InventoryType.Equipment)
                    {
                        info = BazaarEquipmentInfoGenerator.Generate(bz.Item);
                    }
                    if (request.Model.Packet.Filter == 0 || request.Model.Packet.Filter == Status)
                    {
                        list += $"{bz.BazaarItem.BazaarItemId}|{bz.BazaarItem.SellerId}|{bz.Item.ItemVNum}|{soldedAmount}|{amount}|{(package ? 1 : 0)}|{price}|{Status}|{minutesLeft}|{(isNosbazar ? 1 : 0)}|0|{bz.Item.Rare}|{bz.Item.Upgrade}|0|0|{info} ";
                    }
                }
            }

            return $"rc_slist {request.Model.Packet.Index} 0 {list}";
        }
    }
}

using FluentValidation;
using MediatR;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
//using Frostvein.GameObject.Item.Instance;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Commands.InsertOrUpdateBazaarItem
{
    internal class InsertOrUpdateBazaarItemCommandHandler : IRequestHandler<InsertOrUpdateBazaarItemCommand, long>
    {
        private readonly InsertOrUpdateBazaarItemCommandValidator _commandValidator = new();
        private readonly BazaarManager _bazaarManager;

        public InsertOrUpdateBazaarItemCommandHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<long> Handle(InsertOrUpdateBazaarItemCommand command, CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command);

            if (command.BazaarItem == null)
            {
                return -1;
            }

            BazaarItemDTO item = command.BazaarItem;

            DAOFactory.BazaarItemDAO.InsertOrUpdate(ref item);

            var exists = _bazaarManager.BazaarItems.ContainsKey(item.BazaarItemId);

            if (!exists)
            {
                _bazaarManager.BazaarItems.TryAdd(item.BazaarItemId, item);
                _bazaarManager.BazaarItemLinks.TryAdd(item.BazaarItemId, new BazaarItemLink
                {
                    BazaarItem = item,
                    Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(item.ItemInstanceId)),
                    Owner = DAOFactory.CharacterDAO.LoadById(item.SellerId)?.Name
                });
                Console.WriteLine($"Inserting item: {item.BazaarItemId}. CharacterId: {item.SellerId}");
            }
            else
            {
                _bazaarManager.BazaarItems[item.BazaarItemId] = item;
                _bazaarManager.BazaarItemLinks[item.BazaarItemId] = new BazaarItemLink
                {
                    BazaarItem = item,
                    Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(item.ItemInstanceId)),
                    Owner = DAOFactory.CharacterDAO.LoadById(item.SellerId)?.Name
                };

                Console.WriteLine($"Updating item: {item.BazaarItemId}. CharacterId: {item.SellerId}");
            }

            return item.BazaarItemId;
        }
    }
}
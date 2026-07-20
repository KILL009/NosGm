using FluentValidation;
using MediatR;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using System;
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
            if (!command.RefreshOnly)
            {
                DAOFactory.BazaarItemDAO.InsertOrUpdate(ref item);
            }

            long bazaarItemId = item.BazaarItemId;
            lock (_bazaarManager.GetItemLock(bazaarItemId))
            {
                if (command.RefreshOnly)
                {
                    item = DAOFactory.BazaarItemDAO.LoadById(bazaarItemId);
                    if (item == null)
                    {
                        _bazaarManager.BazaarItems.TryRemove(bazaarItemId, out _);
                        _bazaarManager.BazaarItemLinks.TryRemove(bazaarItemId, out _);
                        return -1;
                    }
                }

                var itemDto = DAOFactory.ItemInstanceDAO.LoadById(item.ItemInstanceId);
                if (itemDto == null)
                {
                    _bazaarManager.BazaarItems.TryRemove(bazaarItemId, out _);
                    _bazaarManager.BazaarItemLinks.TryRemove(bazaarItemId, out _);
                    return -1;
                }

                var link = new BazaarItemLink
                {
                    BazaarItem = item,
                    Item = new ItemInstance(itemDto),
                    Owner = DAOFactory.CharacterDAO.LoadById(item.SellerId)?.Name
                };

                bool exists = _bazaarManager.BazaarItems.ContainsKey(bazaarItemId);
                _bazaarManager.BazaarItems[bazaarItemId] = item;
                _bazaarManager.BazaarItemLinks[bazaarItemId] = link;

                Console.WriteLine(exists
                    ? $"Updating item: {bazaarItemId}. CharacterId: {item.SellerId}"
                    : $"Inserting item: {bazaarItemId}. CharacterId: {item.SellerId}");

                return bazaarItemId;
            }
        }
    }
}

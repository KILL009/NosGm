using System;

namespace ChickenAPI.DAL
{
    public interface IUuidDto : IDto
    {
        Guid Id { get; set; }
    }
}
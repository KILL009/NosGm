namespace ChickenAPI.DAL
{
    public interface IMappedDto : IDto
    {
        int Id { get; set; }
    }
}
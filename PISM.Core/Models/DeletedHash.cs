namespace PISM.Core.Models;

public class DeletedHash
{
    public Guid Id { get; set; }
    public string Hash { get; set; } = null!;
    public DateTime DateDeleted { get; set; }
}

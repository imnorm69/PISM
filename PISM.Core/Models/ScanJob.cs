using PISM.Core.Enums;

namespace PISM.Core.Models;

public class ScanJob
{
    public Guid Id { get; set; }
    public string FolderPath { get; set; } = null!;
    public DateTime DateStarted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public ScanJobStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int NewFiles { get; set; }
    public int DuplicatesFound { get; set; }
    public string? ErrorMessage { get; set; }
}

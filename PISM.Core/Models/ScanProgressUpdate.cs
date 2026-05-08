using PISM.Core.Enums;

namespace PISM.Core.Models;

public class ScanProgressUpdate
{
    public Guid JobId { get; set; }
    public string FolderPath { get; set; } = null!;
    public ScanJobStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int NewFiles { get; set; }
    public int DuplicatesFound { get; set; }
    public string? CurrentFileName { get; set; }
}

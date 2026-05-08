namespace PISM.Core.Options;

public class TestingOptions
{
    public const string Section = "Testing";

    /// <summary>
    /// When true: original files are not deleted after encryption and a
    /// _pism_scanned.txt sidecar is written to each scanned folder.
    /// Also enables the /admin/reset endpoint.
    /// </summary>
    public bool PreserveOriginals { get; set; }
}

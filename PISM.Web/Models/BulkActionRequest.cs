namespace PISM.Web.Models;

public class BulkActionRequest
{
    public List<Guid> Ids { get; set; } = [];
    public string Action { get; set; } = string.Empty;
    public string? Tag { get; set; }
}

public class SaveCropRequest
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int Rotation { get; set; }
}

public class AddTagRequest
{
    public string Tag { get; set; } = string.Empty;
}

public class BulkDownloadRequest
{
    public List<Guid> Ids { get; set; } = [];
}

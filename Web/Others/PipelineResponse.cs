namespace ADF.Web.Others;

public class PipelineResponse
{
    public List<Value> Value { get; set; }
}
public class Value
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public dynamic Properties { get; set; }
    public string Etag { get; set; }
}

public class CreateRunResponse
{
    public string RunId { get; set; }
}
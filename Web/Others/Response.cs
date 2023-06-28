namespace ADF.Web.Others;

public class ResponseBody
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Update information successfully!";
    public string Details { get; set; }
    public object Data { get; set; }
    public ResponseBody Succeed()
    {
        ResponseBody body = new();
        body.Success = true;
        return body;
    }
    public ResponseBody Failed()
    {
        ResponseBody body = new();
        body.Success = false;
        body.Message = "Operation failed!";
        return body;
    }
    public ResponseBody View(object data)
    {
        ResponseBody body = new();
        body.Data = data;
        return body;
    }
}

public class PageResponse<T>
{
    public PageResponse(int totalCount, List<T> items)
    {
        TotalCount = totalCount;
        Items = items;
    }

    public int PageIndex { get; set; }
    public int PageCount { get; set; }


    public int TotalCount { get; set; }
    public List<T> Items { get; set; }
}

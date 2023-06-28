using ADF.Web.Models;
using ADF.Web.Others;

namespace ADF.Web.Services;

public interface IDataServices
{
    ResponseBody UpdatePipeline(Pipeline pipeline,string user);
    ResponseBody RunPipeline(List<int> pipelines, string runBy);
    ResponseBody RunPipelineStandalone(int id, string runBy);
    ResponseBody StartPipeline(List<int> pipelines);
    ResponseBody GetRunnablePipelineList();
    ResponseBody PostPipelines(string user);
    ResponseBody GetPipelineList();
}

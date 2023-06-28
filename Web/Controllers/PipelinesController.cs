using ADF.Web.Models;
using ADF.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADF.Web.Controllers;

public class PipelinesController : BaseController
{
    private readonly IDataServices _dataService;

    public PipelinesController(IDataServices dataServices)
    {
        _dataService = dataServices;
    }

    public ActionResult Index() => View();

    [HttpGet]
    public  IActionResult GetPipelineList() => Ok( _dataService.GetPipelineList());

    [HttpGet]
    public IActionResult GetRunnablePipelineList() => Ok(_dataService.GetRunnablePipelineList());

    [HttpPost]
    public IActionResult UpdatePipeLine(Pipeline pipeline) => Ok(_dataService.UpdatePipeline(pipeline, User.Identity.Name));

    [HttpGet]
    public IActionResult SyncPipeLine() => Ok(_dataService.PostPipelines(User.Identity.Name));

    [HttpPost]
    public IActionResult RunPipeline(List<int> pipelines) => Ok(_dataService.RunPipeline(pipelines, User.Identity.Name));


    [HttpPost]
    public IActionResult RunPipelineStandalone(int pipelineId) => Ok(_dataService.RunPipelineStandalone(pipelineId, User.Identity.Name));

    [HttpPost]
    public IActionResult StartPipeline(List<int> pipelines) => Ok(_dataService.StartPipeline(pipelines));
}

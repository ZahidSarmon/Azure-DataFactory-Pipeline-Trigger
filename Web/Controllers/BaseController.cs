using Microsoft.AspNetCore.Mvc;

namespace ADF.Web.Controllers;

public class BaseController : Controller
{
    protected JsonResult Ok(object? data)
    {
        return Json(data);
    }
}

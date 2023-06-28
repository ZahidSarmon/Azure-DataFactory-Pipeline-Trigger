using ADF.Web.Utilities;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADF.Web.Models;
[Table(nameof(Pipeline), Schema = AppConstants.Schema)]
public class Pipeline : EntityBase<int>
{
    public string Name { get; set; }
    public string? DisplayName { get; set; } = "";
    public bool IsRunnable { get; set; }
    public DateTime? LastRunDate { get; set; }
    public string? LastRunBy { get; set; }
    public string? Status { get; set; }
    public string? LastRunId { get; set; }
}
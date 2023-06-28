using System.ComponentModel.DataAnnotations;

namespace ADF.Web.Models;

public abstract class EntityBase<T>
{
    [Key]
    public T Id { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string LastModifiedBy { get; set; }
}

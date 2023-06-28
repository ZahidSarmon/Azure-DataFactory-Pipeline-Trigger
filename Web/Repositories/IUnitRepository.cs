namespace ADF.Web.Repositories;

public interface IUnitRepository<TEntity> : IMasterRepository<TEntity> where TEntity : class { }

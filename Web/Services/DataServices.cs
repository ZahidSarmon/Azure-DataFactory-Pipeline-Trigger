using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Rest;
using ADF.Web.Others;
using ADF.Web.Models;
using ADF.Web.Data;
using ADF.Web.Repositories;

namespace ADF.Web.Services;

public class DataServices : IDataServices
{
    private readonly AppDbContext _context;
    private readonly ADFConfigure _config;
    private readonly List<string> statusSkip = new() { "InProgress", "Queued" };
    ResponseBody response;
    private readonly IUnitRepository<Pipeline> _repository;
    public DataServices(AppDbContext appDbContext, ADFConfigure config,IUnitRepository<Pipeline> repository)
    {
        _context = appDbContext;
        response = new ResponseBody();
        _config = config;   
        _repository = repository;
    }

    public ResponseBody UpdatePipeline(Pipeline pipeline,string user)
    {
        var data = _repository.GetById(pipeline.Id);

        if (data is null) return response.Failed();

        data.IsRunnable = pipeline.IsRunnable;
        data.LastModifiedBy = user;
        data.LastModifiedOn = DateTime.UtcNow;
        data.DisplayName = pipeline.DisplayName;

        _repository.Update(data);

        if (_repository.UnitOfWork.SaveChanges()>0)
            return response.Succeed();

        return response.Failed();
    }

    public ResponseBody RunPipeline(List<int> pipelines, string runBy)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var token = GetToken();

            if (string.IsNullOrEmpty(token)) return response.Failed();

            var runnable_pipeline = _repository.GetAllBySpec(x => pipelines.Contains(x.Id)
            && (string.IsNullOrEmpty(x.Status) || !statusSkip.Contains(x.Status))).ToList();

            foreach (var item in runnable_pipeline)
            {
                string runId = GetRunId(item.Name);
                item.LastRunId = runId;
                item.LastRunBy = runBy;
                item.LastRunDate = DateTime.UtcNow;
            }

            _context.Pipelines.UpdateRange(runnable_pipeline);
            _context.SaveChanges();

            transaction.CreateSavepoint("RunPipeline");

            var runnable_pipeline_start = _repository.GetAllBySpec(x => pipelines.Contains(x.Id) && (string.IsNullOrEmpty(x.Status) || !statusSkip.Contains(x.Status))).ToList();

            foreach (var item in runnable_pipeline_start)
            {
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(item.LastRunId))
                    item.Status = GetStatusProcess(token, item.LastRunId);
            }

            _context.Pipelines.UpdateRange(runnable_pipeline);
            _context.SaveChanges();

            transaction.CreateSavepoint("Start Pipeline");

            transaction.Commit();

            return response.Succeed();
        }catch(Exception exp) {
            if (transaction!=null)
            {
                transaction.Rollback();
            }
        }
        finally{transaction.Dispose();}
        return response.Failed();
    }

    public ResponseBody RunPipelineStandalone(int id, string runBy)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var token = GetToken();

            if (string.IsNullOrEmpty(token)) return response.Failed();

            var runnable_pipeline = _context.Pipelines.Where(x => x.Id==id && (string.IsNullOrEmpty(x.Status) || !statusSkip.Contains(x.Status))).FirstOrDefault();

            if (runnable_pipeline is null) return response.Failed();

            string runId = GetRunId(runnable_pipeline.Name);
            runnable_pipeline.LastRunId = runId;
            runnable_pipeline.LastRunBy = runBy;
            runnable_pipeline.LastRunDate = DateTime.UtcNow;

            _context.Pipelines.Update(runnable_pipeline);
            _context.SaveChanges();

            transaction.CreateSavepoint("RunPipelineStandalone");

            List<int> pipelines = new() {id };

            var runnable_pipeline_start = _repository.GetAllBySpec(x => pipelines.Contains(x.Id) && (string.IsNullOrEmpty(x.Status) || !statusSkip.Contains(x.Status))).ToList();

            foreach (var item in runnable_pipeline_start)
            {
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(item.LastRunId))
                    item.Status = GetStatusProcess(token, item.LastRunId);
            }

            _context.Pipelines.UpdateRange(runnable_pipeline);
            _context.SaveChanges();

            transaction.CreateSavepoint("Start Pipeline");

            transaction.Commit();

            return response.Succeed();
        }
        catch (Exception exp) {
            if (transaction != null)
            {
                transaction.Rollback();
                transaction.Dispose();
            }
        }
        return response.Failed();
    }

    public ResponseBody StartPipeline(List<int> pipelines)
    {
        try
        {
            var token = GetToken();

            if(string.IsNullOrEmpty(token)) return response.Failed();

            var runnable_pipeline = _repository.GetAllBySpec(x => pipelines.Contains(x.Id) && (string.IsNullOrEmpty(x.Status) || !statusSkip.Contains(x.Status))).ToList();

            foreach (var item in runnable_pipeline)
            {
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(item.LastRunId)) 
                    item.Status = GetStatusProcess(token, item.LastRunId);
            }

            _context.Pipelines.UpdateRange(runnable_pipeline);
            _context.SaveChanges();

            return response.Succeed();
        }catch(Exception exp) {
            var test = exp;
        }
        return response.Failed();
    }

    public ResponseBody GetRunnablePipelineList()
    {
        return  response.View(_repository.GetAllBySpec(x => x.IsRunnable == true));
    }

    public ResponseBody PostPipelines(string user)
    {
        var ret = new List<Pipeline>();
        foreach (var item in GetPipelines())
        {
            if (!_repository.GetAll().Select(x => x.Name.Trim().ToLower()).Contains(item.Trim().ToLower()))
            {
                Pipeline pipeline = new Pipeline
                {
                    Name = item,
                    DisplayName = "",
                    CreatedBy = user,
                    CreatedOn = DateTime.UtcNow,
                    LastModifiedBy = "",
                    IsRunnable = false,
                    LastModifiedOn = null
                };
                ret.Add(pipeline);
            }
        }

        _repository.AddRange(ret);
        if (_repository.UnitOfWork.SaveChanges() > 0) return response.Succeed();

        return response.Failed();
    }
    
    public  ResponseBody GetPipelineList()
    {
        return response.View(_repository.GetAll());
    }

    private string GetStatusProcess(string token,string runId)
    {
        ServiceClientCredentials cred = new TokenCredentials(token);

        var client = new DataFactoryManagementClient(cred) { SubscriptionId = _config.SubscriptionId };

        PipelineRun pipelineRun = client.PipelineRuns.Get(_config.ResourceGroupName, _config.FactoryName, runId);

        return pipelineRun.Status;
    }

    private string GetToken()
    {
        var token = ADFServices.GetToken(_config.TenantId, _config.GrantType, _config.ClientId, _config.ClientSecret, _config.Resource);
        return token;
    }

    private IEnumerable<string> GetPipelines()
    {
        var param = new PipelineParam
        {
            FactoryName = _config.FactoryName,
            ResourceGroupName = _config.ResourceGroupName,
            SubscriptionId = _config.SubscriptionId,
            Token = GetToken(),
        };

        return ADFServices.GetPipelines(param);
    }

    private string GetRunId(string pipeline)
    {
        ServiceClientCredentials cred = new TokenCredentials(this.GetToken());

        var client = new DataFactoryManagementClient(cred) { SubscriptionId = _config.SubscriptionId };

        var runResponse = client.Pipelines.CreateRunWithHttpMessagesAsync(_config.ResourceGroupName, _config.FactoryName, pipeline).Result.Body;

        return runResponse.RunId;
    }
}

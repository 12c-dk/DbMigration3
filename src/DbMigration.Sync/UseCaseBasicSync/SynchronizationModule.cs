using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;

namespace DbMigration.Sync.UseCaseBasicSync;

public class SynchronizationModule
{
    private IAdapter _sourceAdapter;
    private IAdapter _targetAdapter;

    private readonly IAdapterFactory _adapterFactory;

    public SynchronizationModule(IAdapterFactory adapterFactory)
    {
        _adapterFactory = adapterFactory;
    }

    public async Task<DbOperationResponse> SetupConnections<TSourceConfig, TTargetConfig>(TSourceConfig sourceConfig, TTargetConfig targetConfig)
    {
        DbValueOperationResponse<IAdapter> sourceAdapterResponse = await _adapterFactory.CreateAdapter(sourceConfig);
        if (!sourceAdapterResponse.OperationResponse.IsOk)
        {
            sourceAdapterResponse.OperationResponse.GeneralResponses.Add(new GeneralError(
                DbOperationResponseSeverity.Error, $"SetupConnections could not create adapter for {nameof(sourceConfig)}"));
            return sourceAdapterResponse.OperationResponse;
        }
            
        var targetAdapterResponse = await _adapterFactory.CreateAdapter(targetConfig);
        if (!targetAdapterResponse.OperationResponse.IsOk)
        {
            targetAdapterResponse.OperationResponse.GeneralResponses.Add(new GeneralError(
                DbOperationResponseSeverity.Error, $"SetupConnections could not create adapter for {nameof(targetConfig)}"));
            return targetAdapterResponse.OperationResponse;
        }

        _sourceAdapter = sourceAdapterResponse.ResponseValue;
        _targetAdapter = targetAdapterResponse.ResponseValue;

        return new DbOperationResponse();
    }

    public async Task<DbValueCollectionOperationResponse<List<DbItem>>> Synchronize()
    {
        DbValueCollectionOperationResponse<List<DbItem>> syncOutput = new DbValueCollectionOperationResponse<List<DbItem>>();

        List<DbItem> dataWithIdentifiers = await _sourceAdapter.GetTableData("SourceTable");
        dataWithIdentifiers.DataToDbItemsWithIdentifiers(new string[] { "Id" });

        DbValueCollectionOperationResponse<List<DbItem>> upsertResult = await _targetAdapter.UpsertRows("TargetTable", dataWithIdentifiers);
        
        syncOutput.OperationResponse.Append(upsertResult.OperationResponse);
        syncOutput.ResponseValue = upsertResult.ResponseValue.ToList();

        List<DbItem> rows = await _targetAdapter.GetTableData("TargetTable");
        var output2 = rows.FormatAsDataAndIdentifiers();

        return syncOutput;
    }
}
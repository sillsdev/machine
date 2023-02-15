using Google.Protobuf.WellKnownTypes;
using Serval.Platform.Result.V1;

namespace Serval.AspNetCore.Services;

public class ServalResultService : ResultService.ResultServiceBase
{
    private static readonly Empty Empty = new();
    private const int PretranslationInsertBatchSize = 128;

    private readonly IRepository<Pretranslation> _pretranslations;

    public ServalResultService(IRepository<Pretranslation> pretranslations)
    {
        _pretranslations = pretranslations;
    }

    public override async Task<Empty> DeleteAllPretranslations(
        DeleteAllPretranslationsRequest request,
        ServerCallContext context
    )
    {
        await _pretranslations.DeleteAllAsync(
            p => p.TranslationEngineRef == request.EngineId,
            context.CancellationToken
        );
        return Empty;
    }

    public override async Task<Empty> InsertPretranslations(
        IAsyncStreamReader<InsertPretranslationRequest> requestStream,
        ServerCallContext context
    )
    {
        var batch = new List<Pretranslation>();
        await foreach (InsertPretranslationRequest request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            batch.Add(
                new Pretranslation
                {
                    TranslationEngineRef = request.EngineId,
                    CorpusRef = request.CorpusId,
                    TextId = request.TextId,
                    Refs = request.Refs.ToList(),
                    Translation = request.Translation,
                }
            );
            if (batch.Count == PretranslationInsertBatchSize)
            {
                await _pretranslations.InsertAllAsync(batch, context.CancellationToken);
                batch.Clear();
            }
        }
        if (batch.Count > 0)
            await _pretranslations.InsertAllAsync(batch, context.CancellationToken);

        return Empty;
    }
}

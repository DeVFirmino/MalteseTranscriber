namespace MalteseTranscriber.Core.Interfaces;

public interface ITranscriptionPipeline
{
    Task InitializeSessionAsync(string sessionId, string connectionId);
    Task ProcessChunkAsync(string sessionId, byte[] audioBytes, int chunkIndex);
    Task FinalizeSessionAsync(string sessionId);
    Task CleanupConnectionAsync(string connectionId);
}

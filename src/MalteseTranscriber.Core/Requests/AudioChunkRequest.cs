namespace MalteseTranscriber.Core.Requests;

public record AudioChunkRequest(string SessionId, string AudioBase64, int ChunkIndex);

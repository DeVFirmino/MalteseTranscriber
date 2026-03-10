namespace MalteseTranscriber.Core.Exceptions;

public class SessionNotFoundException(string sessionId)
    : Exception($"Session '{sessionId}' was not found.");

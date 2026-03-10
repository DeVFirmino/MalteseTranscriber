namespace MalteseTranscriber.Core.Exceptions;

public class MaxConcurrentSessionsException(int max)
    : Exception($"Maximum concurrent sessions ({max}) reached.");
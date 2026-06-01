namespace TracesNT.Exceptions;

public class PermissionDeniedException(string resourceId, Exception inner)
    : Exception($"Access denied for resource '{resourceId}'.", inner);

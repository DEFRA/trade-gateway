namespace TracesNT.Exceptions;

public class TracesCommunicationException(string message, Exception inner) : Exception(message, inner);

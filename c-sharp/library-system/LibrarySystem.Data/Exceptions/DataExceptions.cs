using System;

namespace LibrarySystem.Data.Exceptions;

public class LibraryDataException : Exception
{
    public LibraryDataException(string message) : base(message) { }
    public LibraryDataException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : LibraryDataException
{
    public EntityNotFoundException(string entityName, object key) 
        : base($"{entityName} with ID '{key}' was not found in the database.") { }
}

public class DatabaseOperationException : LibraryDataException
{
    public DatabaseOperationException(string message, Exception innerException) : base(message, innerException) { }
}

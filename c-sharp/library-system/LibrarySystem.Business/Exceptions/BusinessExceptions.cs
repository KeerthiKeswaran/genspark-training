using System;

namespace LibrarySystem.Business.Exceptions;

public class LibraryBusinessException : Exception
{
    public LibraryBusinessException(string message) : base(message) { }
}

public class ValidationException : LibraryBusinessException
{
    public ValidationException(string message) : base(message) { }
}

public class LendingException : LibraryBusinessException
{
    public LendingException(string message) : base(message) { }
}

public class UnauthorizedException : LibraryBusinessException
{
    public UnauthorizedException(string message) : base(message) { }
}

using System;

namespace server.Business.Exceptions
{
    public class BookingValidationException : Exception
    {
        public BookingValidationException(string message) : base(message)
        {
        }
    }
}

using System;

namespace Utils.General
{
    public sealed class UserFacingException : Exception
    {
        public UserFacingException(string message) : base(message)
        {
        }
    }
}
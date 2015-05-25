using System;

namespace wmib
{
    public class WmibException : Exception
    {
        public WmibException()
        {
        }

        public WmibException(string message)
            : base(message)
        {
        }

        public WmibException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

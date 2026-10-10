// Adapted from SharpMonoInjector (https://github.com/warbler/SharpMonoInjector)
// Copyright (c) 2017 Biney - MIT License (see LICENSE-SharpMonoInjector.txt).
// Changes: trimmed to what AQTrainer needs, C# 5 syntax, 64-bit exception pointer fix.
using System;

namespace SharpMonoInjector
{
    public class InjectorException : Exception
    {
        public InjectorException(string message) : base(message)
        {
        }

        public InjectorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

// Adapted from SharpMonoInjector (https://github.com/warbler/SharpMonoInjector)
// Copyright (c) 2017 Biney - MIT License (see LICENSE-SharpMonoInjector.txt).
// Changes: trimmed to what AQTrainer needs, C# 5 syntax, 64-bit exception pointer fix.
using System;

namespace SharpMonoInjector
{
    public struct ExportedFunction
    {
        public string Name;

        public IntPtr Address;

        public ExportedFunction(string name, IntPtr address)
        {
            Name = name;
            Address = address;
        }
    }
}

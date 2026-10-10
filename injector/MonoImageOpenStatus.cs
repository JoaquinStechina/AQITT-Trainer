// Adapted from SharpMonoInjector (https://github.com/warbler/SharpMonoInjector)
// Copyright (c) 2017 Biney - MIT License (see LICENSE-SharpMonoInjector.txt).
// Changes: trimmed to what AQTrainer needs, C# 5 syntax, 64-bit exception pointer fix.
namespace SharpMonoInjector
{
    public enum MonoImageOpenStatus
    {
        MONO_IMAGE_OK,
        MONO_IMAGE_ERROR_ERRNO,
        MONO_IMAGE_MISSING_ASSEMBLYREF,
        MONO_IMAGE_IMAGE_INVALID
    }
}

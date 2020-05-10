using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using TerraFX.Utilities;

#nullable enable

namespace DDSTextureLoader.NET.TextureParsing
{
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static class ThrowHelper
    {
        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName, Exception inner) => throw new ArgumentException(paramName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName, string message) => throw new ArgumentException(paramName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName) => throw new ArgumentException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName, Exception inner) => throw new ArgumentNullException(paramName, inner);

        [DebuggerHidden]
        public static void ThrowIfNull(object o, string paramName) { if (o is null) ThrowArgumentNullException(paramName); }

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName, string message) => throw new ArgumentNullException(paramName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, Exception inner) => throw new ArgumentOutOfRangeException(paramName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowInvalidOperationException(string message, Exception? inner = null) => throw new InvalidOperationException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowPlatformNotSupportedException(string message, Exception? inner = null) => throw new PlatformNotSupportedException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowNotSupportedException(string message, Exception? inner = null) => throw new NotSupportedException(message, inner);

        [DebuggerHidden]
        public static void ThrowIfFailed(int hr, [CallerMemberName] string name = null!)
        {
            if (Windows.FAILED(hr))
                ExceptionUtilities.ThrowExternalException(name, hr);
        }
    }
}

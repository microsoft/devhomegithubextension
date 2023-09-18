﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;

namespace GitHubPlugin.Helpers;
public static class StringExtensions
{
    public static string ToStringInvariant<T>(this T value) => Convert.ToString(value, CultureInfo.InvariantCulture)!;

    public static string FormatInvariant(this string value, params object[] arguments)
    {
        return string.Format(CultureInfo.InvariantCulture, value, arguments);
    }
}

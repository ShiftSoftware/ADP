﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.SyncAgent.Extensions;

public static class TypeExtensions
{
    public static bool IsNumericType(this Type type)
    {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal) ||
               type == typeof(byte?) ||
               type == typeof(sbyte?) ||
               type == typeof(short?) ||
               type == typeof(ushort?) ||
               type == typeof(int?) ||
               type == typeof(uint?) ||
               type == typeof(long?) ||
               type == typeof(ulong?) ||
               type == typeof(float?) ||
               type == typeof(double?) ||
               type == typeof(decimal?);
    }
}

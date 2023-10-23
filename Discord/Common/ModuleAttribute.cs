﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SolarisBot.Discord.Common
{
    /// <summary>
    /// Attribute for automatically loading modules
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class ModuleAttribute : Attribute
    {
        internal string ModuleName { get; }
        internal ModuleAttribute(string moduleName)
        {
            ModuleName = moduleName.ToLower();
        }
    }
}

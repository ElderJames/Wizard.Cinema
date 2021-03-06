﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Infrastructures.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;

namespace Infrastructures
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutoServices(this IServiceCollection services, string assemblyName = "")
        {
            //自动注册
            foreach (TypeInfo implType in TypeScanner.AllTypes.Where(x => x.GetCustomAttribute<ServiceAttribute>() != null))
            {
                ServiceAttribute attr = implType.GetCustomAttribute<ServiceAttribute>();
                IEnumerable<Type> interfaceTypes = implType.ImplementedInterfaces;

                if (attr.InterfaceType != null)
                {
                    services.TryAdd(new ServiceDescriptor(attr.InterfaceType, implType, attr.Lifetime));
                }
                else
                {
                    foreach (Type interfaceType in interfaceTypes)
                    {
                        services.TryAdd(new ServiceDescriptor(interfaceType, implType, attr.Lifetime));
                    }
                }
            }
        }
    }
}

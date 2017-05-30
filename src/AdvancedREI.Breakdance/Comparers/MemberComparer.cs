// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace AdvancedREI.Breakdance.Core
{

    sealed public class MemberComparer : IComparer
    {

        static private readonly Hashtable _memberType;
        static MemberComparer()
        {
            Hashtable memberType = new Hashtable
                {
                    { MemberTypes.Field, 1 },
                    { MemberTypes.Constructor, 2 },
                    { MemberTypes.Property, 3 },
                    { MemberTypes.Event, 4 },
                    { MemberTypes.Method, 5 },
                    { MemberTypes.NestedType, 6 },
                    { MemberTypes.TypeInfo, 7 },
                    { MemberTypes.Custom, 8 }
                };
            _memberType = memberType;
        }

        private Hashtable hash;

        public MemberComparer(Type type)
        {
            hash = new Hashtable();
            for (int i = 0; null != type; ++i, type = type.BaseType)
            {
                hash.Add(type, i);
            }
        }
        public int Compare(object x, object y)
        {
            return Compare((MemberInfo)x, (MemberInfo)y);
        }
        public int Compare(MemberInfo x, MemberInfo y)
        {
            if (x.MemberType == y.MemberType)
            {
                Type xt = x.DeclaringType;
                Type yt = y.DeclaringType;
                if (xt != yt)
                {
                    return (int)hash[yt] - (int)hash[xt];
                }

                /*PropertyInfo xa = x.GetType().GetProperty("Attributes");
                    if (null != xa) {
                        PropertyInfo ya = y.GetType().GetProperty("Attributes");
                        if (null != ya) {
                            int xb = (int) xa.GetValue(x, null);
                            int yb = (int) xa.GetValue(y, null);
                            int b = xb - yb;
                            if (0 != b) {
                                return b;
                            }
                        }
                    }*/

                int cmp = String.Compare(x.Name, y.Name, false, CultureInfo.InvariantCulture);
                if (0 == cmp)
                {
                    MethodInfo xMethodInfo = null, yMethodInfo = null;
                    ParameterInfo[] xParameterInfos, yParameterInfos;
                    switch (x.MemberType)
                    {
                        case MemberTypes.Constructor:
                            xParameterInfos = ((ConstructorInfo)x).GetParameters();
                            yParameterInfos = ((ConstructorInfo)y).GetParameters();
                            break;
                        case MemberTypes.Method:
                            xMethodInfo = (MethodInfo)x;
                            yMethodInfo = (MethodInfo)y;
                            xParameterInfos = xMethodInfo.GetParameters();
                            yParameterInfos = yMethodInfo.GetParameters();
                            break;
                        case MemberTypes.Property:
                            xParameterInfos = ((PropertyInfo)x).GetIndexParameters();
                            yParameterInfos = ((PropertyInfo)y).GetIndexParameters();
                            break;
                        default:
                            xParameterInfos = yParameterInfos = new ParameterInfo[0];
                            break;
                    }
                    cmp = xParameterInfos.Length - yParameterInfos.Length;
                    if (0 == cmp)
                    {
                        int count = xParameterInfos.Length;
                        for (int i = 0; i < count; ++i)
                        {
                            cmp = String.Compare(xParameterInfos[i].ParameterType.FullName, yParameterInfos[i].ParameterType.FullName, false, CultureInfo.InvariantCulture);
                            if (cmp == 0)
                            {
                                // For generic parameters, FullName is null. Hence comparing the names
                                cmp = String.Compare(xParameterInfos[i].ParameterType.Name, yParameterInfos[i].ParameterType.Name, false, CultureInfo.InvariantCulture);
                            }
                            if (0 != cmp)
                            {
                                break;
                            }
                        }

                        if (0 == cmp && xMethodInfo != null)
                        {
                            // Two methods with same name, same parameters. Sort by the # of generic type parameters.
                            cmp = xMethodInfo.GetGenericArguments().Count() - yMethodInfo.GetGenericArguments().Count();
                        }
                    }
                }
                return cmp;
            }
            return ((int)_memberType[x.MemberType] - (int)_memberType[y.MemberType]);
        }
    }

}
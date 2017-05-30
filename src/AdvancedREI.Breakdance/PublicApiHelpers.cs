// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace AdvancedREI.Breakdance.Core
{

    /// <summary>
    /// 
    /// </summary>
    public partial class PublicApiHelpers
    {

        #region Private Static Members

        private static readonly ParameterModifier[] EmptyParameterModifiers = new ParameterModifier[0];
        private static readonly Type[] EmptyTypes = new Type[0];
        private static Hashtable Synonyms;
        private static readonly Type[] ToStringFormatParameter = new Type[] { typeof(IFormatProvider) };
        private static readonly object[] ToSTringFormatValues = new object[] { CultureInfo.InvariantCulture };

        #endregion

        #region Private Instance Members

        private readonly List<Assembly> assemblies = new List<Assembly>();
        private string[] outputFilter;

        #endregion

        static PublicApiHelpers()
        {
            Synonyms = new Hashtable
            {
                { "System.Void", "void" },
                { "System.Object", "object" },
                { "System.String", "string" },
                { "System.Int16", "short" },
                { "System.Int32", "int" },
                { "System.Int64", "long" },
                { "System.Byte", "byte" },
                { "System.Boolean", "bool" },
                { "System.Char", "char" },
                { "System.Decimal", "decimal" },
                { "System.Double", "double" },
                { "System.Single", "float" },
                { "System.Object[]", "object[]" },
                { "System.Char[]", "char[]" },
                { "System.Byte[]", "byte[]" },
                { "System.Int32[]", "int[]" },
                { "System.String[]", "string[]" }
            };
        }

        public void GetPublicApiSurface(string[] assemblyList)
        {

            ArrayList typesList = new ArrayList();

            for (int k = 0; k < assemblyList.Length; ++k)
            {
                try
                {
                    Assembly assembly;
                    if (File.Exists(assemblyList[k]))
                    {
                        assembly = Assembly.LoadFrom(assemblyList[k]);
                    }
                    else
                    {
                        assembly = Assembly.Load(assemblyList[k]);
                    }

                    assemblies.Add(assembly);
                    typesList.AddRange(assembly.GetTypes());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception loading types from assembly '{0}':", assemblyList[k]);
                    Console.WriteLine(e.ToString());
                    Environment.Exit(1);
                }
            }

            typesList.Sort(TypeComparer.Default);

            StringBuilder builder = new StringBuilder();
            DumpAssemblyAPI(typesList, builder);
        }

        private void DumpAssemblyAPI(ArrayList sortedTypeList, StringBuilder builder)
        {
            string lastNamespace = "";
            foreach (Type type in sortedTypeList)
            {
                builder.Length = 0;

                string typeFullName = type.FullName;
                if (typeFullName.StartsWith("<PrivateImplementationDetails>"))
                {
                    continue;
                }
                /*
                if(0 <= Array.BinarySearch(_ignoreTypes,typeFullName)) {
                    continue;
                }
                */
                if (type.IsSpecialName)
                {
                    continue;
                }

                Type declaringType = type;
                while (null != declaringType)
                {
                    switch (TypeAttributes.VisibilityMask & declaringType.Attributes)
                    {
                        case TypeAttributes.Public:
                        case TypeAttributes.NestedPublic:
                        case TypeAttributes.NestedFamily:
                        case TypeAttributes.NestedFamANDAssem:
                        case TypeAttributes.NestedFamORAssem:
                            declaringType = declaringType.DeclaringType;
                            continue;
                        case TypeAttributes.NotPublic:
                        case TypeAttributes.NestedPrivate:
                        case TypeAttributes.NestedAssembly:
                            Debug.Assert(null != declaringType, "null declaringType");
                            break;
                        default:
                            Debug.Assert(false, "unknown type");
                            throw new InvalidOperationException(declaringType.Attributes.ToString());
                    }
                    break;
                }
                if (typeof(TypeConverter).IsAssignableFrom(type))
                {
                    ConstructorInfo ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance, null, EmptyTypes, EmptyParameterModifiers);
                    if (null != ctor)
                    {
                        Console.WriteLine("{0}", type.FullName);
                    }
                    else
                    {
                        Console.WriteLine("{0} missing public ctor", type.FullName);
                    }
                }

                if (null != declaringType)
                {
                    //Console.WriteLine("{0}", type.FullName);
                    continue;
                }

                bool abort = AppendCustomAttributes(builder, type.GetCustomAttributes(false), false, type.IsEnum, true);
                if (abort)
                {
                    continue;
                }
                AppendClassDeclarationAPI(builder, type);
                builder.Append(" {");
                builder.Append(Environment.NewLine);

                string currentNamespace = type.Namespace;
                if (lastNamespace != currentNamespace)
                {
                    lastNamespace = currentNamespace;
                }

                AppendClassMemberAPI(builder, type);
                if (0 < builder.Length)
                {
                    FilterAssembly(builder);
                    Console.Write(builder.ToString());
                    builder.Length = 0;
                }
                Console.Write("}");
                Console.Write(Environment.NewLine);
                Console.Write(Environment.NewLine);
            }
        }

        private void FilterAssembly(StringBuilder builder)
        {
            string[] filter = outputFilter;
            if (null == filter)
            {
                filter = new string[2 + this.assemblies.Count];
                filter[0] = ", " + typeof(object).Assembly;
                filter[1] = ", " + typeof(Uri).Assembly;
                for (int i = 2; i < filter.Length; i++)
                {
                    filter[i] = ", " + assemblies[i - 2];
                }
                outputFilter = filter;
            }
            for (int i = 0; i < filter.Length; ++i)
            {
                builder.Replace(filter[i], "");
            }
        }

        private static void AppendClassDeclarationAPI(StringBuilder builder, Type type)
        {
            if (type.IsPublic | type.IsNestedPublic)
            {
                builder.Append("public ");
            }
            else if (type.IsNestedFamily | type.IsNestedFamORAssem | type.IsNestedFamANDAssem)
            {
                builder.Append("protected ");
            }
            else
            {
                Debug.Assert(false, "non public or protected type");
            }

            if (type.IsInterface)
            {
                builder.Append("interface ");
            }
            else if (type.IsEnum)
            {
                builder.Append("enum ");
            }
            else if (type.IsValueType)
            {
                builder.Append("struct ");
            }
            else if (type.IsClass)
            {
                if (type.IsSealed)
                {
                    builder.Append("sealed ");
                }
                else if (type.IsAbstract)
                {
                    builder.Append("abstract ");
                }
                builder.Append("class ");
            }
            else
            {
                builder.Append("? ");
            }
            builder.Append(type.FullName);

            bool haveColon = false;
            Type baseType = type.BaseType;
            if ((null != baseType) && (typeof(object) != baseType) && (typeof(ValueType) != baseType))
            {
                if (typeof(Enum) == baseType)
                {
                    baseType = Enum.GetUnderlyingType(type);
                }
                haveColon = true;
                builder.Append(" : ");
                AppendParameterType(builder, baseType);
            }

            if (!type.IsEnum)
            {
                Type[] baseInterfaces = type.GetInterfaces();
                Array.Sort(baseInterfaces, TypeComparer.Default);
                foreach (Type baseInterface in baseInterfaces)
                {
                    if (haveColon)
                    {
                        builder.Append(", ");
                    }
                    else
                    {
                        haveColon = true;
                        builder.Append(" : ");
                    }
                    builder.Append(baseInterface.Name);
                }
            }
        }

        private static void AppendClassMemberAPI(StringBuilder builder, Type type)
        {
            MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (0 < members.Length)
            {
                Array.Sort(members, new MemberComparer(type));

                bool lastHadAttributes = false;
                MemberTypes lastMemberType = 0;

                foreach (MemberInfo info in members)
                {
                    bool rememberLast = lastHadAttributes;
                    MemberTypes rememberType = lastMemberType;
                    int startLength = builder.Length;
                    if (((lastMemberType != info.MemberType) && (0 != lastMemberType)) || lastHadAttributes)
                    {
                        builder.Append(Environment.NewLine);
                        lastHadAttributes = false;
                    }
                    lastMemberType = info.MemberType;
                    int newlineLength = builder.Length;

                    bool abort = AppendCustomAttributes(builder, info.GetCustomAttributes(true), true, false, true);
                    if (abort)
                    {
                        builder.Length = startLength;
                        lastHadAttributes = rememberLast;
                        lastMemberType = rememberType;
                        continue;
                    }
                    lastHadAttributes = (newlineLength != builder.Length);
                    builder.Append("\t");
                    int attributeLength = builder.Length;

                    switch (info.MemberType)
                    {
                        case MemberTypes.Constructor:
                            AppendConstructorInfo(builder, type, info as ConstructorInfo);
                            break;
                        case MemberTypes.Event:
                            AppendEventInfo(builder, type, info as EventInfo);
                            break;
                        case MemberTypes.Field:
                            AppendFieldInfo(builder, type, info as FieldInfo);
                            break;
                        case MemberTypes.Method:
                            AppendMethodInfo(builder, type, info as MethodInfo);
                            break;
                        case MemberTypes.Property:
                            AppendPropertyInfo(builder, type, info as PropertyInfo);
                            break;
                        case MemberTypes.NestedType:
                            //DumpClassAPI(builder, info as Type);
                            break;
                        default:
                            builder.Append(" ");
                            builder.Append(info.Name);
                            builder.Append(" ");
                            break;
                    }
                    if (attributeLength == builder.Length)
                    {
                        builder.Length = startLength;
                        lastHadAttributes = rememberLast;
                        lastMemberType = rememberType;
                    }
                }
            }
        }

        private static bool AppendCustomAttributes(StringBuilder builder, object[] attributes, bool indent, bool isEnum, bool appendNewLine)
        {
            if (0 < attributes.Length)
            {
                int count = 0;
                int startLength = builder.Length;
                Array.Sort(attributes, ObjectTypeComparer.Default);

                if (indent)
                {
                    builder.Append("\t");
                }
                builder.Append("[");
                if (appendNewLine)
                {
                    builder.Append(Environment.NewLine);
                }
                foreach (object attribute in attributes)
                {
                    if (attribute is System.Runtime.InteropServices.MarshalAsAttribute)
                    {
                        continue;
                    }
                    else if (attribute is System.Runtime.InteropServices.StructLayoutAttribute)
                    {
                        continue;
                    }
                    else if (attribute is System.Runtime.CompilerServices.MethodImplAttribute)
                    {
                        continue;
                    }
                    else if (attribute is System.Runtime.TargetedPatchingOptOutAttribute)
                    {
                        continue;
                    }
                    else if (attribute is System.Diagnostics.CodeAnalysis.SuppressMessageAttribute)
                    {
                        continue;
                    }
                    else if (attribute is System.Diagnostics.DebuggerStepThroughAttribute)
                    {
                        continue;
                    }
                    else if (isEnum && (attribute is System.SerializableAttribute))
                    {
                        continue;
                    }

                    count++;

                    if (indent)
                    {
                        builder.Append("\t");
                    }
                    builder.Append(attribute.GetType().Name);
                    builder.Append("(");

                    builder.Append("),");
                    if (appendNewLine)
                    {
                        builder.Append(Environment.NewLine);
                    }
                }
                if (0 < count)
                {
                    if (indent)
                    {
                        builder.Append("\t");
                    }
                    builder.Append("]");
                    if (appendNewLine)
                    {
                        builder.Append(Environment.NewLine);
                    }
                }
                else
                {
                    builder.Length = startLength;
                }
            }
            return false;
        }

        private static void AppendConstructorInfo(StringBuilder builder, Type type, ConstructorInfo info)
        {
            if (info.IsPublic)
            {
                builder.Append("public");
            }
            else if (!(type.IsClass && type.IsSealed) && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly))
            {
                builder.Append("protected");
            }
            else return;

            builder.Append(" ");
            builder.Append(type.Name);
            builder.Append(" ");
            AppendParameterInfo(builder, info.GetParameters(), true, true);
            builder.Append(Environment.NewLine);
        }

        private static void AppendEventInfo(StringBuilder builder, Type type, EventInfo info)
        {
            int propertyStart = builder.Length;

            AppendParameterType(builder, info.EventHandlerType);
            builder.Append(" ");
            builder.Append(info.Name);

            builder.Append(" {");
            bool gettable = AppendPropertyMethod(builder, type, info.GetAddMethod(), "add");
            bool settable = AppendPropertyMethod(builder, type, info.GetRemoveMethod(), "remove");
            if (gettable || settable)
            {
                builder.Append(" }");
                builder.Append(Environment.NewLine);
            }
            else
            {
                builder.Length = propertyStart;
            }
        }

        private static void AppendFieldInfo(StringBuilder builder, Type type, FieldInfo info)
        {
            if (type.IsEnum && info.IsSpecialName)
            {
                return;
            }
            if (info.IsPublic)
            {
                if (type.IsEnum)
                {
                    builder.Append("");
                }
                else
                {
                    builder.Append("public");
                }
            }
            else if (!(type.IsClass && type.IsSealed) && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly))
            {
                if (type.IsEnum)
                {
                    builder.Append("");
                }
                else
                {
                    builder.Append("protected");
                }
            }
            else return;

            if (!type.IsEnum)
            {
                if (info.IsStatic)
                {
                    builder.Append(" static");
                }
                else if (info.IsInitOnly)
                {
                    builder.Append(" const");
                }
                if (info.IsInitOnly)
                {
                    builder.Append(" readonly");
                }
            }
            if (!type.IsEnum)
            {
                builder.Append(" ");
                AppendParameterType(builder, info.FieldType);
                builder.Append(" ");
            }

            builder.Append(info.Name);
            builder.Append(" = ");

            if (info.IsLiteral || info.IsStatic)
            {
                object fieldValue = null;
                try
                {
                    fieldValue = info.GetValue(null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if (null != fieldValue)
                {
                    if (fieldValue is string)
                    {
                        builder.Append('\"');
                        builder.Append((string)fieldValue);
                        builder.Append('\"');
                    }
                    else if (fieldValue is long)
                    {
                        builder.Append(((long)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is byte)
                    {
                        builder.Append(((byte)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is bool)
                    {
                        builder.Append(((bool)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is double)
                    {
                        builder.Append(((double)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is short)
                    {
                        builder.Append(((short)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is float)
                    {
                        builder.Append(((float)fieldValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (fieldValue is Guid)
                    {
                        builder.Append('{');
                        builder.Append((Guid)fieldValue);
                        builder.Append('}');
                    }
                    else if (fieldValue is Enum)
                    {
                        // remove the enumness, without assuming a particular underlying type.
                        builder.Append(Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(type)));
                    }
                    else
                    {
                        string svalue;
                        try
                        {
                            MethodInfo tostring = fieldValue.GetType().GetMethod("ToString", ToStringFormatParameter);
                            if (null != tostring)
                            {
                                svalue = (string)tostring.Invoke(fieldValue, ToSTringFormatValues);
                            }
                            else
                            {
                                svalue = fieldValue.ToString();
                            }
                        }
                        catch (Exception e)
                        { // OracleClientException - oracle 8.1.7 or greater not installed
                            svalue = e.ToString();
                            if ("System.Data.OracleClient.OracleNumber" == type.FullName)
                            {
                                switch (info.Name)
                                {
                                    case "E":
                                        svalue = "2.71828182845904523536028747135266249776";
                                        break;
                                    case "MaxValue":
                                        svalue = "9.9999999999999999999999999999999999999E+125";
                                        break;
                                    case "MinusOne":
                                        svalue = "-1";
                                        break;
                                    case "MinValue":
                                        svalue = "-9.9999999999999999999999999999999999999E+125";
                                        break;
                                    case "One":
                                        svalue = "1";
                                        break;
                                    case "PI":
                                        svalue = "3.1415926535897932384626433832795028842";
                                        break;
                                    case "Zero":
                                        svalue = "0";
                                        break;
                                }
                            }
                        }
                        builder.Append(svalue);
                    }
                }
            }
            builder.Append(Environment.NewLine);
        }

        static private void AppendMethodInfo(StringBuilder builder, Type type, MethodInfo info)
        {
            string infoName = info.Name;
            if ("IsRowOptimized" == infoName)
            {
                return;
            }
            if (info.IsSpecialName)
            {
                return;
            }
            if (info.IsPublic)
            {
                if (!type.IsInterface)
                {
                    builder.Append("public ");
                }
            }
            else if (!(type.IsClass && type.IsSealed) && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly))
            {
                if (!type.IsInterface)
                {
                    builder.Append("protected ");
                }
            }
            else if (infoName.StartsWith("Reset") && ("Reset" != infoName))
            {
                PropertyInfo propInfo = type.GetProperty(infoName.Substring("Reset".Length), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
                if (null != propInfo && (0 == info.GetParameters().Length))
                {
                    builder.Append("private ");
                }
                else return;
            }
            else if (infoName.StartsWith("ShouldSerialize") && ("ShouldSerialize" != infoName))
            {
                PropertyInfo propInfo = type.GetProperty(infoName.Substring("ShouldSerialize".Length), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
                if (null != propInfo && (0 == info.GetParameters().Length))
                {
                    builder.Append("private ");
                }
                else return;
            }
            else if (!(type.IsClass && type.IsSealed) && info.IsVirtual)
            {
                if (-1 == info.Name.IndexOf("."))
                {
                    builder.Append("internal ");
                }
            }
            else return;

            if (!type.IsInterface)
            {
                if (info.IsAbstract)
                {
                    builder.Append("abstract ");
                }
                else if (info.IsVirtual && (-1 == info.Name.IndexOf(".")))
                {
                    builder.Append("virtual ");
                }
                else if (info.IsStatic)
                {
                    builder.Append("static ");
                }
            }
            //builder.Append(info.CallingConvention.ToString());
            //builder.Append(" ");

            AppendParameterType(builder, info.ReturnType);
            builder.Append(" ");
            builder.Append(infoName);
            builder.Append(" ");
            AppendParameterInfo(builder, info.GetParameters(), true, true);
            builder.Append(Environment.NewLine);
        }

        static private void AppendPropertyInfo(StringBuilder builder, Type type, PropertyInfo info)
        {
            int propertyStart = builder.Length;

            builder.Append("");
            AppendParameterType(builder, info.PropertyType);
            builder.Append(" ");
            builder.Append(info.Name);
            builder.Append(" ");

            ParameterInfo[] parameters = info.GetIndexParameters();
            if (0 < parameters.Length)
            {
                AppendParameterInfo(builder, parameters, false, true);
            }

            builder.Append(" { ");
            bool gettable = AppendPropertyMethod(builder, type, info.GetGetMethod(true), "get");
            if (gettable)
            {
                builder.Append(' ');
            }
            bool settable = AppendPropertyMethod(builder, type, info.GetSetMethod(true), "set");
            if (settable)
            {
                builder.Append(' ');
            }
            if (gettable || settable)
            {
                builder.Append("}");
                builder.Append(Environment.NewLine);
            }
            else
            {
                builder.Length = propertyStart;
            }
        }

        static private bool AppendPropertyMethod(StringBuilder builder, Type type, MethodInfo info, string method)
        {
            if (null != info)
            {
                int setStart = builder.Length;

                AppendCustomAttributes(builder, info.GetCustomAttributes(true), false, false, false);

                if (info.IsPublic)
                {
                    builder.Append("public ");
                }
                else if (!(type.IsClass && type.IsSealed) && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly))
                {
                    builder.Append("protected ");
                }
                else
                {
                    builder.Length = setStart;
                    return false;
                }
                if (info.IsAbstract)
                {
                    builder.Append("abstract ");
                }
                else if (info.IsVirtual)
                {
                    builder.Append("virtual ");
                }
                else if (info.IsStatic)
                {
                    builder.Append("static ");
                }
                builder.Append(method);
                builder.Append(';');
                return true;
            }
            return false;
        }

        static private void AppendParameterInfo(StringBuilder builder, ParameterInfo[] parameters, bool asMethod, bool withNames)
        {
            if (0 < parameters.Length)
            {
                builder.Append(asMethod ? '(' : '[');
                for (int i = 0; i < parameters.Length; ++i)
                {
                    if (0 < i)
                    {
                        builder.Append(", ");
                    }
                    if (withNames)
                    {
                        AppendParameterInfo(builder, parameters[i]);
                    }
                    else
                    {
                        builder.Append(parameters[i].ParameterType.FullName);
                    }
                }
                builder.Append(asMethod ? ')' : ']');
            }
            else
            {
                builder.Append("()");
            }
        }

        static private void AppendParameterInfo(StringBuilder builder, ParameterInfo info)
        {
            if (info.IsOut)
            {
                builder.Append("out ");
            }
            else if (info.IsOptional)
            {
                builder.Append("params ");
            }
            AppendParameterType(builder, info.ParameterType);
            builder.Append(" ");
            builder.Append(info.Name);
        }

        static private void AppendParameterType(StringBuilder builder, Type parameterType)
        {
            string name = parameterType.FullName ?? parameterType.Name;
            string synonm = (string)Synonyms[name];
            if (null != synonm)
            {
                builder.Append(synonm);
            }
            else if (parameterType.IsGenericType && name.Contains("Version="))
            {
                // If there is generic type with generic parameter (for e.g. IEnumerable<T>),
                // then AppendGenericTypeName produces 'System.IEnumerable[[]]' whereas
                // type.Name is IEnumerable'1. Also, to avoid too any changes with the existing baseline,
                // only going into this method if there is a "Version=" present in the name.
                AppendGenericTypeName(builder, parameterType);
            }
            else if (name.StartsWith("System.Data."))
            {
                builder.Append(parameterType.Name);
            }
            else
            {
                builder.Append(name);
            }
        }

        static private void AppendGenericTypeName(StringBuilder builder, Type type)
        {
            if (type.IsGenericType)
            {
                builder.Append(type.GetGenericTypeDefinition().FullName);
                builder.Append("[");
                bool first = true;
                foreach (var argType in type.GetGenericArguments())
                {
                    if (!first)
                    {
                        builder.Append(",");
                    }
                    builder.Append("[");
                    AppendGenericTypeName(builder, argType);
                    builder.Append("]");
                    first = false;
                }
                builder.Append("]");
            }
            else
            {
                builder.Append(type.FullName);
            }
        }

    }
}

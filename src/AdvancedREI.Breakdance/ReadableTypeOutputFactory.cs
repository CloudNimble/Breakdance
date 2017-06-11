using AdvancedREI.Breakdance.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AdvancedREI.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    internal static class ReadableTypeOutputFactory
    {

        #region Private Members

        private static readonly List<Type> IgnoredAttributes;
        private static Hashtable Synonyms;
        private static readonly Type[] ToStringFormatParameter;
        private static readonly object[] ToStringFormatValues;

        #endregion

        #region Constructors

        static ReadableTypeOutputFactory()
        {
            IgnoredAttributes = new List<Type>
            {
                typeof(MarshalAsAttribute),
                typeof(StructLayoutAttribute),
                typeof(MethodImplAttribute),
                typeof(TargetedPatchingOptOutAttribute),
                typeof(SuppressMessageAttribute),
                typeof(DebuggerStepThroughAttribute),
                typeof(SerializableAttribute),
                typeof(CompilerGeneratedAttribute)
            };
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
            ToStringFormatParameter = new Type[] { typeof(IFormatProvider) };
            ToStringFormatValues = new object[] { CultureInfo.InvariantCulture };
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        internal static List<string> GetCustomAttributesList(IEnumerable<object> attributes)
        {
            var filteredAttributes = attributes.Where(c => !IgnoredAttributes.Contains(c.GetType())).ToList();
            filteredAttributes.Sort(ObjectTypeComparer.Default);

            return filteredAttributes.Select(c => GetCustomAttributeString(c)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        internal static string GetCustomAttributeString(object attribute)
        {
            return $"[{attribute.GetType().FullName}]";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetConstructorInfoString(Type type, ConstructorInfo info)
        {
            if (!info.IsPublic && !info.IsProtected(type)) return string.Empty;
            var visibility = info.IsPublic ? AssemblyConstants.Public : AssemblyConstants.Protected;
            var parameters = GetParameterInfoString(info.GetParameters(), true, true);

            //TODO: RWM: Do we want to cover generic type parameters? Or do they not matter because they are generic? 
            //           Type filters might be important, however.

            return $"{visibility} {type.Name}{parameters}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetEventInfoString(Type type, EventInfo info)
        {
            if (info == null) return String.Empty;

            var getter = GetPropertyAccessorString(type, info.GetAddMethod(), AssemblyConstants.Add);
            var setter = GetPropertyAccessorString(type, info.GetRemoveMethod(), AssemblyConstants.Remove);

            if (string.IsNullOrWhiteSpace(getter) && string.IsNullOrWhiteSpace(setter)) return string.Empty;

            var parameterType = GetParameterTypeString(info.EventHandlerType);

            return $"{parameterType} {info.Name} {AssemblyConstants.BraceOpen} {getter} {setter} {AssemblyConstants.BraceClose}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetFieldInfoString(Type type, FieldInfo info)
        {
            if ((type.IsEnum && info.IsSpecialName) || (!info.IsProtected(type) && !info.IsPublic)) return string.Empty;

            var visibility = string.Empty;
            var modifier = string.Empty;
            var parameterType = !type.IsEnum ? AssemblyConstants.Space + GetParameterTypeString(info.FieldType) : String.Empty;
            var fieldValueString = string.Empty;
            if (info.IsPublic)
            {
                visibility = !type.IsEnum ? AssemblyConstants.Public : String.Empty;
            }
            else
            {
                visibility = !type.IsEnum ? AssemblyConstants.Protected : String.Empty;
            }

            if (!type.IsEnum)
            {
                if (info.IsStatic)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Static;
                }
                else if (info.IsInitOnly)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Const;
                }
                if (info.IsInitOnly)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.ReadOnly;
                }
            }

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

                switch (fieldValue)
                {
                    case string x:
                        fieldValueString = $"\"{x}\"";
                        break;
                    case long x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case byte x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case bool x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case double x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case short x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case float x:
                        fieldValueString = x.ToString(CultureInfo.InvariantCulture);
                        break;
                    case Guid x:
                        fieldValueString = x.ToString("B");
                        break;
                    case Enum x:
                        fieldValueString = Convert.ChangeType(x, Enum.GetUnderlyingType(type)).ToString();
                        break;
                    case null:
                        break;
                    default:
                        try
                        {
                            var toStringMethod = fieldValue.GetType().GetMethod("ToString", ToStringFormatParameter);
                            fieldValueString = toStringMethod != null ? (string)toStringMethod.Invoke(fieldValue, ToStringFormatValues) : fieldValue.ToString();
                        }
                        catch (Exception ex)
                        {
                            fieldValueString = ex.ToString();
                            if (type.FullName == "System.Data.OracleClient.OracleNumber")
                            {
                                switch (info.Name)
                                {
                                    case "E":
                                        fieldValueString = "2.71828182845904523536028747135266249776";
                                        break;
                                    case "MaxValue":
                                        fieldValueString = "9.9999999999999999999999999999999999999E+125";
                                        break;
                                    case "MinusOne":
                                        fieldValueString = "-1";
                                        break;
                                    case "MinValue":
                                        fieldValueString = "-9.9999999999999999999999999999999999999E+125";
                                        break;
                                    case "One":
                                        fieldValueString = "1";
                                        break;
                                    case "PI":
                                        fieldValueString = "3.1415926535897932384626433832795028842";
                                        break;
                                    case "Zero":
                                        fieldValueString = "0";
                                        break;
                                }

                            }
                        }
                        break;
                }
            }
            return $"{visibility}{modifier}{parameterType} {info.Name} = {fieldValueString};";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetMethodInfoString(Type type, MethodInfo info)
        {
            if (info == null) return string.Empty;

            var visibility = string.Empty;
            var modifier = string.Empty;
            string infoName = info.Name;

            if (infoName == "IsRowOptimized" || info.IsSpecialName) return string.Empty;

            if (info.IsPublic)
            {
                visibility = !type.IsInterface ? AssemblyConstants.Public : string.Empty;
            }
            else if (info.IsProtected(type))
            {
                visibility = !type.IsInterface ? AssemblyConstants.Protected : string.Empty;

            }
            else if (infoName.StartsWith("Reset") && ("Reset" != infoName))
            {
                PropertyInfo propInfo = type.GetProperty(infoName.Substring("Reset".Length), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
                if (null != propInfo && (0 == info.GetParameters().Length))
                {
                    visibility = AssemblyConstants.Private;
                }
                else return String.Empty;
            }
            else if (infoName.StartsWith("ShouldSerialize") && ("ShouldSerialize" != infoName))
            {
                PropertyInfo propInfo = type.GetProperty(infoName.Substring("ShouldSerialize".Length), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
                if (null != propInfo && (0 == info.GetParameters().Length))
                {
                    visibility = AssemblyConstants.Private;
                }
                else return string.Empty;
            }
            else if (!(type.IsClass && type.IsSealed) && info.IsVirtual)
            {
                if (-1 == info.Name.IndexOf("."))
                {
                    visibility = AssemblyConstants.Internal;
                }
            }
            else return string.Empty;

            if (!type.IsInterface)
            {
                if (info.IsAbstract)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Abstract;
                }
                else if (info.IsVirtual && (-1 == info.Name.IndexOf(".")))
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Virtual;
                }
                else if (info.IsStatic)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Static;
                }
            }

            var parameterType = GetParameterTypeString(info.ReturnType);
            var parameters = GetParameterInfoString(info.GetParameters(), true, true);

            return $"{visibility}{modifier} {parameterType} {infoName}{parameters}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetPropertyInfoString(Type type, PropertyInfo info)
        {
            if (info == null) return String.Empty;

            var getter = GetPropertyAccessorString(type, info.GetGetMethod(true), AssemblyConstants.Get);
            var setter = GetPropertyAccessorString(type, info.GetSetMethod(true), AssemblyConstants.Set, true);

            if (string.IsNullOrWhiteSpace(getter) && string.IsNullOrWhiteSpace(setter)) return string.Empty;

            var parameterType = GetParameterTypeString(info.PropertyType);
            var indexParameters = GetParameterInfoString(info.GetIndexParameters(), false, true);

            return $"{parameterType} {info.Name}{indexParameters} {AssemblyConstants.BraceOpen} {getter}{setter} {AssemblyConstants.BraceClose}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        /// <param name="method"></param>
        /// <param name="spaceBefore"></param>
        /// <returns></returns>
        internal static string GetPropertyAccessorString(Type type, MethodInfo info, string method, bool spaceBefore = false)
        {
            if (info == null || (!info.IsProtected(type) && !info.IsPublic)) return string.Empty;

            var attributes = GetCustomAttributesList(info.GetCustomAttributes(true));
            var visibility = info.IsPublic ? AssemblyConstants.Public : AssemblyConstants.Protected;
            string modifier = string.Empty;
            var space = spaceBefore ? AssemblyConstants.Space : String.Empty;

            if (info.IsAbstract)
            {
                modifier = AssemblyConstants.Space + AssemblyConstants.Abstract;
            }
            else if (info.IsVirtual)
            {
                modifier = AssemblyConstants.Space + AssemblyConstants.Virtual;
            }
            else if (info.IsStatic)
            {
                modifier = AssemblyConstants.Space + AssemblyConstants.Static;
            }

            return $"{space}{visibility}{modifier} {method};";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="asMethod"></param>
        /// <param name="withNames"></param>
        /// <returns></returns>
        internal static string GetParameterInfoString(IEnumerable<ParameterInfo> parameters, bool asMethod, bool withNames)
        {
            if (parameters.Count() == 0) return asMethod ? AssemblyConstants.ParenthesisOpen + AssemblyConstants.ParenthesisClose : string.Empty;

            var openCharacter = asMethod ? AssemblyConstants.ParenthesisOpen : AssemblyConstants.BracketOpen;
            var closeCharacter = asMethod ? AssemblyConstants.ParenthesisClose : AssemblyConstants.BracketClose;

            return $"{openCharacter}{string.Join(", ", parameters.Select(c => withNames ? GetParameterInfoString(c) : c.ParameterType.FullName))}{closeCharacter}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static string GetParameterInfoString(ParameterInfo info)
        {
            var modifier = string.Empty;

            if (info.IsOut)
            {
                modifier = AssemblyConstants.Out + AssemblyConstants.Space;
            }
            else if (info.IsOptional)
            {
                modifier = AssemblyConstants.Params + AssemblyConstants.Space;
            }

            return $"{modifier}{GetParameterTypeString(info.ParameterType)} {info.Name}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        internal static string GetParameterTypeString(Type parameterType)
        {
            var name = parameterType.FullName ?? parameterType.Name;
            var synonym = (string)Synonyms[name];

            if (synonym != null)
            {
                return synonym;
            }
            else if (parameterType.IsGenericType)
            {
                return GetGenericTypeName(parameterType);
            }
            else if (name.StartsWith("System.Data."))
            {
                return parameterType.Name;
            }
            else
            {
                return name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static string GetGenericTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                return $"{type.GetGenericTypeDefinition().FullName}<{string.Join(", ", type.GetGenericArguments().Select(c => GetGenericTypeName(c)))}>";
            }
            else
            {
                return type.FullName != null ? (string)Synonyms[type.FullName] ?? type.FullName : type.Name;
            }
        }

        /// <summary>
        /// Gets the declaration string for a given type.
        /// </summary>
        /// <param name="type">The type to get the declaration string for.</param>
        /// <returns>A string that ought to look a lot like C#.</returns>
        internal static string GetTypeDeclarationString(Type type)
        {
            var visibility = string.Empty;
            var modifier = string.Empty;
            var codeType = string.Empty;
            var colon = string.Empty;
            var inheritsFrom = string.Empty;

            if (type.IsPublic | type.IsNestedPublic)
            {
                visibility = AssemblyConstants.Public;
            }
            else if (type.IsNestedFamily | type.IsNestedFamORAssem | type.IsNestedFamANDAssem)
            {
                visibility = AssemblyConstants.Protected;
            }
            else
            {
                //Debug.Assert(false, "non public or protected type");
                return string.Empty;
            }

            if (type.IsInterface)
            {
                codeType = AssemblyConstants.Interface;
            }
            else if (type.IsEnum)
            {
                codeType = AssemblyConstants.Enum;
            }
            else if (type.IsValueType)
            {
                codeType = AssemblyConstants.Struct;
            }
            else if (type.IsClass)
            {
                codeType = AssemblyConstants.Class;
                if (type.IsSealed)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Sealed;
                }
                else if (type.IsAbstract)
                {
                    modifier = AssemblyConstants.Space + AssemblyConstants.Abstract;
                }
            }
            else
            {
                modifier = AssemblyConstants.Unknown;
            }

            var baseType = type.BaseType;
            var baseNames = new List<string>();
            var typeName = type.FullName.Replace("[[", "<").Replace("]]", ">");

            if ((null != baseType) && (typeof(object) != baseType) && (typeof(ValueType) != baseType))
            {
                if (typeof(Enum) == baseType)
                {
                    baseType = Enum.GetUnderlyingType(type);
                }
                colon = AssemblyConstants.Colon;
                baseNames.Add(GetParameterTypeString(baseType));
            }

            if (!type.IsEnum)
            {
                var baseInterfaces = type.GetInterfaces();
                Array.Sort(baseInterfaces, TypeComparer.Default);
                baseNames.AddRange(baseInterfaces.Select(c => c.Name));

                inheritsFrom += string.Join(", ", baseNames);
            }

            return $"{visibility}{modifier} {codeType} {typeName}{colon}{inheritsFrom}";
        }

        #endregion

    }

}
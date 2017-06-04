// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using AdvancedREI.Breakdance.Definitions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedREI.Breakdance.Core
{

    /// <summary>
    /// 
    /// </summary>
    public partial class PublicApiHelpers
    {

        #region Private Static Members

        private static readonly ParameterModifier[] EmptyParameterModifiers;
        private static readonly Type[] EmptyTypes;
        internal static readonly BindingFlags VisibleMembers;
        const string Separator = "------------------------------------------------------------";

        #endregion

        #region Private Instance Members

        private readonly List<Assembly> assemblies;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        static PublicApiHelpers()
        {
            EmptyParameterModifiers = new ParameterModifier[0];
            EmptyTypes = new Type[0];
            VisibleMembers = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        }

        /// <summary>
        /// 
        /// </summary>
        public PublicApiHelpers()
        {
            assemblies = new List<Assembly>();
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyList"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetPublicApiSurfaceReport(string[] assemblyList)
        {
            var dictionary = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(assemblyList, c => dictionary.TryAdd(c, GetPublicApiSurfaceReport(c)));
            return dictionary.ToDictionary(c => c.Key, c => c.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static string GetPublicApiSurfaceReport(string assemblyName)
        {
            var typeDefinitions = GenerateTypeDefinitionsForAssembly(assemblyName);
            if (!typeDefinitions.Any()) return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine(Separator);
            sb.AppendLine(assemblyName);
            sb.AppendLine(Separator);
            sb.AppendLine();

            foreach (var typeDefinition in typeDefinitions)
            {
                typeDefinition.Attributes.ForEach(c => sb.AppendLine(c));
                sb.AppendLine(typeDefinition.Class);
                sb.AppendLine(AssemblyConstants.BraceOpen);
                sb.AppendLine();

                foreach (var memberDefinition in typeDefinition.Members)
                {
                    memberDefinition.Attributes.ForEach(c => sb.AppendLine("\t" + c));
                    sb.AppendLine("\t" + memberDefinition.Member);
                    sb.AppendLine();
                }

                sb.AppendLine(AssemblyConstants.BraceClose);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static List<TypeDefinition> GenerateTypeDefinitionsForAssembly (string assemblyName)
        {
            var typesList = new List<Type>();
            var typeDefinitions = new List<TypeDefinition>();
            try
            {
                var assembly = File.Exists(assemblyName) ? Assembly.LoadFrom(assemblyName) : Assembly.Load(assemblyName);
                typesList.AddRange(assembly.GetTypes());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception loading types from assembly '{0}':", assemblyName);
                Console.WriteLine(ex.ToString());
                return typeDefinitions;
            }

            foreach (Type type in typesList.Where(c => !c.IsSpecialName && !c.FullName.StartsWith("<PrivateImplementationDetails>")).OrderBy(c => c.FullName))
            {
                /*
                if(0 <= Array.BinarySearch(_ignoreTypes,typeFullName)) {
                    continue;
                }
                */

                Type declaringType = type;
                while (declaringType != null)
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
                }

                var declarationString = ReadableTypeOutputFactory.GetTypeDeclarationString(type);
                if (string.IsNullOrWhiteSpace(declarationString)) continue;

                var classAttributes = ReadableTypeOutputFactory.GetCustomAttributesList(type.GetCustomAttributes(false));
                var memberDefinitions = GenerateMemberDefinitions(type);

                typeDefinitions.Add(new TypeDefinition(declarationString, classAttributes, memberDefinitions));
            }

            return typeDefinitions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static List<MemberDefinition> GenerateMemberDefinitions(Type type)
        {
            var lines = new List<MemberDefinition>();
            var members = type.GetMembers(VisibleMembers).ToList();
            members.Sort(new MemberComparer(type));

            foreach (var info in members)
            {
                var infoString = string.Empty;
                switch (info.MemberType)
                {
                    case MemberTypes.Constructor:
                        infoString = ReadableTypeOutputFactory.GetConstructorInfoString(type, info as ConstructorInfo);
                        break;
                    case MemberTypes.Event:
                        infoString = ReadableTypeOutputFactory.GetEventInfoString(type, info as EventInfo);
                        break;
                    case MemberTypes.Field:
                        infoString = ReadableTypeOutputFactory.GetFieldInfoString(type, info as FieldInfo);
                        break;
                    case MemberTypes.Method:
                        infoString = ReadableTypeOutputFactory.GetMethodInfoString(type, info as MethodInfo);
                        break;
                    case MemberTypes.Property:
                        infoString = ReadableTypeOutputFactory.GetPropertyInfoString(type, info as PropertyInfo);
                        break;
                    case MemberTypes.NestedType:
                        //DumpClassAPI(builder, info as Type);
                        break;
                    default:
                        infoString = $" {info.Name} ";
                        break;
                }

                if (!string.IsNullOrWhiteSpace(infoString))
                {
                    var attributes = ReadableTypeOutputFactory.GetCustomAttributesList(info.GetCustomAttributes(true));
                    lines.Add(new MemberDefinition(infoString, attributes));
                }
            }

            return lines;
        }

    }

}
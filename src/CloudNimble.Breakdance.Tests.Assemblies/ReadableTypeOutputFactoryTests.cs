using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Tests.Assemblies.SampleApis;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudNimble.Breakdance.Tests.Assemblies
{
    [TestClass]
    public class ReadableTypeOutputFactoryTests
    {

        #region GetConstructorInfo

        [TestMethod]
        public void ReadableTypeOutputFactory_GetConstructorInfo_SomeStaticClass()
        {
            var type = typeof(SomeStaticClass);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var constructorInfo = members.OfType<ConstructorInfo>().FirstOrDefault();
            var constructorString = ReadableTypeOutputFactory.GetConstructorInfoString(type, constructorInfo);

            constructorString.Should().BeNullOrWhiteSpace("Static constructors cannot have parameters, so the API surface never changes.");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetConstructorInfo_SomeGenericClass()
        {
            var type = typeof(SomeGenericClass<string>);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var constructorInfo = members.OfType<ConstructorInfo>().FirstOrDefault();
            var constructorString = ReadableTypeOutputFactory.GetConstructorInfoString(type, constructorInfo);

            constructorString.Should().BeEquivalentTo("public SomeGenericClass`1()");
        }

        #endregion

        #region GetCustomAttributes

        [TestMethod]
        public void ReadableTypeOutputFactory_GetCustomAttributes_ExtensionMethod()
        {
            var members = typeof(SomeExtensionMethod).GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var attributes = ReadableTypeOutputFactory.GetCustomAttributesList(members[0].GetCustomAttributes(true));
            attributes.Should().NotBeEmpty();
            attributes[0].Should().BeEquivalentTo("[System.Runtime.CompilerServices.ExtensionAttribute]");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetCustomAttributes_RegularMethod_NoAttributes()
        {
            var type = typeof(SomeGenericClass<string>);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var methodInfo = members.OfType<MethodInfo>().Where(c => !c.Name.Contains("_")).FirstOrDefault();

            var attributes = ReadableTypeOutputFactory.GetCustomAttributesList(methodInfo.GetCustomAttributes(true));
            attributes.Should().BeEmpty("We have not added any custom attributes to our ToString override.");
        }

        #endregion

        #region GetEventInfo

        [TestMethod]
        public void ReadableTypeOutputFactory_GetEventInfo_SomeStaticClass()
        {
            var type = typeof(SomeStaticClass);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var eventInfo = members.OfType<EventInfo>().FirstOrDefault();
            var eventString = ReadableTypeOutputFactory.GetEventInfoString(type, eventInfo);

            eventString.Should().BeEquivalentTo("CloudNimble.Breakdance.Tests.Assemblies.SampleApis.SomeStaticClass+SomeEventHandler SomeEvent { public static add; public static remove; }");
        }

        #endregion

        #region GetFieldInfoString

        [TestMethod]
        public void ReadableTypeOutputFactory_GetFieldInfo_SomeStaticClass()
        {
            var type = typeof(SomeStaticClass);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var fieldInfo = members.OfType<FieldInfo>().Where(c => !c.Name.Contains("_")).FirstOrDefault();
            var eventString = ReadableTypeOutputFactory.GetFieldInfoString(type, fieldInfo);

            eventString.Should().BeNullOrWhiteSpace("SomeStaticClass only has private backing fields.");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetFieldInfo_SomeGenericClass()
        {
            var type = typeof(SomeGenericClass<string>);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var fieldInfo = members.OfType<FieldInfo>().Where(c => !c.Name.Contains("_")).FirstOrDefault();
            var eventString = ReadableTypeOutputFactory.GetFieldInfoString(type, fieldInfo);

            eventString.Should().BeEquivalentTo("public static string YoMama = \"Yo Mama!\";");
        }

        #endregion

        #region GetMethodInfoString

        [TestMethod]
        public void ReadableTypeOutputFactory_GetMethodInfo_SomeStaticClass()
        {
            var type = typeof(SomeStaticClass);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var methodInfo = members.OfType<MethodInfo>().Where(c => !c.Name.Contains("_")).FirstOrDefault();
            var eventString = ReadableTypeOutputFactory.GetMethodInfoString(type, methodInfo);

            eventString.Should().BeNullOrWhiteSpace("SomeStaticClass only has private backing fields.");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetMethodInfo_SomeGenericClass()
        {
            var type = typeof(SomeGenericClass<string>);
            var members = type.GetMembers(PublicApiHelpers.VisibleMembers).ToList();
            members.Should().NotBeEmpty();

            var methodInfo = members.OfType<MethodInfo>().Where(c => !c.Name.Contains("_")).FirstOrDefault();
            var eventString = ReadableTypeOutputFactory.GetMethodInfoString(type, methodInfo);

            eventString.Should().BeEquivalentTo("public virtual string ToString()");
        }

        #endregion

        #region GetTypeDeclaration

        [TestMethod]
        public void ReadableTypeOutputFactory_GetTypeDeclaration_StaticClass()
        {
            ReadableTypeOutputFactory.GetTypeDeclarationString(typeof(SomeStaticClass))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("public sealed class CloudNimble.Breakdance.Tests.Assemblies.SampleApis.SomeStaticClass");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetTypeDeclaration_InheritedGenericClass()
        {
            ReadableTypeOutputFactory.GetTypeDeclarationString(typeof(SomeStringList))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("public class CloudNimble.Breakdance.Tests.Assemblies.SampleApis.SomeStringList : System.Collections.Generic.List`1<string>, ICollection, IEnumerable, " + 
                "IList, ICollection`1, IEnumerable`1, IList`1, IReadOnlyCollection`1, IReadOnlyList`1");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetTypeDeclaration_GenericClass()
        {
            ReadableTypeOutputFactory.GetTypeDeclarationString(typeof(SomeGenericClass<string>))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("public class CloudNimble.Breakdance.Tests.Assemblies.SampleApis.SomeGenericClass`1<System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089>");
        }


        #endregion

        #region GenericTypeName

        [TestMethod]
        public void ReadableTypeOutputFactory_GetGenericTypeName_List_OfString()
        {
            ReadableTypeOutputFactory.GetGenericTypeName(typeof(List<string>))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("System.Collections.Generic.List`1<string>");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetGenericTypeName_KeyValuePair_OfStringAndGuid()
        {
            ReadableTypeOutputFactory.GetGenericTypeName(typeof(KeyValuePair<string, Guid>))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("System.Collections.Generic.KeyValuePair`2<string, System.Guid>");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetGenericTypeName_KeyValuePair_OfStringAndStringList()
        {
            ReadableTypeOutputFactory.GetGenericTypeName(typeof(KeyValuePair<string, List<string>>))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("System.Collections.Generic.KeyValuePair`2<string, System.Collections.Generic.List`1<string>>");
        }

        [TestMethod]
        public void ReadableTypeOutputFactory_GetGenericTypeName_NotGeneric()
        {
            ReadableTypeOutputFactory.GetGenericTypeName(typeof(string))
                .Should().NotBeNullOrWhiteSpace()
                .And.BeEquivalentTo("string");
        }

        #endregion

    }

}
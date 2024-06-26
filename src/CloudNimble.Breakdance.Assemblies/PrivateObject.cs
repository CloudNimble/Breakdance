﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace CloudNimble.Breakdance.Assemblies
{
    /// <summary>
    /// This class represents the live NON public INTERNAL object in the system
    /// </summary>
    /// <remarks>
    /// This type originally lived in Microsoft.VisualStudio.TestTools.UnitTesting but was removed from V2.
    /// </remarks>
    public class PrivateObject
    {
        // bind everything
        private const BindingFlags BindToEveryThing = BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private static BindingFlags constructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.NonPublic;

        private object target;     // automatically initialized to null
        private Type originalType; // automatically initialized to null

        private Dictionary<string, LinkedList<MethodInfo>> methodCache; // automatically initialized to null

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that contains
        /// the already existing object of the private class
        /// </summary>
        /// <param name="obj"> object that serves as starting point to reach the private members</param>
        /// <param name="memberToAccess">the derefrencing string using . that points to the object to be retrived as in m_X.m_Y.m_Z</param>
        public PrivateObject(object obj, string memberToAccess)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            ValidateAccessString(memberToAccess);
#pragma warning restore CA1062 // Validate arguments of public methods

            PrivateObject temp = obj as PrivateObject;
            temp ??= new PrivateObject(obj);

            // Split The access string
            var arr = memberToAccess.Split(new char[] { '.' });

            for (var i = 0; i < arr.Length; i++)
            {
                object next = temp.InvokeHelper(arr[i], BindToEveryThing | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty, null, CultureInfo.InvariantCulture);
                temp = new PrivateObject(next);
            }

            target = temp.target;
            originalType = temp.originalType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps the
        /// specified type.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly</param>
        /// <param name="typeName">fully qualified name</param>
        /// <param name="args">Argmenets to pass to the constructor</param>
        public PrivateObject(string assemblyName, string typeName, params object[] args)
            : this(assemblyName, typeName, null, args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps the
        /// specified type.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly</param>
        /// <param name="typeName">fully qualified name</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the constructor to get</param>
        /// <param name="args">Arguments to pass to the constructor</param>
        public PrivateObject(string assemblyName, string typeName, Type[] parameterTypes, object[] args)
            : this(Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0}, {1}", typeName, assemblyName), false), parameterTypes, args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps the
        /// specified type.
        /// </summary>
        /// <param name="type">type of the object to create</param>
        /// <param name="args">Arguments to pass to the constructor</param>
        public PrivateObject(Type type, params object[] args)
            : this(type, null, args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps the
        /// specified type.
        /// </summary>
        /// <param name="type">type of the object to create</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the constructor to get</param>
        /// <param name="args">Arguments to pass to the constructor</param>
        public PrivateObject(Type type, Type[] parameterTypes, object[] args)
        {
            Ensure.ArgumentNotNull(type, nameof(type));

            object o;
            if (parameterTypes != null)
            {
                ConstructorInfo ci = type.GetConstructor(BindToEveryThing, null, parameterTypes, null);
                if (ci == null)
                {
                    throw new ArgumentException("The constructor with the specified signature could not be found. You might need to regenerate your private accessor, or the member may be private and defined on a base class. If the latter is true, you need to pass the type that defines the member into PrivateObject's constructor.");
                }

                try
                {
                    o = ci.Invoke(args);
                }
                catch (TargetInvocationException e)
                {
                    Debug.Assert(e.InnerException != null, "Inner exception should not be null.");
                    if (e.InnerException != null)
                    {
                        throw e.InnerException;
                    }

                    throw;
                }
            }
            else
            {
                o = Activator.CreateInstance(type, constructorFlags, null, args, null);
            }

            ConstructFrom(o);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps
        /// the given object.
        /// </summary>
        /// <param name="obj">object to wrap</param>
        public PrivateObject(object obj)
        {
            Ensure.ArgumentNotNull(obj, nameof(obj));

            ConstructFrom(obj);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateObject"/> class that wraps
        /// the given object.
        /// </summary>
        /// <param name="obj">object to wrap</param>
        /// <param name="type">PrivateType object</param>
        public PrivateObject(object obj, PrivateType type)
        {
            Ensure.ArgumentNotNull(type, nameof(type));

            target = obj;
            originalType = type.ReferencedType;
        }

        /// <summary>
        /// Gets or sets the target
        /// </summary>
        public object Target
        {
            get
            {
                return target;
            }

            set
            {
                Ensure.ArgumentNotNull(value, nameof(value));

                target = value;
                originalType = value.GetType();
            }
        }

        /// <summary>
        /// Gets the type of underlying object
        /// </summary>
        public Type RealType
        {
            get
            {
                return originalType;
            }
        }

        private Dictionary<string, LinkedList<MethodInfo>> GenericMethodCache
        {
            get
            {
                if (methodCache == null)
                {
                    BuildGenericMethodCacheForType(originalType);
                }

                Debug.Assert(methodCache != null, "Invalid method cache for type.");

                return methodCache;
            }
        }

        /// <summary>
        /// returns the hash code of the target object
        /// </summary>
        /// <returns>int representing hashcode of the target object</returns>
        public override int GetHashCode()
        {
            Debug.Assert(target != null, "target should not be null.");
            return target.GetHashCode();
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj">Object with whom to compare</param>
        /// <returns>returns true if the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if (this != obj)
            {
                Debug.Assert(target != null, "target should not be null.");
                if (typeof(PrivateObject) == obj?.GetType())
                {
                    return target.Equals(((PrivateObject)obj).target);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, params object[] args)
        {
            return Invoke(name, null, args, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, Type[] parameterTypes, object[] args)
        {
            return Invoke(name, parameterTypes, args, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="typeArguments">An array of types corresponding to the types of the generic arguments.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, Type[] parameterTypes, object[] args, Type[] typeArguments)
        {
            return Invoke(name, BindToEveryThing, parameterTypes, args, CultureInfo.InvariantCulture, typeArguments);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, object[] args, CultureInfo culture)
        {
            return Invoke(name, null, args, culture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, Type[] parameterTypes, object[] args, CultureInfo culture)
        {
            return Invoke(name, BindToEveryThing, parameterTypes, args, culture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, BindingFlags bindingFlags, params object[] args)
        {
            return Invoke(name, bindingFlags, null, args, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, BindingFlags bindingFlags, Type[] parameterTypes, object[] args)
        {
            return Invoke(name, bindingFlags, parameterTypes, args, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, BindingFlags bindingFlags, object[] args, CultureInfo culture)
        {
            return Invoke(name, bindingFlags, null, args, culture);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, BindingFlags bindingFlags, Type[] parameterTypes, object[] args, CultureInfo culture)
        {
            return Invoke(name, bindingFlags, parameterTypes, args, culture, null);
        }

        /// <summary>
        /// Invokes the specified method
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the method to get.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <param name="culture">Culture info</param>
        /// <param name="typeArguments">An array of types corresponding to the types of the generic arguments.</param>
        /// <returns>Result of method call</returns>
        public object Invoke(string name, BindingFlags bindingFlags, Type[] parameterTypes, object[] args, CultureInfo culture, Type[] typeArguments)
        {
            if (parameterTypes != null)
            {
                bindingFlags |= BindToEveryThing | BindingFlags.Instance;

                // Fix up the parameter types
                MethodInfo member = originalType.GetMethod(name, bindingFlags, null, parameterTypes, null);

                // If the method was not found and type arguments were provided for generic paramaters,
                // attempt to look up a generic method.
                if ((member == null) && (typeArguments != null))
                {
                    // This method may contain generic parameters...if so, the previous call to
                    // GetMethod() will fail because it doesn't fully support generic parameters.

                    // Look in the method cache to see if there is a generic method
                    // on the incoming type that contains the correct signature.
                    member = GetGenericMethodFromCache(name, parameterTypes, typeArguments, bindingFlags);
                }

                if (member == null)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, "The member specified ({0}) could not be found. You might need to regenerate your private accessor, or the member may be private and defined on a base class. If the latter is true, you need to pass the type that defines the member into PrivateObject's constructor.", name));
                }

                try
                {
                    if (member.IsGenericMethodDefinition)
                    {
                        MethodInfo constructed = member.MakeGenericMethod(typeArguments);
                        return constructed.Invoke(target, bindingFlags, null, args, culture);
                    }
                    else
                    {
                        return member.Invoke(target, bindingFlags, null, args, culture);
                    }
                }
                catch (TargetInvocationException e)
                {
                    Debug.Assert(e.InnerException != null, "Inner exception should not be null.");
                    if (e.InnerException != null)
                    {
                        throw e.InnerException;
                    }

                    throw;
                }
            }
            else
            {
                return InvokeHelper(name, bindingFlags | BindingFlags.InvokeMethod, args, culture);
            }
        }

        /// <summary>
        /// Gets the array element using array of subsrcipts for each dimension
        /// </summary>
        /// <param name="name">Name of the member</param>
        /// <param name="indices">the indices of array</param>
        /// <returns>An arrya of elements.</returns>
        public object GetArrayElement(string name, params int[] indices)
        {
            return GetArrayElement(name, BindToEveryThing, indices);
        }

        /// <summary>
        /// Sets the array element using array of subsrcipts for each dimension
        /// </summary>
        /// <param name="name">Name of the member</param>
        /// <param name="value">Value to set</param>
        /// <param name="indices">the indices of array</param>
        public void SetArrayElement(string name, object value, params int[] indices)
        {
            SetArrayElement(name, BindToEveryThing, value, indices);
        }

        /// <summary>
        /// Gets the array element using array of subsrcipts for each dimension
        /// </summary>
        /// <param name="name">Name of the member</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="indices">the indices of array</param>
        /// <returns>An arrya of elements.</returns>
        public object GetArrayElement(string name, BindingFlags bindingFlags, params int[] indices)
        {
            Array arr = (Array)InvokeHelper(name, BindingFlags.GetField | bindingFlags, null, CultureInfo.InvariantCulture);
            return arr.GetValue(indices);
        }

        /// <summary>
        /// Sets the array element using array of subsrcipts for each dimension
        /// </summary>
        /// <param name="name">Name of the member</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="value">Value to set</param>
        /// <param name="indices">the indices of array</param>
        public void SetArrayElement(string name, BindingFlags bindingFlags, object value, params int[] indices)
        {
            Array arr = (Array)InvokeHelper(name, BindingFlags.GetField | bindingFlags, null, CultureInfo.InvariantCulture);
            arr.SetValue(value, indices);
        }

        /// <summary>
        /// Get the field
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <returns>The field.</returns>
        public object GetField(string name)
        {
            return GetField(name, BindToEveryThing);
        }

        /// <summary>
        /// Sets the field
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <param name="value">value to set</param>
        public void SetField(string name, object value)
        {
            SetField(name, BindToEveryThing, value);
        }

        /// <summary>
        /// Gets the field
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <returns>The field.</returns>
        public object GetField(string name, BindingFlags bindingFlags)
        {
            return InvokeHelper(name, BindingFlags.GetField | bindingFlags, null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Sets the field
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="value">value to set</param>
        public void SetField(string name, BindingFlags bindingFlags, object value)
        {
            InvokeHelper(name, BindingFlags.SetField | bindingFlags, new object[] { value }, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get the field or property
        /// </summary>
        /// <param name="name">Name of the field or property</param>
        /// <returns>The field or property.</returns>
        public object GetFieldOrProperty(string name)
        {
            return GetFieldOrProperty(name, BindToEveryThing);
        }

        /// <summary>
        /// Sets the field or property
        /// </summary>
        /// <param name="name">Name of the field or property</param>
        /// <param name="value">value to set</param>
        public void SetFieldOrProperty(string name, object value)
        {
            SetFieldOrProperty(name, BindToEveryThing, value);
        }

        /// <summary>
        /// Gets the field or property
        /// </summary>
        /// <param name="name">Name of the field or property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <returns>The field or property.</returns>
        public object GetFieldOrProperty(string name, BindingFlags bindingFlags)
        {
            return InvokeHelper(name, BindingFlags.GetField | BindingFlags.GetProperty | bindingFlags, null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Sets the field or property
        /// </summary>
        /// <param name="name">Name of the field or property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="value">value to set</param>
        public void SetFieldOrProperty(string name, BindingFlags bindingFlags, object value)
        {
            InvokeHelper(name, BindingFlags.SetField | BindingFlags.SetProperty | bindingFlags, new object[] { value }, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>The property.</returns>
        public object GetProperty(string name, params object[] args)
        {
            return GetProperty(name, null, args);
        }

        /// <summary>
        /// Gets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the indexed property.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>The property.</returns>
        public object GetProperty(string name, Type[] parameterTypes, object[] args)
        {
            return GetProperty(name, BindToEveryThing, parameterTypes, args);
        }

        /// <summary>
        /// Set the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="value">value to set</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        public void SetProperty(string name, object value, params object[] args)
        {
            SetProperty(name, null, value, args);
        }

        /// <summary>
        /// Set the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the indexed property.</param>
        /// <param name="value">value to set</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        public void SetProperty(string name, Type[] parameterTypes, object value, object[] args)
        {
            SetProperty(name, BindToEveryThing, value, parameterTypes, args);
        }

        /// <summary>
        /// Gets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>The property.</returns>
        public object GetProperty(string name, BindingFlags bindingFlags, params object[] args)
        {
            return GetProperty(name, bindingFlags, null, args);
        }

        /// <summary>
        /// Gets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the indexed property.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        /// <returns>The property.</returns>
        public object GetProperty(string name, BindingFlags bindingFlags, Type[] parameterTypes, object[] args)
        {
            if (parameterTypes != null)
            {
                PropertyInfo pi = originalType.GetProperty(name, bindingFlags, null, null, parameterTypes, null);
                if (pi == null)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, "The member specified ({0}) could not be found. You might need to regenerate your private accessor, or the member may be private and defined on a base class. If the latter is true, you need to pass the type that defines the member into PrivateObject's constructor.", name));
                }

                return pi.GetValue(target, args);
            }
            else
            {
                return InvokeHelper(name, bindingFlags | BindingFlags.GetProperty, args, null);
            }
        }

        /// <summary>
        /// Sets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="value">value to set</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        public void SetProperty(string name, BindingFlags bindingFlags, object value, params object[] args)
        {
            SetProperty(name, bindingFlags, value, null, args);
        }

        /// <summary>
        /// Sets the property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="bindingFlags">A bitmask comprised of one or more <see cref="T:System.Reflection.BindingFlags"/> that specify how the search is conducted.</param>
        /// <param name="value">value to set</param>
        /// <param name="parameterTypes">An array of <see cref="T:System.Type"/> objects representing the number, order, and type of the parameters for the indexed property.</param>
        /// <param name="args">Arguments to pass to the member to invoke.</param>
        public void SetProperty(string name, BindingFlags bindingFlags, object value, Type[] parameterTypes, object[] args)
        {
            if (parameterTypes != null)
            {
                PropertyInfo pi = originalType.GetProperty(name, bindingFlags, null, null, parameterTypes, null);
                if (pi == null)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, "The member specified ({0}) could not be found. You might need to regenerate your private accessor, or the member may be private and defined on a base class. If the latter is true, you need to pass the type that defines the member into PrivateObject's constructor.", name));
                }

                pi.SetValue(target, value, args);
            }
            else
            {
                object[] pass = new object[(args?.Length ?? 0) + 1];
                pass[0] = value;
                args?.CopyTo(pass, 1);
                InvokeHelper(name, bindingFlags | BindingFlags.SetProperty, pass, null);
            }
        }

        /// <summary>
        /// Validate access string
        /// </summary>
        /// <param name="access"> access string</param>
        private static void ValidateAccessString(string access)
        {
            if (access.Length == 0)
            {
                throw new ArgumentException("Access string has invalid syntax.");
            }

            string[] arr = access.Split('.');
            foreach (string str in arr)
            {
                if ((str.Length == 0) || (str.IndexOfAny(new char[] { ' ', '\t', '\n' }) != -1))
                {
                    throw new ArgumentException("Access string has invalid syntax.");
                }
            }
        }

        /// <summary>
        /// Invokes the memeber
        /// </summary>
        /// <param name="name">Name of the member</param>
        /// <param name="bindingFlags">Additional attributes</param>
        /// <param name="args">Arguments for the invocation</param>
        /// <param name="culture">Culture</param>
        /// <returns>Result of the invocation</returns>
        private object InvokeHelper(string name, BindingFlags bindingFlags, object[] args, CultureInfo culture)
        {
            Debug.Assert(target != null, "Internal Error: Null reference is returned for internal object");

            // Invoke the actual Method
            try
            {
                return originalType.InvokeMember(name, bindingFlags, null, target, args, culture);
            }
            catch (TargetInvocationException e)
            {
                Debug.Assert(e.InnerException != null, "Inner exception should not be null.");
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }

                throw;
            }
        }

        private void ConstructFrom(object obj)
        {
            target = obj;
            originalType = obj.GetType();
        }

        private void BuildGenericMethodCacheForType(Type t)
        {
            Debug.Assert(t != null, "type should not be null.");
            methodCache = new Dictionary<string, LinkedList<MethodInfo>>();

            MethodInfo[] members = t.GetMethods(BindToEveryThing);
            LinkedList<MethodInfo> listByName; // automatically initialized to null

            foreach (MethodInfo member in members)
            {
                if (member.IsGenericMethod || member.IsGenericMethodDefinition)
                {
                    if (!GenericMethodCache.TryGetValue(member.Name, out listByName))
                    {
                        listByName = new LinkedList<MethodInfo>();
                        GenericMethodCache.Add(member.Name, listByName);
                    }

                    Debug.Assert(listByName != null, "list should not be null.");
                    listByName.AddLast(member);
                }
            }
        }

        /// <summary>
        /// Extracts the most appropriate generic method signature from the current private type.
        /// </summary>
        /// <param name="methodName">The name of the method in which to search the signature cache.</param>
        /// <param name="parameterTypes">An array of types corresponding to the types of the parameters in which to search.</param>
        /// <param name="typeArguments">An array of types corresponding to the types of the generic arguments.</param>
        /// <param name="bindingFlags"><see cref="BindingFlags"/> to further filter the method signatures.</param>
        /// <returns>A methodinfo instance.</returns>
        private MethodInfo GetGenericMethodFromCache(string methodName, Type[] parameterTypes, Type[] typeArguments, BindingFlags bindingFlags)
        {
            Debug.Assert(!string.IsNullOrEmpty(methodName), "Invalid method name.");
            Debug.Assert(parameterTypes != null, "Invalid parameter type array.");
            Debug.Assert(typeArguments != null, "Invalid type arguments array.");

            // Build a preliminary list of method candidates that contain roughly the same signature.
            var methodCandidates = GetMethodCandidates(methodName, parameterTypes, typeArguments, bindingFlags);

            // Search of ambiguous methods (methods with the same signature).
            MethodInfo[] finalCandidates = new MethodInfo[methodCandidates.Count];
            methodCandidates.CopyTo(finalCandidates, 0);

            if ((parameterTypes != null) && (parameterTypes.Length == 0))
            {
                for (int i = 0; i < finalCandidates.Length; i++)
                {
                    MethodInfo methodInfo = finalCandidates[i];

                    if (!RuntimeTypeHelper.CompareMethodSigAndName(methodInfo, finalCandidates[0]))
                    {
                        throw new AmbiguousMatchException();
                    }
                }

                // All the methods have the exact same name and sig so return the most derived one.
                return RuntimeTypeHelper.FindMostDerivedNewSlotMeth(finalCandidates, finalCandidates.Length) as MethodInfo;
            }

            // Now that we have a preliminary list of candidates, select the most appropriate one.
            return RuntimeTypeHelper.SelectMethod(finalCandidates, parameterTypes) as MethodInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameterTypes"></param>
        /// <param name="typeArguments"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        private LinkedList<MethodInfo> GetMethodCandidates(string methodName, Type[] parameterTypes, Type[] typeArguments, BindingFlags bindingFlags)
        {
            Debug.Assert(!string.IsNullOrEmpty(methodName), "methodName should not be null.");
            Debug.Assert(parameterTypes != null, "parameterTypes should not be null.");
            Debug.Assert(typeArguments != null, "typeArguments should not be null.");

            var methodCandidates = new LinkedList<MethodInfo>();
            if (!GenericMethodCache.TryGetValue(methodName, out var methods))
            {
                return methodCandidates;
            }

            Debug.Assert(methods != null, "methods should not be null.");

            foreach (var candidate in methods)
            {
                var paramMatch = true;
                var genericArgs = candidate.GetGenericArguments();
                if (genericArgs.Length != typeArguments.Length)
                {
                    continue;
                }

                // Since we can't just get the correct MethodInfo from Reflection,
                // we will just match the number of parameters, their order, and their type
                var methodCandidate = candidate;
                var candidateParams = methodCandidate.GetParameters();

                if (candidateParams.Length != parameterTypes.Length)
                {
                    continue;
                }

                // Exact binding
                if ((bindingFlags & BindingFlags.ExactBinding) != 0)
                {
                    var i = 0;

                    foreach (var candidateParam in candidateParams)
                    {
                        var sourceParameterType = parameterTypes[i++];
                        if (candidateParam.ParameterType.ContainsGenericParameters)
                        {
                            // Since we have a generic parameter here, just make sure the IsArray matches.
                            if (candidateParam.ParameterType.IsArray != sourceParameterType.IsArray)
                            {
                                paramMatch = false;
                                break;
                            }
                        }
                        else
                        {
                            if (candidateParam.ParameterType != sourceParameterType)
                            {
                                paramMatch = false;
                                break;
                            }
                        }
                    }

                    if (paramMatch)
                    {
                        methodCandidates.AddLast(methodCandidate);
                        continue;
                    }
                }
                else
                {
                    methodCandidates.AddLast(methodCandidate);
                }
            }

            return methodCandidates;
        }
    }

}

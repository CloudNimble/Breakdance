// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Core;
using System;
using System.Collections;
using System.Globalization;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// Legacy class used to compare members.
    /// </summary>
    /// <remarks>Should be rewritten or eliminated at our earliest possible convenience.</remarks>
    sealed public class TypeComparer : IComparer
    {

        #region Private Members

        private const bool AlphabeticalGrouping = false;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public static readonly TypeComparer Default = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            Ensure.ArgumentNotNull(x, nameof(x));
            Ensure.ArgumentNotNull(y, nameof(y));

            Type a = x as Type;
            Type b = y as Type;

            string c = a.FullName ?? a.Name;
            string d = b.FullName ?? b.Name;

            int ac = 0, bc = 0;

            for (int i = 0; i < c.Length; ++i)
            {
                if ('.' == c[i]) ac++;
            }
            for (int i = 0; i < d.Length; ++i)
            {
                if ('.' == d[i]) bc++;
            }
            int cmp = ac - bc;
            if (0 == cmp)
            {
                if (!AlphabeticalGrouping)
                {
                    string e = (0 < ac) ? c.Substring(0, c.LastIndexOf('.')) : null;
                    string f = (0 < bc) ? d.Substring(0, d.LastIndexOf('.')) : null;

                    if (0 == String.Compare(e, f, false, CultureInfo.InvariantCulture))
                    {
                        if (a.IsEnum)
                        {
                            if (!b.IsEnum)
                            {
                                cmp = -1;
                            }
                        }
                        else if (a.IsValueType)
                        {
                            if (b.IsEnum)
                            {
                                cmp = 1;
                            }
                            else if (!b.IsValueType)
                            {
                                cmp = -1;
                            }
                        }
                        else if (b.IsEnum || b.IsValueType)
                        {
                            cmp = 1;
                        }
                        if (0 == cmp)
                        {
                            if (a.IsInterface != b.IsInterface)
                            {
                                cmp = (a.IsInterface ? -1 : 1);
                            }
                            else if (a.IsAbstract != b.IsAbstract)
                            {
                                cmp = (a.IsAbstract ? -1 : 1);
                            }
                            else if (a.IsSealed != b.IsSealed)
                            {
                                cmp = (a.IsSealed ? 1 : -1);
                            }
                        }
                    }
                }
                if (0 == cmp)
                {
                    cmp = String.Compare(c, d, false, CultureInfo.InvariantCulture);
                }
            }
            return cmp;
        }

    }

}
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Reflection;

namespace AdvancedREI.Testier.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    sealed public class AssemblyComparer : IComparer
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            string a = ((Assembly)x).GetName().Name;
            string b = ((Assembly)y).GetName().Name;
            int ac = 0, bc = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                if ('.' == a[i]) ac++;
            }
            for (int i = 0; i < b.Length; ++i)
            {
                if ('.' == b[i]) bc++;
            }
            int cmp = ac - bc;
            if (0 == cmp)
            {
                cmp = String.Compare(a, b);
            }
            return cmp;
        }
    }

}
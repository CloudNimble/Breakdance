// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;

namespace AdvancedREI.Breakdance.Core
{

    /// <summary>
    /// 
    /// </summary>
    sealed public class ObjectTypeComparer : IComparer
    {
        static public readonly ObjectTypeComparer Default = new ObjectTypeComparer();
        public int Compare(object x, object y)
        {
            string a = x.GetType().FullName;
            string b = y.GetType().FullName;
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
                cmp = String.Compare(a, b, false, CultureInfo.InvariantCulture);
            }
            return cmp;
        }
    }

}
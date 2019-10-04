// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// Legacy class used to compare types.
    /// </summary>
    /// <remarks>Should be rewritten or eliminated at our earliest possible convenience.</remarks>
    sealed public class ObjectTypeComparer : IComparer, IComparer<object>
    {

        /// <summary>
        /// 
        /// </summary>
        static public readonly ObjectTypeComparer Default = new ObjectTypeComparer();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

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
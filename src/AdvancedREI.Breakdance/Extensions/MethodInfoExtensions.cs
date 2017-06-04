namespace System.Reflection
{

    /// <summary>
    /// 
    /// </summary>
    public static class MethodBaseExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsProtected(this ConstructorInfo info, Type type)
        {
            return IsProtectedInternal(info, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsProtected(this FieldInfo info, Type type)
        {
            return !type.IsSealedClass() && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsProtected(this MethodInfo info, Type type)
        {
            return IsProtectedInternal(info, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsProtectedInternal(this MethodBase info, Type type)
        {
            return !type.IsSealedClass() && (info.IsFamily || info.IsFamilyAndAssembly || info.IsFamilyOrAssembly);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsSealedClass(this Type type)
        {
            return type.IsClass && type.IsSealed;
        }

    }

}
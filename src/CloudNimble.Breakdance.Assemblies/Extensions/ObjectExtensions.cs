using System.Reflection;

namespace System
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Taken from https://stackoverflow.com/a/60529530/403765
    /// </remarks>
    public static class ObjectExtensions
    {

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="throwIfNull"></param>
        /// <returns></returns>
        public static object GetFieldValue(this object obj, string fieldName, bool throwIfNull = true)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var objType = obj.GetType();
            var fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null && throwIfNull)
            {
                throw new ArgumentOutOfRangeException(fieldName, $"Couldn't find field {fieldName} in type {objType.FullName}");
            }
            return fieldInfo?.GetValue(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        public static void SetFieldValue(this object obj, string fieldName, object val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var objType = obj.GetType();
            var fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException(fieldName, $"Couldn't find field {fieldName} in type {objType.FullName}");
            }
            fieldInfo.SetValue(obj, val);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="throwIfNull"></param>
        /// <returns></returns>
        public static object GetPropertyValue(this object obj, string propertyName, bool throwIfNull = true)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var objType = obj.GetType();
            var propertyInfo = GetPropertyInfo(objType, propertyName);
            if (propertyInfo == null && throwIfNull)
            {
                throw new ArgumentOutOfRangeException(propertyName, $"Couldn't find property {propertyName} in type {objType.FullName}");
            }
            return propertyInfo?.GetValue(obj, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var objType = obj.GetType();
            var propertyInfo = GetPropertyInfo(objType, propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentOutOfRangeException(propertyName, $"Couldn't find property {propertyName} in type {objType.FullName}");
            }
            propertyInfo.SetValue(obj, val, null);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            FieldInfo fieldInfo;
            do
            {
                fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);

            return fieldInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propertyInfo;
            do
            {
                propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (propertyInfo == null && type != null);

            return propertyInfo;
        }

        #endregion

    }
}

namespace System.Web.Providers
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Web.Providers.Resources;

    internal static class ValidationHelper
    {
        internal static Exception CheckArrayParameter(ref string[] param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                return new ArgumentNullException(paramName);
            }
            if (param.Length < 1)
            {
                return new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_array_empty, new object[] { paramName }), paramName);
            }
            Hashtable hashtable = new Hashtable(param.Length);
            for (int i = param.Length - 1; i >= 0; i--)
            {
                CheckParameter(ref param[i], checkForNull, checkIfEmpty, checkForCommas, maxSize, paramName + "[ " + i.ToString(CultureInfo.InvariantCulture) + " ]");
                if (hashtable.Contains(param[i]))
                {
                    return new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_duplicate_array_element, new object[] { paramName }), paramName);
                }
                hashtable.Add(param[i], param[i]);
            }
            return null;
        }

        internal static Exception CheckParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize, string paramName)
        {
            if (param == null)
            {
                if (checkForNull)
                {
                    return new ArgumentNullException(paramName);
                }
                return null;
            }
            param = param.Trim();
            if (checkIfEmpty && (param.Length < 1))
            {
                return new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_can_not_be_empty, new object[] { paramName }), paramName);
            }
            if ((maxSize > 0) && (param.Length > maxSize))
            {
                return new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_too_long, new object[] { paramName, maxSize.ToString(CultureInfo.InvariantCulture) }), paramName);
            }
            if (checkForCommas && param.Contains(","))
            {
                return new ArgumentException(string.Format(CultureInfo.CurrentCulture, ProviderResources.Parameter_can_not_contain_comma, new object[] { paramName }), paramName);
            }
            return null;
        }

        internal static bool ValidateParameter(ref string param, bool checkForNull, bool checkIfEmpty, bool checkForCommas, int maxSize)
        {
            if (param == null)
            {
                return !checkForNull;
            }
            param = param.Trim();
            return (((!checkIfEmpty || (param.Length >= 1)) && ((maxSize <= 0) || (param.Length <= maxSize))) && (!checkForCommas || !param.Contains(",")));
        }
    }
}


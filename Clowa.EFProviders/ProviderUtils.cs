using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Linq.Expressions;

namespace Clowa.EFProviders
{
    internal static class ProviderUtils
    {
        /// <summary>
        /// A helper function to retrieve config values from the configuration file.
        /// </summary>
        /// <param name="config">Provider configuration.</param>
        /// <param name="configKey">Key of the configuration that should be read.</param>
        /// <param name="defaultValue">Default value being used if the config does not exist.</param>
        /// <returns>Configuration value or default value if not exisiting.</returns>
        public static object GetConfigValue(NameValueCollection config, string configKey, object defaultValue)
        {
            object configValue;

            try
            {
                configValue = config[configKey];
                configValue = string.IsNullOrEmpty(configValue.ToString()) ? defaultValue : configValue;
            }
            catch
            {
                configValue = defaultValue;
            }

            return configValue;
        }

        /// <summary>
        /// Ensure that application exists. If not -> create new application.
        /// </summary>
        /// <param name="applicationName">Application name.</param>
        /// <param name="context">Entity Framework data context.</param>
        /// <returns>The application object</returns>
        public static Application EnsureApplication(string applicationName, EFProviders.MemberShip context)
        {
            Application application = context.Application.Where(a => a.Name == applicationName).FirstOrDefault();
            if (application == null)
            {
                // Create application
                application = Application.CreateApplication(Guid.NewGuid(), applicationName);
                context.AddToApplication(application);
                context.SaveChanges();
            }

            return application;
        }

        /// <summary>
        /// Builds a contains expression.
        /// </summary>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="valueSelector">The value selector.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public static Expression<Func<TElement, bool>> BuildContainsExpression<TElement, TValue>(Expression<Func<TElement, TValue>> valueSelector, IEnumerable<TValue> values)
        {
            if (null == valueSelector)
            {
                throw new ArgumentNullException("valueSelector");
            }

            if (null == values)
            {
                throw new ArgumentNullException("values");
            }

            ParameterExpression p = valueSelector.Parameters.Single();

            if (!values.Any())
            {
                return e => false;
            }

            IEnumerable<Expression> equals = values.Select(value => (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(TValue))));
            Expression body = equals.Aggregate(Expression.Or);
            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
    }
}

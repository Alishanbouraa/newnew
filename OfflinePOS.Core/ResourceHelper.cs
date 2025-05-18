// OfflinePOS.Core/ResourceHelper.cs
using System;
using System.Windows;

namespace OfflinePOS.Core
{
    /// <summary>
    /// Provides helper methods for managing application resources
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Gets a ResourceDictionary containing common application styles
        /// </summary>
        /// <returns>ResourceDictionary with common styles</returns>
        public static ResourceDictionary GetCommonResourceDictionary()
        {
            return new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/OfflinePOS.Core;component/Styles/CommonStyles.xaml")
            };
        }

        /// <summary>
        /// Ensures that common resources are loaded in the specified ResourceDictionary
        /// </summary>
        /// <param name="targetDictionary">The dictionary to check and update</param>
        public static void EnsureResourcesLoaded(ResourceDictionary targetDictionary)
        {
            // Ensure common styles are loaded
            var commonStylesUri = new Uri("pack://application:,,,/OfflinePOS.Core;component/Styles/CommonStyles.xaml");
            bool hasCommonStyles = false;

            foreach (var dict in targetDictionary.MergedDictionaries)
            {
                if (dict.Source == commonStylesUri)
                {
                    hasCommonStyles = true;
                    break;
                }
            }

            if (!hasCommonStyles)
            {
                targetDictionary.MergedDictionaries.Add(GetCommonResourceDictionary());
            }
        }

        /// <summary>
        /// Gets a resource from the application resources
        /// </summary>
        /// <typeparam name="T">Type of resource to retrieve</typeparam>
        /// <param name="resourceKey">Resource key</param>
        /// <returns>The requested resource or default value if not found</returns>
        public static T GetResource<T>(string resourceKey)
        {
            if (Application.Current.Resources.Contains(resourceKey))
            {
                return (T)Application.Current.Resources[resourceKey];
            }

            return default;
        }
    }
}
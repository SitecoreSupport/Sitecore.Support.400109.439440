using System;
using System.Web.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.DependencyInjection;
using Sitecore.ExperienceForms.Mvc;

namespace Sitecore.Support.ExperienceForms.Mvc.Filters
{
    /// <summary>
    /// Holds reference to methods and classes used by the <see cref="ValidateFormRequestAttribute"/>
    /// </summary>
    internal class ValidateFormRequestContext
    {
        /// <summary>
        /// Gets the current Sitecore context page mode <see cref="Sitecore.Context.PageMode"/> 
        /// </summary>
        internal virtual bool IsExperienceEditor => Context.PageMode.IsExperienceEditor;

        /// <summary>
        /// Gets the form rendering context <see cref="IFormRenderingContext"/> from the services container.
        /// </summary>
        internal virtual IFormRenderingContext FormRenderingContext => ServiceLocator.ServiceProvider.GetService<IFormRenderingContext>();

        /// <summary>
        /// Gets the action to be invoked upon validation.
        /// By default returns reference to <see cref="AntiForgery"/> validation operation.
        /// </summary>
        internal virtual Action Validate => AntiForgery.Validate;
    }
}
using System;
using System.Web.Mvc;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Mvc.Constants;

namespace Sitecore.Support.ExperienceForms.Mvc.Filters
{
    /// <summary>
    /// Performs antiforgery validation using <see cref="System.Web.Helpers.AntiForgery"/>.
    /// </summary>
    /// <remarks>
    /// Validation is not performed when Form Id is missing in the request parameters 
    /// and request is executed in Edit mode by Experience Editor.
    /// </remarks> 
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class ValidateFormRequestAttribute : FilterAttribute, IAuthorizationFilter
    {
        /// <summary>
        /// Request validation context <see cref="Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequestContext"/>.
        /// </summary>
        private readonly Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequestContext _validationContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateFormRequestAttribute" /> class.
        /// </summary>
        public ValidateFormRequestAttribute()
            : this(new Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequestContext())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateFormRequestAttribute" /> class.
        /// </summary>
        /// <param name="validateFormAntiForgeryContext">Request validation context <see cref="Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequestContext"/>.</param>
        internal ValidateFormRequestAttribute(Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequestContext validateFormAntiForgeryContext)
        {
            Assert.ArgumentNotNull(validateFormAntiForgeryContext, nameof(validateFormAntiForgeryContext));
            _validationContext = validateFormAntiForgeryContext;
        }

        /// <inheritdoc />
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            Assert.ArgumentNotNull(filterContext, nameof(filterContext));

            var idPropertyName = _validationContext.FormRenderingContext.CreatePropertyName(AttributeNames.FormItemId);
            var formId = filterContext.RequestContext.HttpContext.Request.Form[idPropertyName];
            if (string.IsNullOrEmpty(formId) && _validationContext.IsExperienceEditor)
            {
                return;
            }

            _validationContext.Validate();
        }
    }
}

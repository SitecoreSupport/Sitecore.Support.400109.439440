using System;
using System.Web.Mvc;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Constants;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc;
using Sitecore.ExperienceForms.Mvc.Constants;
using Sitecore.ExperienceForms.Mvc.Extensions;
using Sitecore.ExperienceForms.Mvc.Filters;
using Sitecore.ExperienceForms.Mvc.Pipelines.GetModel;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Tracking;
using Sitecore.Globalization;
using Sitecore.Mvc.Filters;
using Sitecore.Mvc.Pipelines;
using Sitecore.Web;
using PipelineNames = Sitecore.ExperienceForms.Mvc.Constants.PipelineNames;

namespace Sitecore.Support.ExperienceForms.Mvc.Controllers
{
    /// <summary>
    /// Handles form rendering for MVC controls.
    /// </summary>
    /// <seealso cref="System.Web.Mvc.Controller" />
    public class FormBuilderController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormBuilderController" /> class.
        /// </summary>
        /// <param name="formRenderingContext">The form rendering context.</param>
        /// <param name="formSubmitHandler">The submit form manager.</param>
        public FormBuilderController(IFormRenderingContext formRenderingContext, IFormSubmitHandler formSubmitHandler)
        {
            Assert.ArgumentNotNull(formRenderingContext, nameof(formRenderingContext));
            Assert.ArgumentNotNull(formSubmitHandler, nameof(formSubmitHandler));

            FormRenderingContext = formRenderingContext;
            FormSubmitHandler = formSubmitHandler;
        }

        /// <summary>
        /// Gets or sets the form submit handler.
        /// </summary>
        protected IFormSubmitHandler FormSubmitHandler { get; set; }

        /// <summary>
        /// The form rendering context.
        /// </summary>
        protected IFormRenderingContext FormRenderingContext { get; }

        /// <summary>
        /// Loads the form assigned to the current rendering.
        /// </summary>
        /// <returns>The rendered form <see cref="PartialViewResult"/></returns>
        [HttpGet]
        [SetFormMode(Editing = false)]
        public ActionResult Index()
        {
            if (!string.IsNullOrEmpty(HttpContext.Request.QueryString["fxb.FormItemId"]) || 
                !string.IsNullOrEmpty(HttpContext.Request.QueryString["fxb.HtmlPrefix"]))
            {
                HttpContext.Response.Redirect(Settings.ItemNotFoundUrl);
            }
            FormRenderingContext.SessionId = ID.NewID.ToClientIdString();
            return RenderForm(FormRenderingContext.RenderingFormId);
        }

        /// <summary>
        /// Loads the form with the specified id or an empty form.
        /// </summary>
        /// <param name="id">The form id.</param>
        /// <returns>The rendered form <see cref="PartialViewResult"/></returns>
        /// <remarks>Called only by authorized users from the Form Designer application.</remarks>
        [HttpGet]
        [SitecoreAuthorize(Roles = SitecoreRoles.FormsEditor)]
        [SetFormMode(Editing = true)]
        public ActionResult Load(string id)
        {
            FormRenderingContext.SessionId = ID.NewID.ToClientIdString();
            return RenderForm(id);
        }

        /// <summary>
        /// Executes a post request with the specified data.
        /// </summary>
        /// <param name="data">The posted form data <see cref="FormDataModel" />.</param>
        /// <returns>
        /// The result of the post execution.
        /// </returns>
        [HttpPost]
        [Sitecore.Support.ExperienceForms.Mvc.Filters.ValidateFormRequest]
        public ActionResult Index(FormDataModel data)
        {
            if (data == null)
            {
                return this.Index();
            }

            if (data.NavigationData.NavigationType == NavigationType.Submit)
            {
                FormRenderingContext.RegisterFormEvent(new FormEventData
                {
                    FormId = Guid.Parse(data.FormItemId),
                    EventId = FormPageEventIds.FormSubmitEventId
                });
            }

            FormRenderingContext.StorePostedFields(data.Fields);

            var errors = true;
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(data.NavigationData.ButtonId))
                {
                  return this.Index();
                }

                var submitContext = new FormSubmitContext(data.NavigationData.ButtonId)
                {
                    FormId = Guid.Parse(data.FormItemId),
                    SessionId = Guid.Parse(data.SessionData?.SessionId ?? Guid.Empty.ToString()),
                    Fields = FormRenderingContext.PostedFields
                };

                FormSubmitHandler.Submit(submitContext);
                errors = submitContext.HasErrors;

                if (!errors)
                {
                    if (data.NavigationData.NavigationType == NavigationType.Submit)
                    {
                        return GetSubmitActionResult(submitContext);
                    }
                }
                else
                {
                    foreach (var submitContextError in submitContext.Errors)
                    {
                        ModelState.AddModelError(FormRenderingContext.Prefix, submitContextError.ErrorMessage);
                    }
                }
            }

            if (errors && data.NavigationData.NavigationType != NavigationType.Back)
            {
                FormRenderingContext.RegisterFormEvent(new FormEventData
                {
                    FormId = Guid.Parse(data.FormItemId),
                    EventId = FormPageEventIds.FormErrorEventId
                });

                data.NavigationData.Step = 0;
            }

            FormRenderingContext.NavigationData = data.NavigationData;

            return RenderForm(data.FormItemId);
        }

        /// <summary>
        /// Gets the submit action result.
        /// </summary>
        /// <param name="submitContext">The submit context.</param>
        /// <returns>The <see cref="ActionResult" /> for the submit.</returns>
        protected ActionResult GetSubmitActionResult(FormSubmitContext submitContext)
        {
            Assert.ArgumentNotNull(submitContext, nameof(submitContext));

            FormRenderingContext.RegisterFormEvent(new FormEventData
            {
                FormId = submitContext.FormId,
                EventId = FormPageEventIds.FormSubmitSuccessEventId
            });

            FormRenderingContext.ResetFormSessionData();

            if (submitContext.RedirectOnSuccess)
            {
                if (!string.IsNullOrEmpty(submitContext.RedirectUrl) && !WebUtil.IsOnPage(submitContext.RedirectUrl))
                {
                    if (Request.IsAjaxRequest())
                    {
                        return new JavaScriptResult
                        {
                            Script = "window.location='" + submitContext.RedirectUrl + "';"
                        };
                    }

                    return Redirect(submitContext.RedirectUrl);
                }
            }

            return Index();
        }

        /// <summary>
        /// Renders the form with the specified id if the id is not empty;
        /// otherwise, renders an empty form.
        /// </summary>
        /// <param name="id">The form id.</param>
        /// <returns>The rendered form <see cref="PartialViewResult"/></returns>
        protected virtual ActionResult RenderForm(string id)
        {
            using (var getModelArgs = new GetModelEventArgs())
            {
                getModelArgs.ItemId = id;
                getModelArgs.TemplateId = TemplateIds.FormTemplateId;
                var result = PipelineService.Get().RunPipeline(PipelineNames.GetModel, getModelArgs, a => a);
                if (result.ViewModel == null)
                {
                    return HttpNotFound(Translate.Text("Item not found"));
                }

                return PartialView(result.RenderingSettings.ViewPath, result.ViewModel);
            }
        }
    }
}
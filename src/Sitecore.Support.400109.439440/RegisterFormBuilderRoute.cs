﻿using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace Sitecore.Support.ExperienceForms.Mvc.Pipelines.Initialize
{
    public class InitializeRoutes : Sitecore.Mvc.Pipelines.Loader.InitializeRoutes
    {
        protected override void RegisterRoutes(RouteCollection routes, PipelineArgs args)
        {
            routes.MapRoute("FormBuilder", "formbuilder/{action}/{id}", new
            {
                controller = "FormBuilder",
                action = "Index",
                id = UrlParameter.Optional
            }, new[] { "Sitecore.Support.ExperienceForms.Mvc.Controllers" });
            routes.MapRoute("FieldTracking", "fieldtracking/{action}", new
            {
                controller = "FieldTracking",
                action = "Register"
            });
        }
    }

}
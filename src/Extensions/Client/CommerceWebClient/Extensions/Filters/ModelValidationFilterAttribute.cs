﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using VirtoCommerce.Web.Client.Globalization;


namespace VirtoCommerce.Web.Client.Extensions.Filters
{
	/// <summary>
	/// Class ModelValidationFilterAttribute.
	/// </summary>
    public class ModelValidationFilterAttribute : ActionFilterAttribute
    {
		/// <summary>
		/// Occurs after the action method is invoked.
		/// </summary>
		/// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var modelState = actionExecutedContext.ActionContext.ModelState;
            if (!modelState.IsValid)
            {
                var errors = modelState
                    .Where(s => s.Value.Errors.Count > 0)
                    .Select(s => new KeyValuePair<string, string>(s.Key.Substring(s.Key.IndexOf(".") + 1), s.Value.Errors.First().ErrorMessage.Localize()))
                    .ToArray();

                actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(HttpStatusCode.BadRequest, errors);
            }

            base.OnActionExecuted(actionExecutedContext);
        }
    }
}
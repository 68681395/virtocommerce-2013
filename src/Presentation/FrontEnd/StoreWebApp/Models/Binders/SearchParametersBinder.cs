﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Specialized;
using VirtoCommerce.Web.Helpers;
using System.Text.RegularExpressions;
using VirtoCommerce.Web.Client.Extensions;

namespace VirtoCommerce.Web.Models.Binders
{
	/// <summary>
	/// Class SearchParametersBinder.
	/// </summary>
    public class SearchParametersBinder : IModelBinder
    {
		/// <summary>
		/// The default page size
		/// </summary>
        public const int DefaultPageSize = SearchParameters.DefaultPageSize;

		/// <summary>
		/// Name values to dictionary.
		/// </summary>
		/// <param name="nv">The nv.</param>
		/// <returns>IDictionary{System.StringSystem.String}.</returns>
        public IDictionary<string, string> NvToDict(NameValueCollection nv)
        {
            var d = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var k in nv.AllKeys)
                d[k] = nv[k];
            return d;
        }

		/// <summary>
		/// The facet regex
		/// </summary>
        private static readonly Regex FacetRegex = new Regex("^f_", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Binds the model to a value by using the specified controller context and binding context.
		/// </summary>
		/// <param name="controllerContext">The controller context.</param>
		/// <param name="bindingContext">The binding context.</param>
		/// <returns>The bound value.</returns>
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var qs = controllerContext.HttpContext.Request.QueryString;
            var qsDict = NvToDict(qs);
            var sp = new SearchParameters
            {
                FreeSearch = qs["q"].EmptyToNull(),
                PageIndex = qs["p"].TryParse(1),
                PageSize = qs["pageSize"].TryParse(DefaultPageSize),
                Sort = qs["sort"].EmptyToNull(),
                Facets = qsDict.Where(k => FacetRegex.IsMatch(k.Key))
                    .Select(k => k.WithKey(FacetRegex.Replace(k.Key, "")))
                    .ToDictionary()
            };
            return sp;
        }
    }
}
﻿using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VirtoCommerce.Client;
using VirtoCommerce.Foundation.AppConfig.Model;
using VirtoCommerce.Foundation.Catalogs.Model;
using VirtoCommerce.Foundation.Frameworks.Tagging;
using VirtoCommerce.Web.Client.Extensions.Filters;
using VirtoCommerce.Web.Models;
using VirtoCommerce.Web.Virto.Helpers;
using AppConfigContext = VirtoCommerce.Foundation.AppConfig.Model.ContextFieldConstants;
using ContextFieldConstants = VirtoCommerce.Foundation.Frameworks.ContextFieldConstants;

namespace VirtoCommerce.Web.Controllers
{
	/// <summary>
	/// Class CatalogController.
	/// </summary>
	[Localize]
	public class CatalogController : ControllerBase
    {
		/// <summary>
		/// The _catalog client
		/// </summary>
        private readonly CatalogClient _catalogClient;

		/// <summary>
		/// The _template client
		/// </summary>
        private readonly DisplayTemplateClient _templateClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="CatalogController"/> class.
		/// </summary>
		/// <param name="catalogClient">The catalog client.</param>
		/// <param name="templateClient">The template client.</param>
		public CatalogController(CatalogClient catalogClient,
                                 DisplayTemplateClient templateClient)
        {
			_catalogClient = catalogClient;
            _templateClient = templateClient;
        }

        // GET: /Catalog/
		/// <summary>
		/// Displays the catalog by specified URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>ActionResult.</returns>
		/// <exception cref="System.Web.HttpException">404;Category not found</exception>
		[CustomOutputCache(CacheProfile = "CatalogCache", VaryByCustom = "store;currency;cart")]
        public ActionResult Display(string url)
        {
            var category = _catalogClient.GetCategory(url);
            if (category != null && category.IsActive)
            {
                // set the context variable
                var set = UserHelper.CustomerSession.GetCustomerTagSet();
                set.Add(ContextFieldConstants.CategoryId, new Tag(category.CategoryId));
				UserHelper.CustomerSession.CategoryId = category.CategoryId;

                // display category
                return View(GetDisplayTemplate(TargetTypes.Category, category), category);
            }

			throw new HttpException(404, "Category not found");
        }

		/// <summary>
		/// Displays the item.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>ActionResult.</returns>
		/// <exception cref="System.Web.HttpException">404;Item not found</exception>
        [CustomOutputCache(CacheProfile = "CatalogCache", VaryByCustom = "store;currency;cart")]
        public ActionResult DisplayItem(string url)
        {
			var itemModel = CatalogHelper.CreateCatalogModel(url);

            if (ReferenceEquals(itemModel, null))
            {
                throw new HttpException(404, "Item not found");
            }

            return View(GetDisplayTemplate(TargetTypes.Item, itemModel.CatalogItem), itemModel);
        }

		/// <summary>
		/// Displays item variations.
		/// </summary>
		/// <param name="itemId">The item identifier.</param>
		/// <param name="name">The name.</param>
		/// <param name="selections">The selections.</param>
		/// <param name="variationId">The variation identifier.</param>
		/// <returns>ActionResult.</returns>
		[CustomOutputCache(CacheProfile = "CatalogCache")]
        public ActionResult ItemVariations(string itemId, string name, string[] selections = null,
                                           string variationId = null)
        {
            var variations = _catalogClient.GetItemRelations(itemId);
            var selectedVariation = string.IsNullOrEmpty(variationId) ? null : _catalogClient.GetItem(variationId);
            var model = new VariationsModel(variations, selections, selectedVariation);
            return PartialView(name, model);
        }

		/// <summary>
		/// Associations for the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="templateName">Name of the display template.</param>
		/// <param name="groupName">Name of the association group.</param>
		/// <returns>ActionResult.</returns>
		[CustomOutputCache(CacheProfile = "CatalogCache")]
		public ActionResult Association(ItemModel item, string templateName, string groupName)
        {
            var currentGroup = item.AssociationGroups
                                   .FirstOrDefault(
									   ag => ag.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
			return currentGroup == null ? null : PartialView(templateName, currentGroup);
        }

		/// <summary>
		/// Renders associated item.
		/// </summary>
		/// <param name="itemId">The item identifier.</param>
		/// <param name="name">The name.</param>
		/// <param name="associationType">Type of the association.</param>
		/// <returns>ActionResult.</returns>
        public ActionResult AssociatedItem(string itemId, string name, string associationType)
        {
			return DisplayItemById(itemId, name: name, associationType: associationType, displayOptions: ItemDisplayOptions.ItemOnly);
        }

		/// <summary>
		/// Displays the item by identifier.
		/// </summary>
		/// <param name="itemId">The item identifier.</param>
		/// <param name="parentItemId">The parent item identifier.</param>
		/// <param name="name">The name.</param>
		/// <param name="associationType">Type of the association.</param>
		/// <param name="forcedActive">if set to <c>true</c> [forced active].</param>
		/// <param name="responseGroups">The response groups.</param>
		/// <param name="displayOptions">The display options.</param>
		/// <returns>ActionResult.</returns>
		[CustomOutputCache(CacheProfile = "CatalogCache")]
		public ActionResult DisplayItemById(string itemId, string parentItemId = null, string name = "MiniItem", string associationType = null, bool forcedActive = false, ItemResponseGroups responseGroups = ItemResponseGroups.ItemSmall, ItemDisplayOptions displayOptions = ItemDisplayOptions.ItemSmall)
		{
			return DisplayItemByIdNoCache(itemId, parentItemId, name, associationType, forcedActive, responseGroups,
			                              displayOptions);
		}

		//Called from banner/ProductImageAndPrice to avoid nested caching
		/// <summary>
		/// Displays the item by identifier no cache.
		/// </summary>
		/// <param name="itemId">The item identifier.</param>
		/// <param name="parentItemId">The parent item identifier.</param>
		/// <param name="name">The name.</param>
		/// <param name="associationType">Type of the association.</param>
		/// <param name="forcedActive">if set to <c>true</c> [forced active].</param>
		/// <param name="responseGroups">The response groups.</param>
		/// <param name="displayOptions">The display options.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult DisplayItemByIdNoCache(string itemId, string parentItemId = null, string name = "MiniItem", string associationType = null, bool forcedActive = false, ItemResponseGroups responseGroups = ItemResponseGroups.ItemSmall, ItemDisplayOptions displayOptions = ItemDisplayOptions.ItemSmall)
		{
			var itemModel = CatalogHelper.CreateCatalogModel(itemId, parentItemId, associationType, forcedActive, responseGroups, displayOptions);
			return itemModel != null ? PartialView(name, itemModel) : null;
		}

		/// <summary>
		/// Adds the review.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns>ActionResult.</returns>
		[CustomOutputCache(CacheProfile = "CatalogCache")]
        public ActionResult AddReview(string id)
        {
            return PartialView("CreateReview",
                               new MReview
                                   {
                                       ItemId = id,
                                       CreatedDateTime = DateTime.UtcNow,
                                       Reviewer = new MReviewer {NickName = UserHelper.CustomerSession.CustomerName}
                                   });
        }

		/// <summary>
		/// Method uses displayTemplateClient to resolve which display template should be displayed
		/// based on current context
		/// </summary>
		/// <param name="type">Target type (Item, Category)</param>
		/// <param name="displayObject">The display object.</param>
		/// <returns>display template name</returns>
        private string GetDisplayTemplate(TargetTypes type, object displayObject)
        {
            // set the context variable
            var set = UserHelper.CustomerSession.GetCustomerTagSet();

            //Add Tags for category
            switch (type)
            {
                case TargetTypes.Category:
                    var category = displayObject as CategoryBase;
                    if (category != null)
                    {
                        set.Add(AppConfigContext.CategoryId, new Tag(category.CategoryId));
                    }
                    break;
                case TargetTypes.Item:
                    var item = displayObject as Item;
                    if (item != null)
                    {
                        set.Add(AppConfigContext.ItemId, new Tag(item.ItemId));
                        set.Add(AppConfigContext.ItemType, new Tag(item.GetType().Name));
                    }
                    break;
            }

            var viewName = _templateClient.GetDisplayTemplate(type, set);
            return string.IsNullOrEmpty(viewName) ? type.ToString() : viewName;
        }
    }
}
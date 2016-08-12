﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.Practices.Prism.Commands;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.ManagementClient.Core.Infrastructure;
using VirtoCommerce.ManagementClient.Core.Infrastructure.DataVirtualization;
using VirtoCommerce.Foundation.Catalogs.Model;
using VirtoCommerce.Foundation.Catalogs.Repositories;
using catalogModel = VirtoCommerce.Foundation.Catalogs.Model;

namespace VirtoCommerce.ManagementClient.DynamicContent.ViewModel
{
    public class SearchCategoryViewModel : ViewModelBase, ISearchCategoryViewModel, IVirtualListLoader<Category>
    {
	    private readonly IRepositoryFactory<ICatalogRepository> _catalogRepositoryFactory;
        private readonly object _catalogInfo;

        public DelegateCommand ClearFiltersCommand { get; private set; }
        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand<Category> SelectCommand { get; private set; }

        public string SearchName { get; set; }
        public string SearchCode { get; set; }
        public string SearchCatalogId { get; set; }
        public bool CanChangeSearchCatalog { get; private set; }

        public SearchCategoryViewModel(IRepositoryFactory<ICatalogRepository> catalogRepositoryFactory, object catalogInfo)
        {
            _catalogInfo = catalogInfo;
	        _catalogRepositoryFactory = catalogRepositoryFactory;
        }

        #region ISupportDelayInitialization Members

        public void InitializeForOpen()
        {
            CanChangeSearchCatalog = SearchModifier.HasFlag(SearchCategoryModifier.UserCanChangeSearchCatalog);

            if (_catalogInfo is CatalogBase && !CanChangeSearchCatalog)
            {
                AvailableCatalogs = new List<CatalogBase> {(CatalogBase) _catalogInfo};
	            SearchCatalogId = AvailableCatalogs[0].CatalogId;
            }
            else
            {
                if (_catalogInfo is CatalogBase) SearchCatalogId = ((CatalogBase)_catalogInfo).CatalogId;
                else if (!string.IsNullOrEmpty(_catalogInfo as string)) SearchCatalogId = (string)_catalogInfo;

                using (var repository = _catalogRepositoryFactory.GetRepositoryInstance())
                {
                    var query = repository.Catalogs;
                    if (!string.IsNullOrEmpty(SearchCatalogId) && !CanChangeSearchCatalog)
                    {
                        query = query.Where(x => x.CatalogId == SearchCatalogId);
                    }
                    else
                    {
                        if (SearchModifier.HasFlag(SearchCategoryModifier.RealCatalogsOnly)) query = query.OfType<catalogModel.Catalog>();

                        query = query.OrderBy(x => x.Name);
                    }

                    AvailableCatalogs = query.ToList();
                }
            }

            SearchCommand = new DelegateCommand(DoSearch);
            ClearFiltersCommand = new DelegateCommand(DoClearFilters);
            SelectCommand = new DelegateCommand<Category>(RaiseSelectInteractionRequest);

            ListItemsSource = new VirtualList<Category>(this, 20, SynchronizationContext.Current);
        }

        #endregion

        #region ISearchCategoryViewModel Members

        public SearchCategoryModifier SearchModifier { private get; set; }
        public List<CatalogBase> AvailableCatalogs { get; private set; }
        public CategoryBase SelectedItem { get; set; }
		public bool UseSubCategories { get; set; }

        private ICollectionView _itemsSource;
        public ICollectionView ListItemsSource
        {
            get { return _itemsSource; }
            private set { _itemsSource = value; OnPropertyChanged(); }
        }

        #endregion

        #region IVirtualListLoader<Category> Members

        public bool CanSort
        {
            get
            {
                return true;
            }
        }

        public IList<Category> LoadRange(int startIndex, int count, SortDescriptionCollection sortDescriptions, out int overallCount)
        {
            var retVal = new List<Category>();

            using (var repository = _catalogRepositoryFactory.GetRepositoryInstance())
            {
                var query = repository.Categories.OfType<Category>();

                if (!String.IsNullOrEmpty(SearchName))
                {
                    query = query.Where(x => x.Name.Contains(SearchName));
                }

                if (!String.IsNullOrEmpty(SearchCode))
                {
                    query = query.Where(x => x.Code.Contains(SearchCode));
                }

                if (!String.IsNullOrEmpty(SearchCatalogId))
                {
                    query = query.Where(x => x.CatalogId == SearchCatalogId);
                }

                overallCount = query.Count();
                var results = query.OrderBy(x => x.Name).Skip(startIndex).Take(count);
                retVal.AddRange(results);
            }
            return retVal;
        }

        #endregion

        #region private members

        private void DoSearch()
        {
            ListItemsSource.Refresh();
            SelectedItem = null;
        }

        private void DoClearFilters()
        {
            SearchName = SearchCode = null;
            OnPropertyChanged("SearchName");
            OnPropertyChanged("SearchCode");
        }

        private void RaiseSelectInteractionRequest(Category item)
        {
            SelectedItem = item;
        }

        #endregion

    }
}

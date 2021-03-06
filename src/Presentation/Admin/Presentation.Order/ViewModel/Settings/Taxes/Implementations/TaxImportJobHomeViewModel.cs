﻿using System.Linq;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Importing.Factories;
using VirtoCommerce.ManagementClient.Import.ViewModel.Implementations;
using VirtoCommerce.ManagementClient.Core.Infrastructure;
using VirtoCommerce.Foundation.Importing.Model;
using VirtoCommerce.Foundation.Importing.Repositories;
using VirtoCommerce.ManagementClient.Import.ViewModel.Interfaces;
using VirtoCommerce.ManagementClient.Import.ViewModel.Wizard;
using VirtoCommerce.ManagementClient.Order.ViewModel.Settings.Taxes.Interfaces;

namespace VirtoCommerce.ManagementClient.Order.ViewModel.Settings.Taxes.Implementations
{
    public class TaxImportJobHomeViewModel : ImportJobHomeViewModel, ITaxImportHomeViewModel
	{
		
		#region Constructor
		public TaxImportJobHomeViewModel(
			IRepositoryFactory<IImportRepository> importRepository,
			IViewModelsFactory<ICreateImportJobViewModel> wizardVmFactory,
			IViewModelsFactory<IImportJobRunViewModel> runVmFactory,
			IViewModelsFactory<IImportJobViewModel> itemVmFactory,
			IViewModelsFactory<IImportJobProgressViewModel> progressVmFactory,
			IImportJobEntityFactory entityFactory,
			IAuthenticationContext authContext, 
			SubTabsDefaultViewModel parentViewModel)
			: base(importRepository, wizardVmFactory, runVmFactory, itemVmFactory, progressVmFactory, entityFactory, authContext, parentViewModel)
		{
			AvailableImporters = new[]
				{
					ImportEntityType.TaxCategory,
					ImportEntityType.Jurisdiction, 
					ImportEntityType.JurisdictionGroup,
					ImportEntityType.TaxValue
				};
			DefaultImporter = AvailableImporters.First().ToString();
		}
		#endregion

		

    }
}

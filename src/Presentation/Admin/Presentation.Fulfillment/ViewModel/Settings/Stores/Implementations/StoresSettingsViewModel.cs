﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Omu.ValueInjecter;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Frameworks.ConventionInjections;
using VirtoCommerce.Foundation.Security.Model;
using VirtoCommerce.ManagementClient.Configuration.ViewModel;
using VirtoCommerce.ManagementClient.Configuration.ViewModel.Implementations;
using VirtoCommerce.ManagementClient.Core.Infrastructure;
using VirtoCommerce.ManagementClient.Core.Infrastructure.Navigation;
using VirtoCommerce.ManagementClient.Core.Infrastructure.Tiles;
using VirtoCommerce.ManagementClient.Fulfillment.ViewModel.Settings.Stores.Interfaces;
using VirtoCommerce.ManagementClient.Fulfillment.ViewModel.Settings.Stores.Wizard.Interfaces;
using VirtoCommerce.ManagementClient.Fulfillment.ViewModel.Wizard;
using VirtoCommerce.Foundation.Stores.Factories;
using VirtoCommerce.Foundation.Stores.Model;
using VirtoCommerce.Foundation.Stores.Repositories;

namespace VirtoCommerce.ManagementClient.Fulfillment.ViewModel.Settings.Stores.Implementations
{
	public class StoresSettingsViewModel : HomeSettingsEditableViewModel<Store>, IStoresSettingsViewModel
	{

		#region Dependencies

		private readonly NavigationManager _navManager;
		private readonly IRepositoryFactory<IStoreRepository> _repositoryFactory;
		private readonly IAuthenticationContext _authContext;
		private readonly TileManager _tileManager;
		#endregion

		#region Constructor

		public StoresSettingsViewModel(
			IRepositoryFactory<IStoreRepository> repositoryFactory,
			IStoreEntityFactory entityFactory, 
			IViewModelsFactory<ICreateStoreViewModel> wizardVmFactory, 
			IViewModelsFactory<IStoreViewModel> editVmFactory, 
			IAuthenticationContext authContext, 
			NavigationManager navManager, 
			TileManager tileManager)
			: base(entityFactory, wizardVmFactory, editVmFactory)
		{
			_repositoryFactory = repositoryFactory;
			_navManager = navManager;
			_tileManager = tileManager;
			_authContext = authContext;
			PopulateTiles();

			LinkedStoreNotifictaionRequest=new InteractionRequest<ConditionalConfirmation>();
		}

		#endregion

		#region Requests

		public InteractionRequest<ConditionalConfirmation> LinkedStoreNotifictaionRequest { get; private set; }

		#endregion

		#region HomeSettingsViewModel members

		protected override object LoadData()
		{
			using (var repository = _repositoryFactory.GetRepositoryInstance())
			{
				if (repository != null)
				{
					var items = repository.Stores.OrderBy(s => s.Name).ToList();
					return items;
				}
			}
			return null;
		}

		public override void RefreshItem(object item)
		{
			var itemToUpdate = item as Store;
			if (itemToUpdate != null)
			{
				Store itemFromInnerItem =
					Items.SingleOrDefault(s => s.StoreId == itemToUpdate.StoreId);

				if (itemFromInnerItem != null)
				{
					OnUIThread(() =>
					{
						itemFromInnerItem.InjectFrom<CloneInjection>(itemToUpdate);
						OnPropertyChanged("Items");
					});
				}
			}
		}

		#endregion

		#region HomeSettingsEditableViewModel members

		protected override void RaiseItemAddInteractionRequest()
		{
			var item = EntityFactory.CreateEntity<Store>();

			var vm = WizardVmFactory.GetViewModelInstance(
				new KeyValuePair<string, object>("item", item));
			var confirmation = new ConditionalConfirmation()
				{
					Title = "Create Store",
					Content = vm
				};
			ItemAdd(item, confirmation, _repositoryFactory.GetRepositoryInstance());
		}

		protected override void RaiseItemEditInteractionRequest(Store item)
		{
			var itemVm = EditVmFactory.GetViewModelInstance(
				new KeyValuePair<string, object>("item", item),
				new KeyValuePair<string, object>("parent", this));

			var openTracking = (IOpenTracking)itemVm;
			openTracking.OpenItemCommand.Execute();
		}

		protected override void RaiseItemDeleteInteractionRequest(Store item)
		{
			var confirmation = new ConditionalConfirmation()
				{
					Content = string.Format("Are you sure you want to delete Store '{0}'?", item.Name),
					Title = "Delete confirmation"
				};
			ItemDelete(item, confirmation, _repositoryFactory.GetRepositoryInstance());
		}

		protected override void ItemDelete(Store item, ConditionalConfirmation confirmation, IRepository repository)
		{
			CommonConfirmRequest.Raise(confirmation, async (x) =>
			{
				if (x.Confirmed)
				{
					string storeName;
					if (IsCurrentStoreLinkedToAnotherStore(item.StoreId, out storeName))
					{

						var linkedConfirmation = new ConditionalConfirmation()
						{
							Content =
								string.Format(
									"Store '{0}' are linked to Store '{1}'. You can't delete it.",
									item.Name, storeName),
							Title = "Warning"
						};
						LinkedStoreNotifictaionRequest.Raise(
							linkedConfirmation, (y) =>
							{

							});
						return;
					}

					OnUIThread(() => { ShowLoadingAnimation = true; });
					await Task.Run(() =>
					{
						repository.Attach(item);
						repository.Remove(item);
						repository.UnitOfWork.Commit();
					});
					OnUIThread(() =>
					{
						Items.Remove(item);
						ShowLoadingAnimation = false;
						RefreshItemListCommand.Execute();
					});

				}
			});
		}

		#endregion

		#region PricateMethods

		private bool IsCurrentStoreLinkedToAnotherStore(string storeId, out string storeName)
		{
			bool result = false;
			storeName = string.Empty;
			using (var repository = _repositoryFactory.GetRepositoryInstance())
			{
				var linkedStore = repository.StoreLinkedStores.Where(sls => sls.LinkedStoreId == storeId).FirstOrDefault();
				if (linkedStore != null)
				{

					var parentStore = repository.Stores.Where(s => s.StoreId == linkedStore.StoreId).SingleOrDefault();
					storeName = parentStore.Name;
					result = true;
				}
			}
			return result;
		}

		#endregion

		#region Tiles

		private bool NavigateToTabPage(string id)
		{

			var navigationData = _navManager.GetNavigationItemByName(Configuration.NavigationNames.HomeName);
			if (navigationData != null)
			{
				_navManager.Navigate(navigationData);
				var mainViewModel = _navManager.GetViewFromRegion(navigationData) as ConfigurationHomeViewModel;

				return (mainViewModel != null && mainViewModel.SetCurrentTabById(id));
			}
			return false;
		}

		private void PopulateTiles()
		{

			if (_authContext.CheckPermission(PredefinedPermissions.SettingsStores))
			{
				_tileManager.AddTile(new NumberTileItem()
				{
					IdModule = Configuration.NavigationNames.MenuName,
					IdTile = "StoresSettings",
					TileTitle = "STORES",
					Order = 1,
					IdColorSchema = TileColorSchemas.Schema2,
					NavigateCommand = new DelegateCommand(() => NavigateToTabPage(NavigationNames.StoresSettingsHomeName)),
					Refresh = async (tileItem) =>
					{
						using (var repository = _repositoryFactory.GetRepositoryInstance())
						{
							try
							{
								if (tileItem is NumberTileItem)
								{
									var query = await Task.Factory.StartNew(() => repository.Stores.Count());
									(tileItem as NumberTileItem).TileNumber = query.ToString();
								}
							}
							catch
							{
							}
						}
					}
				});
			}

		}

		#endregion
	}
}

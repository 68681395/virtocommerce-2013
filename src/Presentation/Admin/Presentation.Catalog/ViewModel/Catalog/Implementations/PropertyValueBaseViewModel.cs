﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using VirtoCommerce.Foundation.Catalogs.Model;
using VirtoCommerce.ManagementClient.Asset.ViewModel.Interfaces;
using VirtoCommerce.ManagementClient.Catalog.Model;
using VirtoCommerce.ManagementClient.Catalog.ViewModel.Catalog.Interfaces;
using VirtoCommerce.ManagementClient.Core.Controls;
using VirtoCommerce.ManagementClient.Core.Infrastructure;

namespace VirtoCommerce.ManagementClient.Catalog.ViewModel.Catalog.Implementations
{
	public class PropertyValueBaseViewModel : ViewModelBase, IPropertyValueBaseViewModel, IMultiSelectControlCommands
	{
		#region Dependencies
		
		private readonly IViewModelsFactory<IPickAssetViewModel> _vmFactory;

		#endregion

		public PropertyValueBaseViewModel(IViewModelsFactory<IPickAssetViewModel> vmFactory, PropertyAndPropertyValueBase item)
		{
			InnerItem = item;
			_vmFactory = vmFactory;

			if (InnerItem.IsMultiValue)
			{
				var defaultView = CollectionViewSource.GetDefaultView(InnerItem.Property.PropertyValues);
				defaultView.Filter = FilterPropertyValues;
			}

			SetVisibility();
			AssetPickCommand = new DelegateCommand(RaiseAssetPickInteractionRequest);
			AssetRemoveCommand = new DelegateCommand(RaiseAssetRemoveInteractionRequest);
			CommonConfirmRequest = new InteractionRequest<Confirmation>();
		}

		//public ObservableCollection<PropertyValue> AvailableValues { get; set; }
		public bool IsShortStringValue { get; private set; }
		public bool IsLongStringValue { get; private set; }
		public bool IsDecimalValue { get; private set; }
		public bool IsIntegerValue { get; private set; }
		public bool IsBooleanValue { get; private set; }
		public bool IsDateTimeValue { get; private set; }
		public bool IsAssetValue { get; private set; }
		public bool InnerPropertyIsEnum { get; set; }
		public bool InnerPropertyIsNotEnum { get; set; }

		#region IPropertyValueBaseViewModel

		private PropertyAndPropertyValueBase _innerItem;
		public PropertyAndPropertyValueBase InnerItem
		{
			get
			{
				return _innerItem;
			}

			set
			{
				_innerItem = value;
				OnPropertyChanged();
			}
		}


		public void SetVisibility()
		{
			InnerPropertyIsEnum = InnerItem.IsEnum && !InnerItem.IsMultiValue;
			InnerPropertyIsNotEnum = !InnerItem.IsEnum;

			var itemValueType = (PropertyValueType)InnerItem.PropertyValueType;
			IsShortStringValue = itemValueType == PropertyValueType.ShortString && !InnerItem.IsEnum;
			IsLongStringValue = itemValueType == PropertyValueType.LongString && !InnerItem.IsEnum;
			IsDecimalValue = itemValueType == PropertyValueType.Decimal && !InnerItem.IsEnum;
			IsIntegerValue = itemValueType == PropertyValueType.Integer && !InnerItem.IsEnum;
			IsBooleanValue = itemValueType == PropertyValueType.Boolean && !InnerItem.IsEnum;
			IsDateTimeValue = itemValueType == PropertyValueType.DateTime && !InnerItem.IsEnum;
			IsAssetValue = itemValueType == PropertyValueType.Image && !InnerItem.IsEnum;
		}

		public DelegateCommand AssetPickCommand { get; private set; }
		public DelegateCommand AssetRemoveCommand { get; private set; }
		public InteractionRequest<Confirmation> CommonConfirmRequest { get; private set; }

		public bool Validate()
		{
			if (InnerItem.Value != null && !InnerItem.IsMultiValue)
			{
				InnerItem.Value.Validate(true);

				if (InnerItem.Property != null && InnerItem.Property.IsEnum)
				{
					if (InnerItem.Value.KeyValue == null)
						InnerItem.Value.SetError("KeyValue", "must select", true);
					else
						InnerItem.Value.ClearError("KeyValue");
				}
				else
				{
					var itemValueType = (PropertyValueType)InnerItem.PropertyValueType;
					switch (itemValueType)
					{
						case PropertyValueType.ShortString:
							if (string.IsNullOrEmpty(InnerItem.Value.ShortTextValue))
								InnerItem.Value.SetError("ShortTextValue", "Value is required", true);
							// check for duplicates
							//else if (_parent.InnerItem.Values.Any(x => x.ValueType == InnerItem.ValueType))
							//    InnerItem.SetError("ShortTextValue", "Value is required", true);
							else
								InnerItem.Value.ClearError("ShortTextValue");
							break;
						case PropertyValueType.LongString:
							if (string.IsNullOrEmpty(InnerItem.Value.LongTextValue))
								InnerItem.Value.SetError("LongTextValue", "Value is required", true);
							else
								InnerItem.Value.ClearError("LongTextValue");
							break;
						case PropertyValueType.Decimal:
							break;
						case PropertyValueType.Integer:
							break;
						case PropertyValueType.Boolean:
							break;
						case PropertyValueType.DateTime:
							break;
						case PropertyValueType.Image:
							break;
						//case PropertyValueType.DictionaryKey:
						//default:
						//    break;
					}
				}

				// validation hack for new values
				InnerItem.Value.ClearError("ItemId");
				InnerItem.Value.ClearError("CategoryId");

				return InnerItem.Value.Errors.Count == 0;
			}
			else
			{
				InnerItem.Values.ToList().ForEach(value => value.Validate());
				return InnerItem.Values.All(val => val.Errors.Count == 0);
			}
		}

		#endregion

		private void RaiseAssetPickInteractionRequest()
		{
			var itemVM = _vmFactory.GetViewModelInstance();
			
			CommonConfirmRequest.Raise(
				new ConditionalConfirmation(itemVM.Validate) { Content = itemVM, Title = "Select property value" },
				(x) =>
				{
					if (x.Confirmed)
					{
						InnerItem.Value.ShortTextValue = itemVM.SelectedAsset.Name;
						InnerItem.Value.LongTextValue = itemVM.SelectedAsset.FolderItemId;
					}
				});
		}

		private void RaiseAssetRemoveInteractionRequest()
		{
			InnerItem.Value.ShortTextValue = null;
			InnerItem.Value.LongTextValue = null;
		}

		#region IMultiSelectControlCommands
		public void SelectItem(object selectedObj)
		{
			var selectedItem = (PropertyValue)selectedObj;
			var item = selectedItem;

			InnerItem.Values.Add(item);
		}

		public void SelectAllItems(ICollectionView availableItemsCollectionView)
		{
			var itemsList = availableItemsCollectionView.Cast<PropertyValue>().ToList();
			itemsList.ForEach(SelectItem);
		}

		public void UnSelectItem(object selectedObj)
		{

			var selectedItem = (PropertyValue)selectedObj;

			var item = InnerItem.Values.First(x => x.PropertyValueId.Equals(selectedItem.PropertyValueId));
			InnerItem.Values.Remove(item);
		}

		public void UnSelectAllItems(System.Collections.IList currentListItems)
		{
			if (currentListItems is IList<PropertyValue>)
				InnerItem.Values.ToList().ForEach(UnSelectItem);
		}
		#endregion

		private bool FilterPropertyValues(object item)
		{
			var result = false;
			if (item is PropertyValue)
			{
				var itemTyped = (PropertyValue)item;
				result = InnerItem.Values.All(x => x.PropertyValueId != itemTyped.PropertyValueId);
			}
			return result;
		}
	}
}

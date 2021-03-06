﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtoCommerce.Foundation.Frameworks;
using System.Runtime.Serialization;
using System.Data.Services.Common;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtoCommerce.Foundation.Catalogs.Model
{
	[DataContract]
	[EntitySet("CategoryBases")]
	[DataServiceKey("CategoryId")]
	public abstract class CategoryBase : StorageEntity
	{
		public CategoryBase()
		{
			_CategoryId = GenerateNewKey();
		}

		private string _CategoryId;
		[Key]
		[StringLength(128)]
		[DataMember]
		public string CategoryId
		{
			get
			{
				return _CategoryId;
			}
			set
			{
				SetValue(ref _CategoryId, () => this.CategoryId, value);
			}
		}

		private string _Code;
		[Required(ErrorMessage = "Field 'Category Code' is required.")]
		[DataMember]
        [StringLength(64)]
		public string Code
		{
			get
			{
				return _Code;
			}
			set
			{
				SetValue(ref _Code, () => this.Code, value);
			}
		}

		private bool _IsActive;
		[DataMember]
        [Required]
		public bool IsActive
		{
			get
			{
				return _IsActive;
			}
			set
			{
				SetValue(ref _IsActive, () => this.IsActive, value);
			}
		}

		private int _Priority;
		[DataMember]
		public int Priority
		{
			get
			{
				return _Priority;
			}
			set
			{
				SetValue(ref _Priority, () => this.Priority, value);
			}
		}

		#region Navigation Properties

		private string _CatalogId;
		[DataMember]
		[StringLength(128)]
		[ForeignKey("Catalog")]
		[Required]
		public string CatalogId
		{
			get
			{
				return _CatalogId;
			}
			set
			{
				SetValue(ref _CatalogId, () => this.CatalogId, value);
			}
		}

		[DataMember]
		public virtual CatalogBase Catalog { get; set; }

		private string _ParentCategoryId;
		[DataMember]
		[StringLength(128)]
		[ForeignKey("ParentCategory")]
		public string ParentCategoryId
		{
			get
			{
				return _ParentCategoryId;
			}
			set
			{
				SetValue(ref _ParentCategoryId, () => this.ParentCategoryId, value);
			}
		}

		[DataMember]
		public virtual CategoryBase ParentCategory { get; set; }

        ObservableCollection<LinkedCategory> _LinkedCategories = null;
        public virtual ObservableCollection<LinkedCategory> LinkedCategories
        {
            get
            {
                if (_LinkedCategories == null)
                    _LinkedCategories = new ObservableCollection<LinkedCategory>();

                return _LinkedCategories;
            }
        }

		#endregion

		//Must be unique
		public override string ToString()
		{
			return CategoryId;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.Foundation.AppConfig.Repositories;
using VirtoCommerce.Foundation.Catalogs.Repositories;
using VirtoCommerce.Foundation.Frameworks.Csv;
using VirtoCommerce.Foundation.Importing.Model;
using VirtoCommerce.Foundation.Importing.Repositories;
using VirtoCommerce.Foundation.Frameworks.Extensions;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Assets.Services;
using VirtoCommerce.Foundation.Orders.Repositories;

namespace VirtoCommerce.Foundation.Importing.Services
{
	[UnityInstanceProviderServiceBehaviorAttribute]
	public class ImportService : IImportService
	{
		#region Const

		private static string _comma = ",";
		private static string _tab = "\t";
		private static string _semicolon = ";";

		#endregion

		#region Dependencies
		private readonly IImportRepository _importJobRepository;
		private readonly ICatalogRepository _catalogRepository;
		private readonly ITaxRepository _taxRepository;
		private readonly IOrderRepository _orderRepository;
		private readonly IAssetService _assetProvider;
		private readonly IAppConfigRepository _appConfigRepository;
		#endregion

		#region privates
		private readonly List<IEntityImporter> _entityImporters;
		private readonly Dictionary<string, ImportResult> _importResults;
		#endregion

		#region constructor

		public ImportService(IImportRepository importRepository, ICatalogRepository catalogRepository, IOrderRepository orderRepository, ITaxRepository taxRepository, IAppConfigRepository appConfigRepository, IAssetService blobProvider)
		{
			_orderRepository = orderRepository;
			_taxRepository = taxRepository;
			_catalogRepository = catalogRepository;
			_importJobRepository = importRepository;
			_appConfigRepository = appConfigRepository;
			_assetProvider = blobProvider;

			_entityImporters = new List<IEntityImporter>
				{
					new ItemImporter() { Name = "Product"},
					new ItemImporter() { Name = "Sku"},
					new ItemImporter() { Name = "Bundle"},
					new ItemImporter() { Name = "DynamicKit"},
					new ItemImporter() { Name = "Package"},
					new PriceImporter(_catalogRepository),
					new AssociationImporter(_catalogRepository),
					new CategoryImporter(),
					new LocalizationImporter(),
					new TaxValueImporter(),
					new ItemAssetImporter(),
					new TaxCategoryImporter(),
					new JurisdictionImporter(),
					new JurisdictionGroupImporter()
				};

			_importResults = new Dictionary<string, ImportResult>();
		}
		
		#endregion

		#region IImportService

		public string[] GetEntityImporters()
		{
			return _entityImporters.Select(i => i.Name).ToArray();
		}
		
		public string[] GetSystemPropertyNames(string entityImporterName)
		{
			return _entityImporters.First(i => i.Name == entityImporterName).SystemPropertyNames;
		}

		public string GetCsvColumnsAutomatically(string csvFileName)
		{
			var _delimiter = string.Empty;
			string[] columns;

			using (var data = GetCsvContent(csvFileName))
			{
				var csvReader = new CsvReader(data, _comma);
				columns = csvReader.ReadRow();
				_delimiter = _comma;
			}

			using (var data = GetCsvContent(csvFileName))
			{
				var csvReader = new CsvReader(data, _semicolon);
				var temp = csvReader.ReadRow();
				if ((temp != null && columns!= null) && (temp.Count() > columns.Count()))
				{
					columns = temp;
					_delimiter = _semicolon;
				}
			}

			using (var data = GetCsvContent(csvFileName))
			{
				var csvReader = new CsvReader(data, _tab);
				var temp = csvReader.ReadRow();
				if ((temp != null && columns!= null) && (temp.Count() > columns.Count()))
				{
					_delimiter = _tab;
				}
			}

			return _delimiter;
		}
		
		public string[] GetCsvColumns(string csvFileName, string columnDelimiter)
		{
			using (var data = GetCsvContent(csvFileName))
			{
				var csvReader = new CsvReader(data, columnDelimiter);
				return csvReader.ReadRow();
			}
		}

		public bool Exists(string csvFileName)
		{
			return true;
		}
		
		public void RunImportJob(string jobId, string csvFileName)
		{
			var job = _importJobRepository.ImportJobs.Expand("PropertiesMap").Where(x => x.ImportJobId == jobId).SingleOrDefault();

			if (job != null)
			{
				var result = new ImportResult();
				_importResults[jobId] = result;

				using (var data = GetCsvContent(csvFileName))
				{
					//result.Length = 100000;
					result.Length = data.BaseStream.CanSeek ? data.BaseStream.Length : 100000;
					var csvReader = new CsvReader(data, job.ColumnDelimiter);
					try
					{
						result.IsRunning = true;
						result.Started = DateTime.Now;

						Import(job, csvReader, result);
					}
					finally
					{
						result.Stopped = DateTime.Now;
						result.IsRunning = false;
					}
				}
			}
		}
		
		public ImportResult GetImportResult(string jobId)
		{
			ImportResult result = null;

			if (_importResults.ContainsKey(jobId))
				result = _importResults[jobId];

			return result;
		}

		#endregion

		#region Private methods

		private StreamReader GetCsvContent(string fileName)
		{
			var stream = _assetProvider.OpenReadOnly(fileName);
			if (stream.CanSeek)
				stream.Seek(0, SeekOrigin.Begin);
			return new StreamReader(stream);
		}
		
		private void Import(ImportJob job, CsvReader reader, ImportResult result)
		{
			var skip = job.ImportStep;
			var count = job.ImportCount;
			var startIndex = job.StartIndex;

			var csvNames = GetCsvNamesAndIndexes(reader);
			if (csvNames.Count > 0)
			{
				var importer = _entityImporters.SingleOrDefault(i => i.Name == job.EntityImporter);

				if (importer != null)
				{
					var processed = 0;
					while (true)
					{
						try
						{
							var csvValues = reader.ReadRow();
							processed++;
							if (csvValues == null)
								break;

							if ((result.ProcessedRecordsCount < count || count <= 0) && processed >= startIndex && ((processed - startIndex) % skip == 0))
							{
								var systemValues = MapColumns(job.PropertiesMap.Where(prop => prop.IsSystemProperty), csvNames, csvValues);
								var customValues = MapColumns(job.PropertiesMap.Where(prop => !prop.IsSystemProperty), csvNames, csvValues);
								
								var rep = IsTaxImport(importer.Name) ? _orderRepository : (IRepository) _catalogRepository;

								if (importer.Name == ImportEntityType.Localization.ToString())
									rep = _appConfigRepository;

								var res = importer.Import(job.CatalogId, job.PropertySetId, systemValues, customValues, rep);
								result.CurrentProgress = reader.CurrentPosition;
								
								if (string.IsNullOrEmpty(res))
								{
									rep.UnitOfWork.Commit();
									result.ProcessedRecordsCount++;
								}
								else
								{
									result.ErrorsCount++;
									if (result.Errors == null)
										result.Errors = new List<string>();
									result.Errors.Add(string.Format("Row: {0}, Error: {1}", result.ProcessedRecordsCount + result.ErrorsCount, res));

									//check if errors amount reached the allowed errors limit if yes do not save made changes.
									if (result.ErrorsCount >= job.MaxErrorsCount)
									{
										break;
									}
								}
							}
						}
						catch (Exception e)
						{
							result.ErrorsCount++;
							if (result.Errors == null)
								result.Errors = new List<string>();
							result.Errors.Add(e.Message+Environment.NewLine+e);

							//check if errors amount reached the allowed errors limit if yes do not save made changes.
							if (result.ErrorsCount >= job.MaxErrorsCount)
							{
								break;
							}
						}
					}
				}
			}
			else
			{
				if (result.Errors == null)
					result.Errors = new List<string>();
				result.Errors.Add("File contains no rows");
			}

			if (result.ErrorsCount <= job.MaxErrorsCount)
			{
				try
				{
					_catalogRepository.UnitOfWork.Commit();
				}
				catch (Exception e)
				{
					if (result.Errors == null)
						result.Errors = new List<string>();
					result.Errors.Add(e.Message);
				}
			}

		}

		private static bool IsTaxImport(string entityImporter)
		{
			return entityImporter == ImportEntityType.Jurisdiction.ToString() || entityImporter == ImportEntityType.JurisdictionGroup.ToString() ||
			       entityImporter == ImportEntityType.TaxValue.ToString();
		}

		private static Dictionary<string, int> GetCsvNamesAndIndexes(CsvReader reader)
		{
			var csvNames = reader.ReadRow();

			var csvNamesAndIndexes = new Dictionary<string, int>();
			if (csvNames != null)
			{
				for (var i = 0; i < csvNames.Length; i++)
					csvNamesAndIndexes.Add(csvNames[i], i);
			}
			return csvNamesAndIndexes;
		}

		private static ImportItem[] MapColumns(IEnumerable<MappingItem> mappingItems, IReadOnlyDictionary<string, int> csvNamesAndIndexes, IList<string> csvValues)
		{
			var csvNamesAndValues = new List<ImportItem>();

			if (mappingItems != null)
			{
				foreach (var mappingItem in mappingItems)
				{
					var importItem = new ImportItem {Name = mappingItem.EntityColumnName, Locale = mappingItem.Locale};
					if (mappingItem.CsvColumnName != null && csvNamesAndIndexes.ContainsKey(mappingItem.CsvColumnName))
					{
						var columnIndex = csvNamesAndIndexes[mappingItem.CsvColumnName];

						if (csvValues[columnIndex] != null)
							importItem.Value = csvValues[columnIndex];

						if (!string.IsNullOrEmpty(mappingItem.StringFormat))
							importItem.Value = string.Format(mappingItem.StringFormat, importItem.Value);
					}
					else if (mappingItem.CustomValue != null)
					{
						importItem.Value = mappingItem.CustomValue;

						if (!string.IsNullOrEmpty(mappingItem.StringFormat))
							importItem.Value = string.Format(mappingItem.StringFormat, importItem.Value);
					}

					csvNamesAndValues.Add(importItem);
				}
			}

			return csvNamesAndValues.ToArray();
		}

		#endregion
	}
}

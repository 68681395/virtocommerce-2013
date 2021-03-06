﻿using System;
using System.Management.Automation;
using VirtoCommerce.Foundation.Data.Orders;
using VirtoCommerce.PowerShell.Orders;

namespace VirtoCommerce.PowerShell.DatabaseSetup.Cmdlet
{
	[CLSCompliant(false)]
	[Cmdlet(VerbsData.Publish, "Virto-Order-Database", SupportsShouldProcess = true, DefaultParameterSetName = "DbConnection")]
	public class PublishOrderDatabase : DatabaseCommand
	{
		public override void Publish(string dbconnection, string data, bool sample)
		{
			base.Publish(dbconnection, data, sample);
			var connection = dbconnection;
			SafeWriteDebug("ConnectionString: " + connection);

			using (var db = new EFOrderRepository(connection))
			{
				if (sample)
				{
					SafeWriteVerbose("Running sample scripts");
					new SqlOrderSampleDatabaseInitializer().InitializeDatabase(db);
				}
				else
				{
					SafeWriteVerbose("Running minimum scripts");
					new SqlOrderDatabaseInitializer().InitializeDatabase(db);
				}
			}
		}
	}
}

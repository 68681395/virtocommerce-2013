﻿using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.IO;
using DbMigrator = VirtoCommerce.Foundation.Data.Infrastructure.DbMigrator;

namespace FunctionalTests.TestHelpers
{
    public class MigrationsTestBase : TestBase
    {
        public const string DefaultDatabaseName = "VCMigrationsTests";

        public TestDatabase TestDatabase { get; private set; }

        public InfoContext Info
        {
            get { return TestDatabase.Info; }
        }

        public bool TableExists(string name)
        {
            return Info.TableExists(name);
        }

        public bool ColumnExists(string table, string name)
        {
            return Info.ColumnExists(table, name);
        }

        public void ResetDatabase()
        {
            //DropDatabase();
            //TestDatabase.EnsureDatabase();
            if (DatabaseExists())
            {
                TestDatabase.ResetDatabase();
            }
            else
            {
                TestDatabase.EnsureDatabase();
                TestDatabase.ResetDatabase();
            }
        }

        public void DropDatabase()
        {
            if (DatabaseExists())
            {
                TestDatabase.DropDatabase();
            }
        }

        public bool DatabaseExists()
        {
            if (TestDatabase == null)
                Init(DefaultDatabaseName);

            return TestDatabase.Exists();
        }

        public string ConnectionString
        {
            get { return TestDatabase.ConnectionString; }
        }

        public virtual void Init(string databaseName)
        {
            try
            {
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetTempPath());
                TestDatabase = new SqlTestDatabase(databaseName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        protected DbMigrator CreateMigrator<TConfiguration>() where TConfiguration : DbMigrationsConfiguration
        {
            var configuration = typeof(TConfiguration).CreateInstance<TConfiguration>();
            //var configuration = new Configuration();
            configuration.TargetDatabase = new DbConnectionInfo(TestDatabase.ConnectionString, TestDatabase.ProviderName);
			var migrator = new DbMigrator(configuration);
            return migrator;
        }

        public TContext CreateContext<TContext>()
    where TContext : DbContext
        {
            var contextInfo = new DbContextInfo(
                typeof(TContext), new DbConnectionInfo(TestDatabase.ConnectionString, TestDatabase.ProviderName));

            return (TContext)contextInfo.CreateInstance();
        }
    }
}

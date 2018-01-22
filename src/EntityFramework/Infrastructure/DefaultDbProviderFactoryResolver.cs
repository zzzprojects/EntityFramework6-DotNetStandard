// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.


#if !NET40

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Utilities;
    using System.Data.SqlClient;

    internal class DefaultDbProviderFactoryResolver : IDbProviderFactoryResolver
    {
        public DbProviderFactory ResolveProviderFactory(DbConnection connection)
        {
            Check.NotNull(connection, "connection");

            return SqlClientFactory.Instance;
            //return DbProviderFactories.GetFactory(connection);
        }
    }
}

#endif

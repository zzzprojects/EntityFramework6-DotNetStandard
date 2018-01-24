// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.


#if !NET40

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Utilities;
    using System.Data.SqlClient;

    internal class DefaultDbProviderFactoryResolver : IDbProviderFactoryResolver
    {
        public DbProviderFactory ResolveProviderFactory(DbConnection connection)
        {
            Check.NotNull(connection, "connection");

            if (connection is EntityConnection) return EntityProviderFactory.Instance;
            if (connection is SqlConnection) return SqlClientFactory.Instance;

            throw new NotImplementedException();
            //return DbProviderFactories.GetFactory(connection);
        }
    }
}

#endif

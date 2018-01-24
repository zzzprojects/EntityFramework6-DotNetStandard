using System;
using System.Collections.Generic;
using System.Text;

using System.Data.Entity;

namespace NetCoreTestApp
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(string connectionString) : base(connectionString)
        {
        }

        public DbSet<BlogPost> BlogPosts { get; set; }
    }
}

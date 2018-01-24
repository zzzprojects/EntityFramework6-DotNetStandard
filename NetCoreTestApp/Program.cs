using System;
using System.Linq;
using System.Data.Entity;

namespace NetCoreTestApp
{
    using System.Threading.Tasks;

    /// <summary>
    /// Test app for EF6 on .NET Standard 2.0 / .NET Core.
    /// 
    /// To use, install SQL Express and create database called Ef6NetCoreTest. Then run:
    /// CREATE TABLE [dbo].[BlogPosts](
    /// [Id] [int] IDENTITY(1,1) NOT NULL,
    /// [Title] [varchar](500) NOT NULL
    /// ) ON [PRIMARY]
    /// GO
    /// INSERT INTO BlogPosts (Title) VALUES ('Test 1'), ('Test 2');
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            using (var context =
                new MyDbContext("Data Source=.\\SQLEXPRESS;Initial Catalog=Ef6NetCoreTest;Integrated Security=true"))
            {
                var posts = await context.BlogPosts.Where(i => i.Title == "Test 2").ToListAsync();

                foreach (var post in posts)
                {
                    Console.WriteLine(post.Title);
                }

                Console.ReadKey();
            }
        }
    }
}

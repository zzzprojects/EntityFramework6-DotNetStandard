using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace NetCoreTestApp
{
    public class BlogPost
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }
    }
}

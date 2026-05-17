using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace crochet_store
{
    public static class DBConnection
    {
        public static readonly string ConnectionString =
            @"Data Source=.\SQLEXPRESS;
              Initial Catalog=db_crochet_craft3;
              Integrated Security=True;
              Encrypt=False;";
    }
}


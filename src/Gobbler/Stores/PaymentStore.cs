using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gobbler.Domain;
using Dapper;
using System.Reflection;

namespace Gobbler.Stores
{
    public class PaymentStore
    {
        public const string TableName = "payments";

        IDbConnection _db;

        static PaymentStore()
        {
            SimpleCRUD.SetDialect( SimpleCRUD.Dialect.MySQL );
        }

        public PaymentStore( IDbConnection db )
        {
            _db = db;
        }

        public Payment Add( Payment obj )
        {
            _db.Insert( obj );
            return obj;
        }

        public IList<Payment> GetAll()
        {
            return _db.GetList<Payment>().ToList();
        }

        public static bool ValidateTable( IDbConnection db )
        {
            var cols = db.Query<string>( @"
                select column_name
                from information_schema.columns a
                where a.table_name = @tableName
	                and a.table_schema = @database",
                new {
                    tableName = TableName,
                    database = db.Database
                } 
            );
            var props = typeof( Payment ).GetProperties( BindingFlags.Public | BindingFlags.Instance );

            var join = new HashSet<string>( cols );
            // Check column names match
            foreach( var property in props ) {
                if ( !join.Contains( property.Name ) ) {
                    return false;
                }
            }

            return true;
        }
    }
}
